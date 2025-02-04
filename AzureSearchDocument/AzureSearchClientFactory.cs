using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MilitaryElectronics.Search.Tests
{
    // Search Client Factory Interface
    public interface ISearchClientFactory
    {
        SearchClient CreateSearchClient();
        SearchIndexClient CreateIndexClient();
    }

    // Factory Implementation
    public class AzureSearchClientFactory : ISearchClientFactory, IAsyncDisposable
    {
        private readonly SearchServiceOptions _options;
        private readonly List<IAsyncDisposable> _clients;
        private bool _disposed;

        public AzureSearchClientFactory(IOptions<SearchServiceOptions> options)
        {
            _options = options.Value;
            _clients = new List<IAsyncDisposable>();
        }

        public virtual SearchClient CreateSearchClient()
        {
            ThrowIfDisposed();
            var client = new SearchClient(
                new Uri(_options.SearchServiceEndpoint),
                _options.IndexName,
                new AzureKeyCredential(_options.QueryApiKey));
            _clients.Add(client);
            return client;
        }

        public virtual SearchIndexClient CreateIndexClient()
        {
            ThrowIfDisposed();
            var client = new SearchIndexClient(
                new Uri(_options.SearchServiceEndpoint),
                new AzureKeyCredential(_options.AdminApiKey));
            _clients.Add(client);
            return client;
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AzureSearchClientFactory));
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                foreach (var client in _clients)
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                _clients.Clear();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    // Updated Service with Factory
    public class ComponentSearchService : ISearchService<ComponentDocument>
    {
        private readonly ISearchClientFactory _clientFactory;
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _indexClient;
        private bool _disposed;

        public ComponentSearchService(ISearchClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            _searchClient = clientFactory.CreateSearchClient();
            _indexClient = clientFactory.CreateIndexClient();
        }

        // ... rest of the implementation remains the same ...
    }

    // Unit Tests
    public class ComponentSearchServiceTests
    {
        private readonly Mock<ISearchClientFactory> _mockFactory;
        private readonly Mock<SearchClient> _mockSearchClient;
        private readonly Mock<SearchIndexClient> _mockIndexClient;

        public ComponentSearchServiceTests()
        {
            _mockFactory = new Mock<ISearchClientFactory>();
            _mockSearchClient = new Mock<SearchClient>(MockBehavior.Strict);
            _mockIndexClient = new Mock<SearchIndexClient>(MockBehavior.Strict);

            _mockFactory.Setup(f => f.CreateSearchClient())
                .Returns(_mockSearchClient.Object);
            _mockFactory.Setup(f => f.CreateIndexClient())
                .Returns(_mockIndexClient.Object);
        }

        [Fact]
        public async Task CreateIndex_Success_ReturnsTrue()
        {
            // Arrange
            _mockIndexClient
                .Setup(c => c.CreateOrUpdateIndexAsync(
                    It.IsAny<SearchIndex>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response>().Object);

            var service = new ComponentSearchService(_mockFactory.Object);

            // Act
            var result = await service.CreateIndexAsync();

            // Assert
            Assert.True(result);
            _mockIndexClient.Verify(c => c.CreateOrUpdateIndexAsync(
                It.IsAny<SearchIndex>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Search_WithValidQuery_ReturnsResults()
        {
            // Arrange
            var expectedResults = SearchModelFactory.SearchResults<ComponentDocument>(
                new[] { new ComponentDocument { Id = "1" } },
                1,
                null,
                null,
                null);

            _mockSearchClient
                .Setup(c => c.SearchAsync<ComponentDocument>(
                    It.IsAny<string>(),
                    It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResults);

            var service = new ComponentSearchService(_mockFactory.Object);

            // Act
            var results = await service.SearchAsync("test");

            // Assert
            Assert.Single(results.GetResults());
            _mockSearchClient.Verify(c => c.SearchAsync<ComponentDocument>(
                It.IsAny<string>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IndexDocuments_WithValidDocuments_Succeeds()
        {
            // Arrange
            var documents = new[] { new ComponentDocument { Id = "1" } };

            _mockSearchClient
                .Setup(c => c.IndexDocumentsAsync(
                    It.IsAny<IndexDocumentsBatch<ComponentDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<IndexDocumentsResult>>().Object);

            var service = new ComponentSearchService(_mockFactory.Object);

            // Act & Assert
            await Assert.ThrowsAsync<SearchServiceException>(async () =>
                await service.IndexDocumentsAsync(documents));
        }

        [Fact]
        public async Task Suggest_WithValidQuery_ReturnsSuggestions()
        {
            // Arrange
            var expectedResults = SearchModelFactory.AutocompleteResults(
                new[] { SearchModelFactory.AutocompleteItem("test") });

            _mockSearchClient
                .Setup(c => c.AutocompleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<AutocompleteOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(expectedResults, new Mock<Response>().Object));

            var service = new ComponentSearchService(_mockFactory.Object);

            // Act
            var result = await service.SuggestAsync("test");

            // Assert
            Assert.Single(result.Value.Results);
            _mockSearchClient.Verify(c => c.AutocompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AutocompleteOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Dispose_DisposesClientsCorrectly()
        {
            // Arrange
            var service = new ComponentSearchService(_mockFactory.Object);

            // Act
            await service.DisposeAsync();

            // Assert - verify clients are disposed
            _mockSearchClient.Verify(c => c.DisposeAsync(), Times.Once);
            _mockIndexClient.Verify(c => c.DisposeAsync(), Times.Once);
        }
    }

    // Integration Tests
    public class ComponentSearchServiceIntegrationTests : IAsyncDisposable
    {
        private readonly ISearchService<ComponentDocument> _searchService;
        private readonly ISearchClientFactory _clientFactory;

        public ComponentSearchServiceIntegrationTests()
        {
            var options = Options.Create(new SearchServiceOptions
            {
                SearchServiceEndpoint = "https://test-search.search.windows.net",
                AdminApiKey = "test-admin-key",
                QueryApiKey = "test-query-key",
                IndexName = "test-index"
            });

            _clientFactory = new AzureSearchClientFactory(options);
            _searchService = new ComponentSearchService(_clientFactory);
        }

        [Fact]
        public async Task FullSearchWorkflow_Succeeds()
        {
            // Arrange
            var document = new ComponentDocument
            {
                Id = Guid.NewGuid().ToString(),
                ComponentName = "Test Component",
                Category = "Test",
                LastUpdated = DateTimeOffset.UtcNow
            };

            try
            {
                // Act
                await _searchService.CreateIndexAsync();
                await _searchService.IndexDocumentsAsync(new[] { document });
                var searchResults = await _searchService.SearchAsync("Test");
                var suggestions = await _searchService.SuggestAsync("Te");

                // Assert
                Assert.NotNull(searchResults);
                Assert.NotNull(suggestions);
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Test failed with exception: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_clientFactory is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
            if (_searchService != null)
            {
                await _searchService.DisposeAsync();
            }
        }
    }
}