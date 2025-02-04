using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace MilitaryElectronics.Search
{
    // Configuration
    public class SearchServiceOptions
    {
        public const string ConfigurationSection = "AzureSearch";
        public string SearchServiceEndpoint { get; set; } = string.Empty;
        public string AdminApiKey { get; set; } = string.Empty;
        public string QueryApiKey { get; set; } = string.Empty;
        public string IndexName { get; set; } = "military-electronics-index";
    }

    // Base Search Document Interface
    public interface ISearchDocument
    {
        string Id { get; set; }
        DateTimeOffset LastUpdated { get; set; }
    }

    // Base Interface for Search Operations
    public interface ISearchService<T> where T : class, ISearchDocument
    {
        Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default);
        Task<Response<IndexDocumentsResult>> IndexDocumentsAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default);
        Task<SearchResults<T>> SearchAsync(string searchText, SearchOptions options = null, CancellationToken cancellationToken = default);
        Task<Response<AutocompleteResults>> SuggestAsync(string searchText, bool fuzzy = true, CancellationToken cancellationToken = default);
    }

    // Document Models
    public class ComponentDocument : ISearchDocument
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = string.Empty;

        [SearchableField(IsSortable = true, IsFilterable = true, IsFacetable = true)]
        public string ComponentName { get; set; } = string.Empty;

        [SearchableField(IsSortable = true, IsFilterable = true, IsFacetable = true)]
        public string Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = false)]
        public string Description { get; set; } = string.Empty;

        [SearchableField(IsSortable = true, IsFilterable = true, IsFacetable = true)]
        public string Manufacturer { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string[] MilSpecLevel { get; set; } = Array.Empty<string>();

        public PerformanceMetrics PerformanceMetrics { get; set; } = new();

        public Cost Cost { get; set; } = new();

        public EnvironmentalRatings EnvironmentalRatings { get; set; } = new();

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string[] Tags { get; set; } = Array.Empty<string>();

        [SimpleField(IsSortable = true, IsFilterable = true)]
        public DateTimeOffset LastUpdated { get; set; }
    }

    // Service Implementation
    public class ComponentSearchService : ISearchService<ComponentDocument>, IDisposable
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
                // Log the exception
                throw new SearchServiceException("Failed to create index", ex);
            }
        }

        public virtual async Task<Response<IndexDocumentsResult>> IndexDocumentsAsync(
            IEnumerable<ComponentDocument> documents,
            CancellationToken cancellationToken = default)
        {
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _searchClient?.Dispose();
                    _searchIndexClient?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    // Custom Exception
    public class SearchServiceException : Exception
    {
        public SearchServiceException(string message) : base(message) { }
        public SearchServiceException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    // Extension Methods
    public static class SearchOptionsExtensions
    {
        public static SearchOptions AddFilters(
            this SearchOptions options,
            double? minMtbf = null,
            double? maxCost = null,
            IEnumerable<string> categories = null,
            IEnumerable<string> manufacturers = null)
        {
            var filterConditions = new List<string>();

            if (minMtbf.HasValue)
            {
                filterConditions.Add($"performanceMetrics/mtbfHours ge {minMtbf.Value}");
            }

            if (maxCost.HasValue)
            {
                filterConditions.Add($"cost/unitCost le {maxCost.Value}");
            }

            if (categories?.Any() == true)
            {
                var categoryFilter = string.Join(" or ", 
                    categories.Select(c => $"category eq '{c}'"));
                filterConditions.Add($"({categoryFilter})");
            }

            if (manufacturers?.Any() == true)
            {
                var manufacturerFilter = string.Join(" or ", 
                    manufacturers.Select(m => $"manufacturer eq '{m}'"));
                filterConditions.Add($"({manufacturerFilter})");
            }

            options.Filter = string.Join(" and ", filterConditions);
            return options;
        }
    }

    // Dependency Injection Setup
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSearchServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<SearchServiceOptions>(
                configuration.GetSection(SearchServiceOptions.ConfigurationSection));
            
            services.AddScoped<ISearchService<ComponentDocument>, ComponentSearchService>();
            
            return services;
        }
    }

    // Example Usage with DI
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