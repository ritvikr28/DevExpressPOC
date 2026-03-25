using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Native.Json;
using DevExpress.DataAccess.Web;
using DevExpress.DataAccess.Wizard.Services;
using DXApplication1.Data;
using System.Text.Json;

namespace DXApplication1.Services
{
    public class CustomApiDataConnectionStorage : IDataSourceWizardJsonConnectionStorage
    {
        private static readonly string _connectionsPath = "Data/api-connections.json";
        private static List<JsonDataConnectionDescription> _apiConnections;

        private static List<JsonDataConnectionDescription> LoadConnections()
        {
            if (_apiConnections != null)
                return _apiConnections;

            if (!File.Exists(_connectionsPath))
                return new List<JsonDataConnectionDescription>();

            var json = File.ReadAllText(_connectionsPath);
            _apiConnections = JsonSerializer.Deserialize<List<JsonDataConnectionDescription>>(json)
                ?? new List<JsonDataConnectionDescription>();
            return _apiConnections;
        }

        public List<JsonDataConnectionDescription> GetConnections()
        {
            return LoadConnections();
        }

        bool IJsonConnectionStorageService.CanSaveConnection => false;

        bool IJsonConnectionStorageService.ContainsConnection(string connectionName)
        {
            return LoadConnections().Any(x => x.Name == connectionName);
        }

        IEnumerable<JsonDataConnection> IJsonConnectionStorageService.GetConnections()
        {
            return LoadConnections().Select(x => CreateJsonDataConnectionFromString(x));
        }

        JsonDataConnection IJsonDataConnectionProviderService.GetJsonDataConnection(string name)
        {
            var connection = LoadConnections().FirstOrDefault(x => x.Name == name);
            if (connection == null)
                throw new KeyNotFoundException();
            return CreateJsonDataConnectionFromString(connection);
        }

        void IJsonConnectionStorageService.SaveConnection(string connectionName, JsonDataConnection dataConnection, bool saveCredentials)
        {
            throw new System.NotSupportedException();
        }

        public static JsonDataConnection CreateJsonDataConnectionFromString(JsonDataConnectionDescription dataConnection)
        {
            return new JsonDataConnection(dataConnection.ConnectionString)
            {
                StoreConnectionNameOnly = true,
                Name = dataConnection.Name,
            };
        }
    }
}