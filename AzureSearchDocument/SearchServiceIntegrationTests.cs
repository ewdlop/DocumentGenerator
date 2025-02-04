using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace MilitaryElectronics.Search.IntegrationTests
{
    public class TestSearchService : BaseSearchService<ComponentDocument>
    {
        public TestSearchService(
            SearchClient searchClient,
            SearchIndexClient indexClient,
            ISearchIndexDefinitionService indexDefinitionService,
            ILogger<TestSearchService> logger)
            : base(searchClient, indexClient, indexDefinitionService, logger)
        {
        }

        public SearchClient ExposedSearchClient => SearchClient;
        public SearchIndexClient ExposedIndexClient => IndexClient;
        public bool IsDisposedState => IsDisposed;
    }

    public class SearchServiceIntegrationTests : IAsyncDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ServiceCollection _services;

        public SearchServiceIntegrationTests()
        {
            _services = new ServiceCollection();
            ConfigureServices(_services);
            _serviceProvider = _services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddDebug());

            services.AddSearchServices(options =>
            {
                options.SearchServiceEndpoint = "https://test-search.search.windows.net";
                options.AdminApiKey = "test-admin-key";
                options.QueryApiKey = "test-query-key";
                options.IndexName = "test-index";
            });

            // Register test service
            services.AddScoped<TestSearchService>();
        }

        [Fact]
        public void ServiceProvider_ResolvesAllDependencies()
        {
            // Arrange & Act
            var searchService = _serviceProvider.GetRequiredService<ISearchService<ComponentDocument>>();

            // Assert
            Assert.NotNull(searchService);
            Assert.IsType<InjectedComponentSearchService>(searchService);
        }

        [Fact]
        public void ServiceProvider_ResolvesScopedServices_InNewScope()
        {
            // Arrange
            using var scope1 = _serviceProvider.CreateScope();
            using var scope2 = _serviceProvider.CreateScope();

            // Act
            var service1 = scope1.ServiceProvider.GetRequiredService<ISearchService<ComponentDocument>>();
            var service2 = scope2.ServiceProvider.GetRequiredService<ISearchService<ComponentDocument>>();

            // Assert
            Assert.NotNull(service1);
            Assert.NotNull(service2);
            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void ServiceProvider_ResolvesOptions()
        {
            // Arrange & Act
            var options = _serviceProvider.GetRequiredService<IOptions<SearchServiceOptions>>();

            // Assert
            Assert.NotNull(options.Value);
            Assert.Equal("https://test-search.search.windows.net", options.Value.SearchServiceEndpoint);
            Assert.Equal("test-index", options.Value.IndexName);
        }

        [Fact]
        public void ServiceProvider_ResolvesSearchClients()
        {
            // Arrange & Act
            var searchClient = _serviceProvider.GetRequiredService<SearchClient>();
            var indexClient = _serviceProvider.GetRequiredService<SearchIndexClient>();

            // Assert
            Assert.NotNull(searchClient);
            Assert.NotNull(indexClient);
        }

        [Fact]
        public async Task SearchService_DisposeAsync_DisposesCorrectly()
        {
            // Arrange
            var testService = _serviceProvider.GetRequiredService<TestSearchService>();

            // Act
            await testService.DisposeAsync();

            // Assert
            Assert.True(testService.IsDisposedState);
        }

        [Fact]
        public async Task ScopedService_DisposesWithScope()
        {
            // Arrange
            TestSearchService testService;
            
            // Act
            await using (var scope = _serviceProvider.CreateAsyncScope())
            {
                testService = scope.ServiceProvider.GetRequiredService<TestSearchService>();
            }

            // Assert
            Assert.True(testService.IsDisposedState);
        }

        [Fact]
        public void ServiceProvider_InjectsLogger()
        {
            // Arrange & Act
            var service = _serviceProvider.GetRequiredService<TestSearchService>();
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<TestSearchService>();

            // Assert
            Assert.NotNull(logger);
        }

        [Fact]
        public async Task SearchService_CreateIndex_UsesCorrectClients()
        {
            // Arrange
            var testService = _serviceProvider.GetRequiredService<TestSearchService>();

            // Act & Assert
            await Assert.ThrowsAsync<RequestFailedException>(
                async () => await testService.CreateIndexAsync());
        }

        public async ValueTask DisposeAsync()
        {
            if (_serviceProvider is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
    }

    public class SearchServiceLifetimeTests
    {
        [Fact]
        public async Task MultipleScopes_MaintainIndependentLifetimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSearchServices(options =>
            {
                options.SearchServiceEndpoint = "https://test-search.search.windows.net";
                options.AdminApiKey = "test-admin-key";
                options.QueryApiKey = "test-query-key";
                options.IndexName = "test-index";
            });

            var provider = services.BuildServiceProvider();

            // Act
            ISearchService<ComponentDocument> service1;
            ISearchService<ComponentDocument> service2;

            await using (var scope1 = provider.CreateAsyncScope())
            {
                service1 = scope1.ServiceProvider.GetRequiredService<ISearchService<ComponentDocument>>();
                
                await using (var scope2 = provider.CreateAsyncScope())
                {
                    service2 = scope2.ServiceProvider.GetRequiredService<ISearchService<ComponentDocument>>();
                    
                    // Assert
                    Assert.NotNull(service1);
                    Assert.NotNull(service2);
                    Assert.NotSame(service1, service2);
                }
            }
        }
    }

    public class CustomSearchServiceTests
    {
        public class CustomComponentSearchService : InjectedComponentSearchService
        {
            public CustomComponentSearchService(
                SearchClient searchClient,
                SearchIndexClient indexClient,
                ISearchIndexDefinitionService indexDefinitionService,
                IOptions<SearchServiceOptions> options,
                ILogger<CustomComponentSearchService> logger)
                : base(searchClient, indexClient, indexDefinitionService, options, logger)
            {
            }

            protected override SearchOptions CreateDefaultSearchOptions()
            {
                var options = base.CreateDefaultSearchOptions();
                options.Size = 100; // Custom size
                return options;
            }
        }

        [Fact]
        public void CustomService_RegistersAndResolves()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSearchServices(options =>
            {
                options.SearchServiceEndpoint = "https://test-search.search.windows.net";
                options.AdminApiKey = "test-admin-key";
                options.QueryApiKey = "test-query-key";
                options.IndexName = "test-index";
            });

            // Replace the default implementation with custom one
            services.Replace(ServiceDescriptor.Scoped<ISearchService<ComponentDocument>, CustomComponentSearchService>());

            var provider = services.BuildServiceProvider();

            // Act
            var service = provider.GetRequiredService<ISearchService<ComponentDocument>>();

            // Assert
            Assert.NotNull(service);
            Assert.IsType<CustomComponentSearchService>(service);
        }
    }
}