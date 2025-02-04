using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace MilitaryElectronics.Search
{
    public interface ISearchService<T> : IAsyncDisposable where T : class, ISearchDocument
    {
        Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default);
        Task<Response<IndexDocumentsResult>> IndexDocumentsAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default);
        Task<SearchResults<T>> SearchAsync(string searchText, SearchOptions options = null, CancellationToken cancellationToken = default);
        Task<Response<AutocompleteResults>> SuggestAsync(string searchText, bool fuzzy = true, CancellationToken cancellationToken = default);
    }

    // Service Implementation
    public class ComponentSearchService : ISearchService<ComponentDocument>
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _searchIndexClient;
        private readonly SearchServiceOptions _options;
        private bool _disposed;

        public ComponentSearchService(IOptions<SearchServiceOptions> options)
        {
            _options = options.Value;
            _searchIndexClient = new SearchIndexClient(
                new Uri(_options.SearchServiceEndpoint), 
                new AzureKeyCredential(_options.AdminApiKey));
            
            _searchClient = new SearchClient(
                new Uri(_options.SearchServiceEndpoint), 
                _options.IndexName, 
                new AzureKeyCredential(_options.QueryApiKey));
        }

        public virtual async Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                var fieldBuilder = new FieldBuilder();
                var searchFields = fieldBuilder.Build(typeof(ComponentDocument));

                var definition = new SearchIndex(_options.IndexName, searchFields)
                {
                    Suggesters = {
                        new SearchSuggester("componentSuggester", new[] { 
                            nameof(ComponentDocument.ComponentName),
                            nameof(ComponentDocument.Manufacturer),
                            nameof(ComponentDocument.Category)
                        })
                    }
                };

                await _searchIndexClient.CreateOrUpdateIndexAsync(definition, cancellationToken: cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                throw new SearchServiceException("Failed to create index", ex);
            }
        }

        public virtual async Task<Response<IndexDocumentsResult>> IndexDocumentsAsync(
            IEnumerable<ComponentDocument> documents,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                var batch = IndexDocumentsBatch.Upload(documents);
                return await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                throw new SearchServiceException("Failed to index documents", ex);
            }
        }

        public virtual async Task<SearchResults<ComponentDocument>> SearchAsync(
            string searchText,
            SearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                options ??= new SearchOptions
                {
                    IncludeTotalCount = true,
                    Size = 50,
                    Skip = 0,
                    OrderBy = { "LastUpdated desc" }
                };

                return await _searchClient.SearchAsync<ComponentDocument>(
                    searchText, 
                    options, 
                    cancellationToken);
            }
            catch (Exception ex)
            {
                throw new SearchServiceException("Failed to perform search", ex);
            }
        }

        public virtual async Task<Response<AutocompleteResults>> SuggestAsync(
            string searchText,
            bool fuzzy = true,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                var options = new AutocompleteOptions
                {
                    Mode = AutocompleteMode.OneTermWithContext,
                    UseFuzzyMatching = fuzzy,
                    Size = 10
                };

                return await _searchClient.AutocompleteAsync(
                    searchText, 
                    "componentSuggester", 
                    options, 
                    cancellationToken);
            }
            catch (Exception ex)
            {
                throw new SearchServiceException("Failed to get suggestions", ex);
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ComponentSearchService));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
            {
                return;
            }

            if (_searchClient != null)
            {
                await _searchClient.DisposeAsync().ConfigureAwait(false);
            }

            if (_searchIndexClient != null)
            {
                await _searchIndexClient.DisposeAsync().ConfigureAwait(false);
            }

            _disposed = true;
        }

        ~ComponentSearchService()
        {
            _ = DisposeAsyncCore();
        }
    }

    // Example Usage with async using
    public class SearchExample
    {
        private readonly ISearchService<ComponentDocument> _searchService;

        public SearchExample(ISearchService<ComponentDocument> searchService)
        {
            _searchService = searchService ?? 
                throw new ArgumentNullException(nameof(searchService));
        }

        public async Task RunSearchExampleAsync(CancellationToken cancellationToken = default)
        {
            await using (_searchService.ConfigureAwait(false))
            {
                // Create or update index
                await _searchService.CreateIndexAsync(cancellationToken);

                // Index some documents
                var components = new List<ComponentDocument>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ComponentName = "RAD-HARD Processor XM-5",
                        Category = "Processor",
                        Manufacturer = "TechCorp",
                        MilSpecLevel = new[] { "MIL-STD-883" },
                        PerformanceMetrics = new PerformanceMetrics
                        {
                            RadiationHardening = 100,
                            OperatingTempMin = -55,
                            OperatingTempMax = 125,
                            PowerConsumption = 15.5,
                            MtbfHours = 50000
                        },
                        LastUpdated = DateTimeOffset.UtcNow
                    }
                };

                await _searchService.IndexDocumentsAsync(components, cancellationToken);

                // Perform a search with filters
                var options = new SearchOptions()
                    .AddFilters(
                        minMtbf: 40000,
                        maxCost: 15000,
                        categories: new[] { "Processor" },
                        manufacturers: new[] { "TechCorp" }
                    );

                var searchResults = await _searchService.SearchAsync(
                    "processor",
                    options,
                    cancellationToken);

                // Get suggestions
                var suggestions = await _searchService.SuggestAsync(
                    "proc",
                    fuzzy: true,
                    cancellationToken);
            }
        }
    }

    // Resource management helper
    public static class AsyncDisposableExtensions
    {
        public static async Task<T> UsingAsync<T, TDisposable>(
            this TDisposable disposable,
            Func<TDisposable, Task<T>> action) 
            where TDisposable : IAsyncDisposable
        {
            try
            {
                return await action(disposable).ConfigureAwait(false);
            }
            finally
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    // Example of async resource management
    public class ResourceManagementExample
    {
        private readonly ISearchService<ComponentDocument> _searchService;

        public ResourceManagementExample(ISearchService<ComponentDocument> searchService)
        {
            _searchService = searchService;
        }

        public async Task<SearchResults<ComponentDocument>> SafeSearchAsync(
            string searchText,
            SearchOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _searchService.UsingAsync(async service =>
                await service.SearchAsync(searchText, options, cancellationToken)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }
}