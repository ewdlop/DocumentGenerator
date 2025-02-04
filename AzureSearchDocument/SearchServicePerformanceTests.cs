using Azure.Core.Extensions;
using Azure.Search.Documents;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace MilitaryElectronics.Search.Performance
{
    // Updated Client Factory with Azure Client Builder
    public static class SearchClientBuilderExtensions
    {
        public static IAzureClientBuilder<SearchClient, SearchClientOptions> AddSearchClient(
            this AzureClientFactoryBuilder builder,
            string serviceName = "Search")
        {
            return builder.AddClient<SearchClient, SearchClientOptions>((options, provider) =>
            {
                var searchOptions = provider.GetRequiredService<IOptions<SearchServiceOptions>>().Value;
                var credential = new AzureKeyCredential(searchOptions.QueryApiKey);
                return new SearchClient(
                    new Uri(searchOptions.SearchServiceEndpoint),
                    searchOptions.IndexName,
                    credential,
                    options);
            });
        }

        public static IAzureClientBuilder<SearchIndexClient, SearchClientOptions> AddSearchIndexClient(
            this AzureClientFactoryBuilder builder,
            string serviceName = "SearchIndex")
        {
            return builder.AddClient<SearchIndexClient, SearchClientOptions>((options, provider) =>
            {
                var searchOptions = provider.GetRequiredService<IOptions<SearchServiceOptions>>().Value;
                var credential = new AzureKeyCredential(searchOptions.AdminApiKey);
                return new SearchIndexClient(
                    new Uri(searchOptions.SearchServiceEndpoint),
                    credential,
                    options);
            });
        }
    }

    // Performance Tests
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class SearchServicePerformanceTests
    {
        private ISearchService<ComponentDocument> _searchService;
        private ServiceProvider _serviceProvider;
        private readonly List<ComponentDocument> _testDocuments;
        private const int BatchSize = 1000;

        public SearchServicePerformanceTests()
        {
            _testDocuments = GenerateTestDocuments(BatchSize);
        }

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Configure services
            services.Configure<SearchServiceOptions>(options =>
            {
                options.SearchServiceEndpoint = "https://your-service.search.windows.net";
                options.AdminApiKey = "your-admin-key";
                options.QueryApiKey = "your-query-key";
                options.IndexName = "perf-test-index";
            });

            // Add Azure clients
            services.AddAzureClients(builder =>
            {
                builder.AddSearchClient();
                builder.AddSearchIndexClient();
            });

            services.AddScoped<ISearchService<ComponentDocument>, ComponentSearchService>();

            _serviceProvider = services.BuildServiceProvider();
            _searchService = _serviceProvider.GetRequiredService<ISearchService<ComponentDocument>>();
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            await _searchService.DisposeAsync();
            await _serviceProvider.DisposeAsync();
        }

        private List<ComponentDocument> GenerateTestDocuments(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new ComponentDocument
                {
                    Id = i.ToString(),
                    ComponentName = $"Test Component {i}",
                    Category = $"Category {i % 5}",
                    Manufacturer = $"Manufacturer {i % 10}",
                    Description = $"Description for component {i}",
                    MilSpecLevel = new[] { $"MIL-STD-{i % 3}" },
                    Tags = new[] { $"tag{i}", $"performance", $"test" },
                    LastUpdated = DateTimeOffset.UtcNow
                }).ToList();
        }

        [Benchmark]
        public async Task BulkIndexing()
        {
            await _searchService.IndexDocumentsAsync(_testDocuments);
        }

        [Benchmark]
        public async Task SimpleSearch()
        {
            await _searchService.SearchAsync("test");
        }

        [Benchmark]
        public async Task ComplexSearch()
        {
            var options = new SearchOptions
            {
                Filter = "category eq 'Category 1'",
                OrderBy = { "lastUpdated desc" },
                Facets = { "category", "manufacturer", "milSpecLevel" },
                IncludeTotalCount = true,
                Size = 50
            };

            await _searchService.SearchAsync("test", options);
        }

        [Benchmark]
        public async Task Suggestions()
        {
            await _searchService.SuggestAsync("te", true);
        }
    }

    // Load Testing Helper
    public class SearchLoadTester
    {
        private readonly ISearchService<ComponentDocument> _searchService;
        private readonly int _concurrentUsers;
        private readonly int _requestsPerUser;

        public SearchLoadTester(
            ISearchService<ComponentDocument> searchService,
            int concurrentUsers = 10,
            int requestsPerUser = 100)
        {
            _searchService = searchService;
            _concurrentUsers = concurrentUsers;
            _requestsPerUser = requestsPerUser;
        }

        public async Task<LoadTestResult> RunLoadTestAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task>();
            var errorCount = 0;
            var latencies = new ConcurrentBag<double>();

            for (int user = 0; user < _concurrentUsers; user++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int request = 0; request < _requestsPerUser; request++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var requestStopwatch = Stopwatch.StartNew();
                        try
                        {
                            await _searchService.SearchAsync(
                                $"test{request}", 
                                new SearchOptions { Size = 10 }, 
                                cancellationToken);

                            latencies.Add(requestStopwatch.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception)
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            return new LoadTestResult
            {
                TotalRequests = _concurrentUsers * _requestsPerUser,
                ErrorCount = errorCount,
                TotalDuration = stopwatch.Elapsed,
                AverageLatency = latencies.Average(),
                MinLatency = latencies.Min(),
                MaxLatency = latencies.Max(),
                P95Latency = CalculatePercentile(latencies.ToList(), 95),
                P99Latency = CalculatePercentile(latencies.ToList(), 99)
            };
        }

        private static double CalculatePercentile(List<double> latencies, int percentile)
        {
            var sorted = latencies.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
            return sorted[index];
        }
    }

    public class LoadTestResult
    {
        public int TotalRequests { get; set; }
        public int ErrorCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double AverageLatency { get; set; }
        public double MinLatency { get; set; }
        public double MaxLatency { get; set; }
        public double P95Latency { get; set; }
        public double P99Latency { get; set; }

        public double RequestsPerSecond => TotalRequests / TotalDuration.TotalSeconds;
        public double ErrorRate => (double)ErrorCount / TotalRequests;
    }

    // Example Usage
    public class PerformanceTestExample
    {
        public async Task RunPerformanceTestsAsync()
        {
            // Run benchmarks
            BenchmarkRunner.Run<SearchServicePerformanceTests>();

            // Setup services
            var services = new ServiceCollection();
            services.Configure<SearchServiceOptions>(options =>
            {
                options.SearchServiceEndpoint = "https://your-service.search.windows.net";
                options.AdminApiKey = "your-admin-key";
                options.QueryApiKey = "your-query-key";
                options.IndexName = "perf-test-index";
            });

            services.AddAzureClients(builder =>
            {
                builder.AddSearchClient();
                builder.AddSearchIndexClient();
            });

            services.AddScoped<ISearchService<ComponentDocument>, ComponentSearchService>();

            using var serviceProvider = services.BuildServiceProvider();
            var searchService = serviceProvider.GetRequiredService<ISearchService<ComponentDocument>>();

            // Run load test
            var loadTester = new SearchLoadTester(
                searchService,
                concurrentUsers: 10,
                requestsPerUser: 100);

            var result = await loadTester.RunLoadTestAsync();

            Console.WriteLine($"Load Test Results:");
            Console.WriteLine($"Total Requests: {result.TotalRequests}");
            Console.WriteLine($"Requests/sec: {result.RequestsPerSecond:F2}");
            Console.WriteLine($"Error Rate: {result.ErrorRate:P2}");
            Console.WriteLine($"Average Latency: {result.AverageLatency:F2}ms");
            Console.WriteLine($"P95 Latency: {result.P95Latency:F2}ms");
            Console.WriteLine($"P99 Latency: {result.P99Latency:F2}ms");
        }
    }
}