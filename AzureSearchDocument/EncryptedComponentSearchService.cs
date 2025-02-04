using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace MilitaryElectronics.Search
{
    public interface IEncryptionService
    {
        Task<string> EncryptAsync(string plainText, string purpose);
        Task<string> DecryptAsync(string cipherText, string purpose);
        string GenerateHash(string input);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly string _encryptionKey;

        public EncryptionService(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<SearchServiceOptions> options)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _encryptionKey = options.Value.EncryptionKey;
        }

        public async Task<string> EncryptAsync(string plainText, string purpose)
        {
            var protector = _dataProtectionProvider.CreateProtector(purpose);
            
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            aes.GenerateIV();

            using var ms = new MemoryStream();
            await ms.WriteAsync(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(
                ms, 
                aes.CreateEncryptor(), 
                CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cryptoStream))
            {
                await writer.WriteAsync(plainText);
            }

            var encryptedData = ms.ToArray();
            return protector.Protect(Convert.ToBase64String(encryptedData));
        }

        public async Task<string> DecryptAsync(string cipherText, string purpose)
        {
            var protector = _dataProtectionProvider.CreateProtector(purpose);
            var encryptedData = Convert.FromBase64String(protector.Unprotect(cipherText));

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);

            var iv = new byte[aes.IV.Length];
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var ms = new MemoryStream(
                encryptedData, 
                iv.Length, 
                encryptedData.Length - iv.Length);
            using var cryptoStream = new CryptoStream(
                ms, 
                aes.CreateDecryptor(), 
                CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);

            return await reader.ReadToEndAsync();
        }

        public string GenerateHash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
    }

    public class EncryptedComponentDocument : ComponentDocument
    {
        public string EncryptedData { get; set; }
        public string DataHash { get; set; }
    }

    public class EncryptedComponentSearchService : InjectedComponentSearchService
    {
        private readonly IEncryptionService _encryptionService;
        protected const string EncryptionPurpose = "ComponentSearch";

        public EncryptedComponentSearchService(
            SearchClient searchClient,
            SearchIndexClient indexClient,
            ISearchIndexDefinitionService indexDefinitionService,
            IOptions<SearchServiceOptions> options,
            ILogger<EncryptedComponentSearchService> logger,
            IEncryptionService encryptionService)
            : base(searchClient, indexClient, indexDefinitionService, options, logger)
        {
            _encryptionService = encryptionService;
        }

        protected override async Task OnBeforeIndexDocumentsAsync(
            IReadOnlyList<ComponentDocument> documents,
            CancellationToken cancellationToken)
        {
            await base.OnBeforeIndexDocumentsAsync(documents, cancellationToken);

            foreach (var doc in documents)
            {
                if (doc is EncryptedComponentDocument encDoc)
                {
                    var sensitiveData = new
                    {
                        doc.Manufacturer,
                        doc.MilSpecLevel,
                        doc.PerformanceMetrics
                    };

                    var jsonData = System.Text.Json.JsonSerializer.Serialize(sensitiveData);
                    encDoc.EncryptedData = await _encryptionService.EncryptAsync(
                        jsonData, 
                        EncryptionPurpose);
                    encDoc.DataHash = _encryptionService.GenerateHash(jsonData);
                }
            }
        }

        protected override async Task OnAfterSearchAsync(
            string searchText,
            SearchOptions options,
            SearchResults<ComponentDocument> results,
            CancellationToken cancellationToken)
        {
            await base.OnAfterSearchAsync(searchText, options, results, cancellationToken);

            await foreach (var result in results.GetResultsAsync())
            {
                if (result.Document is EncryptedComponentDocument encDoc)
                {
                    var decryptedJson = await _encryptionService.DecryptAsync(
                        encDoc.EncryptedData, 
                        EncryptionPurpose);
                    
                    var hash = _encryptionService.GenerateHash(decryptedJson);
                    if (hash != encDoc.DataHash)
                    {
                        Logger.LogError("Data integrity check failed for document {id}", encDoc.Id);
                        continue;
                    }

                    var sensitiveData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(decryptedJson);
                    // Populate decrypted data back to the document
                    // Implementation depends on your specific needs
                }
            }
        }

        protected override SearchOptions CreateDefaultSearchOptions()
        {
            var options = base.CreateDefaultSearchOptions();
            options.Select.Add(nameof(EncryptedComponentDocument.EncryptedData));
            options.Select.Add(nameof(EncryptedComponentDocument.DataHash));
            return options;
        }
    }

    // Configuration extension
    public static class EncryptionServiceCollectionExtensions
    {
        public static IServiceCollection AddSearchEncryption(
            this IServiceCollection services,
            Action<SearchServiceOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddDataProtection();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.Replace(ServiceDescriptor.Scoped<ISearchService<ComponentDocument>, 
                EncryptedComponentSearchService>());

            return services;
        }
    }

    // Usage example
    public class SearchServiceExample
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSearchEncryption(options =>
            {
                options.SearchServiceEndpoint = "https://your-service.search.windows.net";
                options.AdminApiKey = "your-admin-key";
                options.QueryApiKey = "your-query-key";
                options.IndexName = "encrypted-components-index";
                options.EncryptionKey = Convert.ToBase64String(
                    System.Security.Cryptography.Aes.Create().Key);
            });
        }
    }
}
