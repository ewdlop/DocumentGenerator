using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MilitaryElectronics.Search
{
    // Base search service with protected members and virtual methods
    public abstract class BaseSearchService<T> : ISearchService<T> where T : class, ISearchDocument
    {
        protected readonly SearchClient SearchClient;
        protected readonly SearchIndexClient IndexClient;
        protected readonly ILogger Logger;
        protected readonly ISearchIndexDefinitionService IndexDefinitionService;
        protected bool IsDisposed;

        protected BaseSearchService(
            SearchClient searchClient,
            SearchIndexClient indexClient,
            ISearchIndexDefinitionService indexDefinitionService,
            ILogger logger)
        {
            SearchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
            IndexClient = indexClient ?? throw new ArgumentNullException(nameof(indexClient));
            IndexDefinitionService = indexDefinitionService ?? throw new ArgumentNullException(nameof(indexDefinitionService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected virtual void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        protected virtual SearchOptions CreateDefaultSearchOptions()
        {
            return new SearchOptions
            {
                IncludeTotalCount = true,
                Size = 50,
                Skip = 0,
                OrderBy = { "LastUpdated desc" }
            };
        }

        protected virtual AutocompleteOptions CreateDefaultAutocompleteOptions(bool fuzzy)
        {
            return new AutocompleteOptions
            {
                Mode = AutocompleteMode.OneTermWithContext,
                UseFuzzyMatching = fuzzy,
                Size = 10
            };
        }

        public virtual async Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                Logger.LogInformation("Creating or updating search index");
                var definition = await GetIndexDefinitionAsync(cancellationToken);
                var response = await IndexClient.CreateOrUpdateIndexAsync(
                    definition,
                    cancellationToken: cancellationToken);
                
                Logger.LogInformation("Successfully created or updated search index");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create or update search index");
                throw new SearchServiceException("Failed to create index", ex);
            }
        }

        protected virtual Task<SearchIndex> GetIndexDefinitionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(IndexDefinitionService.GetIndexDefinition());
        }

        public virtual async Task<Response<IndexDocumentsResult>> IndexDocumentsAsync(
            IEnumerable<T> documents,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                var documentsList = documents.ToList();
                Logger.LogInformation("Indexing {count} documents", documentsList.Count);
                
                await OnBeforeIndexDocumentsAsync(documentsList, cancellationToken);
                var batch = IndexDocumentsBatch.Upload(documentsList);
                var response = await SearchClient.IndexDocumentsAsync(batch, cancellationToken);
                await OnAfterIndexDocumentsAsync(documentsList, response, cancellationToken);
                
                Logger.LogInformation("Successfully indexed documents");
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to index documents");
                throw new SearchServiceException("Failed to index documents", ex);
            }
        }

        protected virtual Task OnBeforeIndexDocumentsAsync(
            IReadOnlyList<T> documents,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnAfterIndexDocumentsAsync(
            IReadOnlyList<T> documents,
            Response<IndexDocumentsResult> response,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<SearchResults<T>> SearchAsync(
            string searchText,
            SearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                Logger.LogInformation("Performing search with query: {searchText}", searchText);
                
                options ??= CreateDefaultSearchOptions();
                await OnBeforeSearchAsync(searchText, options, cancellationToken);
                
                var results = await SearchClient.SearchAsync<T>(
                    searchText,
                    options,
                    cancellationToken);

                await OnAfterSearchAsync(searchText, options, results, cancellationToken);
                
                Logger.LogInformation("Search completed successfully");
                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to perform search");
                throw new SearchServiceException("Failed to perform search", ex);
            }
        }

        protected virtual Task OnBeforeSearchAsync(
            string searchText,
            SearchOptions options,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnAfterSearchAsync(
            string searchText,
            SearchOptions options,
            SearchResults<T> results,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<Response<AutocompleteResults>> SuggestAsync(
            string searchText,
            bool fuzzy = true,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                Logger.LogInformation("Getting suggestions for: {searchText}", searchText);
                
                var options = CreateDefaultAutocompleteOptions(fuzzy);
                await OnBeforeSuggestAsync(searchText, options, cancellationToken);
                
                var response = await SearchClient.AutocompleteAsync(
                    searchText,
                    await GetSuggesterNameAsync(cancellationToken),
                    options,
                    cancellationToken);

                await OnAfterSuggestAsync(searchText, options, response, cancellationToken);
                
                Logger.LogInformation("Successfully retrieved suggestions");
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get suggestions");
                throw new SearchServiceException("Failed to get suggestions", ex);
            }
        }

        protected virtual Task<string> GetSuggesterNameAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult("componentSuggester");
        }

        protected virtual Task OnBeforeSuggestAsync(
            string searchText,
            AutocompleteOptions options,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnAfterSuggestAsync(
            string searchText,
            AutocompleteOptions options,
            Response<AutocompleteResults> response,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (!IsDisposed)
            {
                await DisposeAsyncCore().ConfigureAwait(false);
                IsDisposed = true;
            }
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            try
            {
                await OnBeforeDisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await OnDisposeAsync().ConfigureAwait(false);
            }
        }

        protected virtual Task OnBeforeDisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnDisposeAsync()
        {
            return Task.CompletedTask;
        }
    }

    // Concrete implementation
    public class InjectedComponentSearchService : BaseSearchService<ComponentDocument>
    {
        private readonly SearchServiceOptions _options;

        public InjectedComponentSearchService(
            SearchClient searchClient,
            SearchIndexClient indexClient,
            ISearchIndexDefinitionService indexDefinitionService,
            IOptions<SearchServiceOptions> options,
            ILogger<InjectedComponentSearchService> logger)
            : base(searchClient, indexClient, indexDefinitionService, logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override SearchOptions CreateDefaultSearchOptions()
        {
            var options = base.CreateDefaultSearchOptions();
            options.SearchFields.Add(nameof(ComponentDocument.ComponentName));
            options.SearchFields.Add(nameof(ComponentDocument.Description));
            return options;
        }

        protected override Task OnBeforeSearchAsync(
            string searchText,
            SearchOptions options,
            CancellationToken cancellationToken)
        {
            Logger.LogInformation("Customizing search for component search");
            options.IncludeFacets = true;
            options.Facets.Add(nameof(ComponentDocument.Category));
            options.Facets.Add(nameof(ComponentDocument.Manufacturer));
            return base.OnBeforeSearchAsync(searchText, options, cancellationToken);
        }

        protected override async Task OnAfterSearchAsync(
            string searchText,
            SearchOptions options,
            SearchResults<ComponentDocument> results,
            CancellationToken cancellationToken)
        {
            await base.OnAfterSearchAsync(searchText, options, results, cancellationToken);
            Logger.LogInformation("Found {count} results for component search", 
                await results.GetCountAsync());
        }

        protected override async Task OnBeforeIndexDocumentsAsync(
            IReadOnlyList<ComponentDocument> documents,
            CancellationToken cancellationToken)
        {
            await base.OnBeforeIndexDocumentsAsync(documents, cancellationToken);
            foreach (var doc in documents)
            {
                doc.LastUpdated = DateTimeOffset.UtcNow;
            }
        }
    }

    // Example usage
    public class SearchExample
    {
        private readonly ISearchService<ComponentDocument> _searchService;

        public SearchExample(ISearchService<ComponentDocument> searchService)
        {
            _searchService = searchService;
        }

        public async Task CustomSearchImplementationAsync()
        {
            var options = new SearchOptions
            {
                Filter = "category eq 'Processor'",
                OrderBy = { "lastUpdated desc" }
            };

            var results = await _searchService.SearchAsync("test", options);
            foreach (var result in results.GetResults())
            {
                Console.WriteLine($"Found: {result.Document.ComponentName}");
            }
        }
    }
}