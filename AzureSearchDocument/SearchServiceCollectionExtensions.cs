using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MilitaryElectronics.Search
{
    // Registration extensions
    public static class SearchServiceCollectionExtensions
    {
        public static IServiceCollection AddSearchServices(
            this IServiceCollection services,
            Action<SearchServiceOptions> configureOptions)
        {
            services.Configure(configureOptions);

            services.AddAzureClients(builder =>
            {
                builder.AddClient<SearchClient, SearchClientOptions>((options, provider) =>
                {
                    var searchOptions = provider.GetRequiredService<IOptions<SearchServiceOptions>>().Value;
                    return new SearchClient(
                        new Uri(searchOptions.SearchServiceEndpoint),
                        searchOptions.IndexName,
                        new AzureKeyCredential(searchOptions.QueryApiKey),
                        options);
                });

                builder.AddClient<SearchIndexClient, SearchClientOptions>((options, provider) =>
                {
                    var searchOptions = provider.GetRequiredService<IOptions<SearchServiceOptions>>().Value;
                    return new SearchIndexClient(
                        new Uri(searchOptions.SearchServiceEndpoint),
                        new AzureKeyCredential(searchOptions.AdminApiKey),
                        options);
                });
            });

            services.AddSingleton<ISearchIndexDefinitionService, SearchIndexDefinitionService>();
            services.AddScoped<ISearchService<ComponentDocument>, InjectedComponentSearchService>();

            return services;
        }
    }

    // Index definition service
    public interface ISearchIndexDefinitionService
    {
        SearchIndex GetIndexDefinition();
    }

    public class SearchIndexDefinitionService : ISearchIndexDefinitionService
    {
        private readonly string _indexName;

        public SearchIndexDefinitionService(IOptions<SearchServiceOptions> options)
        {
            _indexName = options.Value.IndexName;
        }

        public SearchIndex GetIndexDefinition()
        {
            var fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(ComponentDocument));

            return new SearchIndex(_indexName, searchFields)
            {
                Suggesters =
                {
                    new SearchSuggester("componentSuggester", new[]
                    {
                        nameof(ComponentDocument.ComponentName),
                        nameof(ComponentDocument.Manufacturer),
                        nameof(ComponentDocument.Category)
                    })
                }
            };
        }
    }

    // Injected implementation of search service
    public class InjectedComponentSearchService : ISearchService<ComponentDocument>
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _indexClient;
        private readonly ISearchIndexDefinitionService _indexDefinitionService;
        private readonly ILogger<InjectedComponentSearchService> _logger;
        private bool _disposed;

        public InjectedComponentSearchService(
            SearchClient searchClient,
            SearchIndexClient indexClient,
            ISearchIndexDefinitionService indexDefinitionService,
            ILogger<InjectedComponentSearchService> logger)
        {
            _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
            _indexClient = indexClient ?? throw new ArgumentNullException(nameof(indexClient));
            _indexDefinitionService = indexDefinitionService ?? throw new ArgumentNullException(nameof(indexDefinitionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual async Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating or updating search index");
                var definition = _indexDefinitionService.GetIndexDefinition();
                var response = await _indexClient.CreateOrUpdateIndexAsync(
                    definition,
                    cancellationToken: cancellationToken);
                
                _logger.LogInformation("Successfully created or updated search index");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or update search index");
                throw new SearchServiceException("Failed to create index", ex);
            }
        }

        public virtual async Task<Response<IndexDocumentsResult>> IndexDocumentsAsync(
            IEnumerable<ComponentDocument> documents,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Indexing {count} documents", documents.Count());
                var batch = IndexDocumentsBatch.Upload(documents);
                var response = await _searchClient.IndexDocumentsAsync(
                    batch,
                    cancellationToken: cancellationToken);
                
                _logger.LogInformation("Successfully indexed documents");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index documents");
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
                _logger.LogInformation("Performing search with query: {searchText}", searchText);
                
                options ??= new SearchOptions
                {
                    IncludeTotalCount = true,
                    Size = 50,
                    Skip = 0,
                    OrderBy = { "LastUpdated desc" }
                };

                var results = await _searchClient.SearchAsync<ComponentDocument>(
                    searchText,
                    options,
                    cancellationToken);

                _logger.LogInformation("Search completed successfully");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform search");
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
                _logger.LogInformation("Getting suggestions for: {searchText}", searchText);
                
                var options = new AutocompleteOptions
                {
                    Mode = AutocompleteMode.OneTermWithContext,
                    UseFuzzyMatching = fuzzy,
                    Size = 10
                };

                var response = await _searchClient.AutocompleteAsync(
                    searchText,
                    "componentSuggester",
                    options,
                    cancellationToken);

                _logger.LogInformation("Successfully retrieved suggestions");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get suggestions");
                throw new SearchServiceException("Failed to get suggestions", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    // Example usage
    public class SearchServiceExample
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSearchServices(options =>
            {
                options.SearchServiceEndpoint = "https://your-service.search.windows.net";
                options.AdminApiKey = "your-admin-key";
                options.QueryApiKey = "your-query-key";
                options.IndexName = "components-index";
            });

            // Configure other services
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }

        public class SearchController
        {
            private readonly ISearchService<ComponentDocument> _searchService;

            public SearchController(ISearchService<ComponentDocument> searchService)
            {
                _searchService = searchService;
            }

            public async Task<SearchResults<ComponentDocument>> Search(string query)
            {
                return await _searchService.SearchAsync(query);
            }
        }
    }
}