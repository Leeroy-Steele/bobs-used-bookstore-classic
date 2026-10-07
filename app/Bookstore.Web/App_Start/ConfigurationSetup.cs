using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using BobsBookstoreClassic.Data;
using Bookstore.Common;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Web
{
    public static class ConfigurationSetup
    {
        public static void InitializeConfiguration(IConfiguration configuration)
        {
            // Populate BookstoreConfiguration from IConfiguration.
            // IConfiguration uses ":" as separator; BookstoreConfiguration uses "/" - we convert.
            foreach (var setting in configuration.AsEnumerable())
            {
                if (setting.Value != null)
                {
                    var key = setting.Key.Replace(":", "/");
                    BookstoreConfiguration.AddSetting(key, setting.Value);
                }
            }

            // Also load connection strings explicitly
            var connStr = configuration.GetConnectionString("BookstoreDatabaseConnection");
            if (connStr != null)
            {
                BookstoreConfiguration.AddConnectionString("BookstoreDatabaseConnection", connStr);
            }
        }

        public static void ConfigureAwsSettings()
        {
            var rootPath = "/" + Constants.AppName;
            const string databasePath = "/Database";
            const string authenticationPath = "/Authentication";
            const string fileServicePath = "/Files";

            if (BookstoreConfiguration.TryGetSetting("Services/Database") == "aws")
            {
                using var client = new AmazonSimpleSystemsManagementClient();
                var request = new GetParameterRequest { Name = $"{rootPath}{databasePath}/ConnectionStrings/BookstoreDatabaseConnection" };
                var response = client.GetParameterAsync(request).GetAwaiter().GetResult();
                BookstoreConfiguration.AddConnectionString("BookstoreDatabaseConnection", response.Parameter.Value);
            }

            if (BookstoreConfiguration.TryGetSetting("Services/Authentication") == "aws")
            {
                using var client = new AmazonSimpleSystemsManagementClient();
                var request = new GetParametersByPathRequest { Path = $"{rootPath}{authenticationPath}/", Recursive = true };
                var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
                foreach (var parameter in response.Parameters)
                {
                    BookstoreConfiguration.AddSetting(parameter.Name.Replace($"{rootPath}/", string.Empty), parameter.Value);
                }
            }

            if (BookstoreConfiguration.TryGetSetting("Services/FileService") == "aws")
            {
                using var client = new AmazonSimpleSystemsManagementClient();
                var request = new GetParametersByPathRequest { Path = $"{rootPath}{fileServicePath}/", Recursive = true };
                var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
                foreach (var parameter in response.Parameters)
                {
                    BookstoreConfiguration.AddSetting(parameter.Name.Replace($"{rootPath}/", string.Empty), parameter.Value);
                }
            }
        }
    }
}
