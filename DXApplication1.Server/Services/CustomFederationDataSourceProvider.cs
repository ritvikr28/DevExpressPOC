#nullable enable
using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Web;
using DXApplication1.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DXApplication1.Services
{
    /// <summary>
    /// Provides Federated Data Source support for DevExpress reports.
    /// This enables JOIN operations between multiple JSON data sources.
    /// 
    /// IMPORTANT: For Data Federation to work with JOINs:
    /// 1. This factory must be registered with RegisterFederationDataSourceProviderFactory
    /// 2. The JSON data sources must be properly resolved through CustomApiDataConnectionStorage
    /// 3. Data must be pre-loaded before federation queries can run
    /// </summary>
    public class CustomFederationDataSourceProviderFactory : IFederationDataSourceProviderFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomFederationDataSourceProviderFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public IFederationDataSourceProviderService Create()
        {
            var connectionStorage = new CustomApiDataConnectionStorage(_configuration, _httpContextAccessor);
            return new CustomFederationDataSourceProvider(connectionStorage);
        }
    }

    /// <summary>
    /// Implements IFederationDataSourceProviderService to enable Data Federation with JOIN support.
    /// This service resolves JSON data connections for use in federated queries.
    /// When DevExpress encounters a FederationDataSource with queries referencing JSON connections,
    /// it calls this service to resolve those connections.
    /// </summary>
    public class CustomFederationDataSourceProvider : IFederationDataSourceProviderService
    {
        private readonly CustomApiDataConnectionStorage _connectionStorage;

        public CustomFederationDataSourceProvider(CustomApiDataConnectionStorage connectionStorage)
        {
            _connectionStorage = connectionStorage;
        }

        /// <summary>
        /// Gets a JSON data connection by name for use in federation queries.
        /// The connection is resolved from the registered connection storage and
        /// includes authorization headers when available.
        /// </summary>
        public JsonDataConnection GetJsonDataConnection(string name)
        {
            var connections = _connectionStorage.GetConnections();
            var connection = connections.FirstOrDefault(c => c.Name == name);
            if (connection == null)
                throw new InvalidOperationException($"JSON data connection '{name}' not found in registered connections.");
            
            var token = _connectionStorage.GetBearerToken();
            return CustomApiDataConnectionStorage.CreateJsonDataConnection(connection, token);
        }
    }
}
