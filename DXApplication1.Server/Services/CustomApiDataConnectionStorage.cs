#nullable enable
using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Native.Json;
using DevExpress.DataAccess.Web;
using DevExpress.DataAccess.Wizard.Services;
using DXApplication1.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace DXApplication1.Services
{
    public class CustomApiDataConnectionStorage : IDataSourceWizardJsonConnectionStorage
    {
        private static readonly string _connectionsPath = "Data/api-connections.json";
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomApiDataConnectionStorage(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _baseUrl = (configuration["ReportingBaseUrl"] ?? "https://localhost:44369").TrimEnd('/');
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Extracts the Bearer token from the current HTTP request, if any.
        /// Returns null when called outside of an HTTP request context.
        /// </summary>
        public string? GetBearerToken()
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                return authHeader.Substring("Bearer ".Length).Trim();
            return null;
        }

        private List<JsonDataConnectionDescription> LoadConnections()
        {
            if (!File.Exists(_connectionsPath))
                return new List<JsonDataConnectionDescription>();

            var json = File.ReadAllText(_connectionsPath)
                .Replace("{baseUrl}", _baseUrl);
            return JsonSerializer.Deserialize<List<JsonDataConnectionDescription>>(json)
                ?? new List<JsonDataConnectionDescription>();
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
            var token = GetBearerToken();
            return LoadConnections().Select(x => CreateJsonDataConnection(x, token));
        }

        JsonDataConnection IJsonDataConnectionProviderService.GetJsonDataConnection(string name)
        {
            var connection = LoadConnections().FirstOrDefault(x => x.Name == name);
            if (connection == null)
                throw new KeyNotFoundException();
            return CreateJsonDataConnection(connection, GetBearerToken());
        }

        void IJsonConnectionStorageService.SaveConnection(string connectionName, JsonDataConnection dataConnection, bool saveCredentials)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Creates a <see cref="JsonDataConnection"/> from a <see cref="JsonDataConnectionDescription"/>.
        /// When <paramref name="bearerToken"/> is provided and the connection string specifies a URI,
        /// a <see cref="UriJsonSource"/> with an Authorization header is used so that the server-side
        /// DevExpress report engine forwards the caller's token when fetching JSON data.
        /// </summary>
        public static JsonDataConnection CreateJsonDataConnection(JsonDataConnectionDescription dataConnection, string? bearerToken = null)
        {
            if (!string.IsNullOrEmpty(bearerToken))
            {
                var (uri, _) = ParseConnectionString(dataConnection.ConnectionString);
                if (uri is not null)
                {
                    var uriSource = new UriJsonSource(uri);
                    uriSource.HeaderParameters.Add(new HeaderParameter("Authorization", $"Bearer {bearerToken}"));
                    return new JsonDataConnection(uriSource)
                    {
                        StoreConnectionNameOnly = true,
                        Name = dataConnection.Name
                    };
                }
            }

            // Fall back to plain connection string when no token is available or the
            // connection string is not a URI-based one.
            return new JsonDataConnection(dataConnection.ConnectionString)
            {
                StoreConnectionNameOnly = true,
                Name = dataConnection.Name
            };
        }

        /// <summary>
        /// Kept for backwards compatibility with any external callers.
        /// </summary>
        public static JsonDataConnection CreateJsonDataConnectionFromString(JsonDataConnectionDescription dataConnection)
            => CreateJsonDataConnection(dataConnection, bearerToken: null);

        /// <summary>
        /// Parses a DevExpress JSON connection string of the form
        /// <c>Uri=https://...;RootElement=items</c> and returns the absolute URI if found.
        /// The RootElement value is also returned but is typically empty in this codebase.
        /// </summary>
        private static (Uri? uri, string? rootElement) ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return (null, null);

            string? uriString = null;
            string? rootElement = null;

            foreach (var segment in connectionString.Split(';'))
            {
                var eq = segment.IndexOf('=');
                if (eq < 0) continue;
                var key = segment.Substring(0, eq).Trim();
                var value = segment.Substring(eq + 1).Trim();

                if (key.Equals("Uri", StringComparison.OrdinalIgnoreCase))
                    uriString = value;
                else if (key.Equals("RootElement", StringComparison.OrdinalIgnoreCase))
                    rootElement = value;
            }

            if (string.IsNullOrEmpty(uriString))
                return (null, rootElement);

            return Uri.TryCreate(uriString, UriKind.Absolute, out var uri)
                ? (uri, rootElement)
                : (null, rootElement);
        }
    }
}
