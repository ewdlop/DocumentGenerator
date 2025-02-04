using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System.Text.Json.Serialization;

namespace MilitaryElectronics.Search
{
    // Search Index Model
    public class ComponentDocument
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; }

        [SearchableField(IsSortable = true, IsFilterable = true, IsFacetable = true)]
        public string ComponentName { get; set; }

        [SearchableField(IsSortable = true, IsFilterable = true, IsFacetable = true)]
        public string Category { get; set; }

        [SearchableField(IsFilterable = false)]
        public string Description { get; set; }

        [SearchableField(IsSortable = true, IsFilterable = true, IsFacetable = true)]
        public string Manufacturer { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string[] MilSpecLevel { get; set; }

        public PerformanceMetrics PerformanceMetrics { get; set; }

        public Cost Cost { get; set; }

        public EnvironmentalRatings EnvironmentalRatings { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string[] Tags { get; set; }

        [SimpleField(IsSortable = true, IsFilterable = true)]
        public DateTimeOffset LastUpdated { get; set; }
    }

    public class PerformanceMetrics
    {
        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double RadiationHardening { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public int OperatingTempMin { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public int OperatingTempMax { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double PowerConsumption { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public long MtbfHours { get; set; }
    }

    public class Cost
    {
        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double UnitCost { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double MaintenanceCostYearly { get; set; }
    }

    public class EnvironmentalRatings
    {
        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double VibrationResistance { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double ShockResistance { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double EmpResistance { get; set; }
    }

    // Search Service Client
    public class ComponentSearchService
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _searchIndexClient;
        private const string IndexName = "military-electronics-index";

        public ComponentSearchService(string searchServiceEndpoint, string adminApiKey, string queryApiKey)
        {
            _searchIndexClient = new SearchIndexClient(new Uri(searchServiceEndpoint), 
                new AzureKeyCredential(adminApiKey));
            
            _searchClient = new SearchClient(new Uri(searchServiceEndpoint), 
                IndexName, new AzureKeyCredential(queryApiKey));
        }

        public async Task CreateIndexAsync()
        {
            var fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(ComponentDocument));

            var definition = new SearchIndex(IndexName, searchFields)
            {
                Suggesters = {
                    new SearchSuggester("componentSuggester", new[] { 
                        nameof(ComponentDocument.ComponentName),
                        nameof(ComponentDocument.Manufacturer),
                        nameof(ComponentDocument.Category)
                    })
                },
                ScoringProfiles = {
                    new ScoringProfile("performanceBooster")
                    {
                        Functions = new List<ScoringFunction>
                        {
                            new MagnitudeScoringFunction(
                                nameof(ComponentDocument.PerformanceMetrics) + "/" + 
                                nameof(PerformanceMetrics.MtbfHours),
                                2,
                                new MagnitudeScoringParameters(10000, 100000)
                                {
                                    ShouldBoostBeyondRangeByConstant = false
                                })
                        }
                    }
                }
            };

            await _searchIndexClient.CreateOrUpdateIndexAsync(definition);
        }

        public async Task<Response<IndexDocumentsResult>> IndexComponentsAsync(
            IEnumerable<ComponentDocument> components)
        {
            var batch = IndexDocumentsBatch.Upload(components);
            return await _searchClient.IndexDocumentsAsync(batch);
        }

        public async Task<SearchResults<ComponentDocument>> SearchComponentsAsync(
            string searchText,
            SearchOptions options = null)
        {
            options ??= new SearchOptions
            {
                IncludeTotalCount = true,
                Filter = null,
                OrderBy = { "LastUpdated desc" }
            };

            return await _searchClient.SearchAsync<ComponentDocument>(searchText, options);
        }

        public async Task<Response<AutocompleteResults>> SuggestComponentsAsync(
            string searchText,
            bool fuzzy = true)
        {
            var options = new AutocompleteOptions
            {
                Mode = AutocompleteMode.OneTermWithContext,
                UseFuzzyMatching = fuzzy,
                Size = 10
            };

            return await _searchClient.AutocompleteAsync(searchText, "componentSuggester", options);
        }

        // Advanced search with filters
        public async Task<SearchResults<ComponentDocument>> SearchWithFiltersAsync(
            string searchText,
            double? minMtbf = null,
            double? maxCost = null,
            string[] categories = null,
            string[] manufacturers = null)
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

            if (categories?.Length > 0)
            {
                var categoryFilter = string.Join(" or ", categories.Select(c => $"category eq '{c}'"));
                filterConditions.Add($"({categoryFilter})");
            }

            if (manufacturers?.Length > 0)
            {
                var manufacturerFilter = string.Join(" or ", 
                    manufacturers.Select(m => $"manufacturer eq '{m}'"));
                filterConditions.Add($"({manufacturerFilter})");
            }

            var options = new SearchOptions
            {
                Filter = string.Join(" and ", filterConditions),
                OrderBy = { "cost/unitCost asc" },
                IncludeTotalCount = true,
                Facets = {
                    "category",
                    "manufacturer",
                    "milSpecLevel",
                    "performanceMetrics/mtbfHours,interval:10000",
                    "cost/unitCost,interval:1000"
                }
            };

            return await _searchClient.SearchAsync<ComponentDocument>(searchText, options);
        }
    }

    // Example Usage
    public class SearchExample
    {
        public async Task RunSearchExampleAsync()
        {
            var searchService = new ComponentSearchService(
                "https://your-service.search.windows.net",
                "admin-api-key",
                "query-api-key"
            );

            // Create or update index
            await searchService.CreateIndexAsync();

            // Index some documents
            var components = new List<ComponentDocument>
            {
                new ComponentDocument
                {
                    Id = "1",
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
                    Cost = new Cost
                    {
                        UnitCost = 12500,
                        MaintenanceCostYearly = 1000
                    },
                    Tags = new[] { "radiation-hardened", "high-reliability", "space-grade" },
                    LastUpdated = DateTimeOffset.UtcNow
                }
            };

            await searchService.IndexComponentsAsync(components);

            // Perform a search
            var searchResults = await searchService.SearchWithFiltersAsync(
                "processor",
                minMtbf: 40000,
                maxCost: 15000,
                categories: new[] { "Processor" },
                manufacturers: new[] { "TechCorp" }
            );

            // Get suggestions
            var suggestions = await searchService.SuggestComponentsAsync("proc");
        }
    }
}