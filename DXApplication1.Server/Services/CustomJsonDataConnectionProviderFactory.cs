using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Web;
using DXApplication1.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DXApplication1.Services
{
    public class CustomJsonDataConnectionProviderFactory : IJsonDataConnectionProviderFactory
    {
        private readonly IConfiguration _configuration;

        public CustomJsonDataConnectionProviderFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IJsonDataConnectionProviderService Create()
        {
            var storage = new CustomApiDataConnectionStorage(_configuration);
            return new WebDocumentViewerJsonDataConnectionProvider(storage.GetConnections());
        }
    }

    public class WebDocumentViewerJsonDataConnectionProvider : IJsonDataConnectionProviderService
    {
        readonly IEnumerable<JsonDataConnectionDescription> jsonDataConnections;
        public WebDocumentViewerJsonDataConnectionProvider(IEnumerable<JsonDataConnectionDescription> jsonDataConnections)
        {
            this.jsonDataConnections = jsonDataConnections;
        }
        public JsonDataConnection GetJsonDataConnection(string name)
        {
            var connection = jsonDataConnections.FirstOrDefault(x => x.Name == name);
            if (connection == null)
                throw new InvalidOperationException($"Connection '{name}' not found.");
            return CustomApiDataConnectionStorage.CreateJsonDataConnectionFromString(connection);
        }
    }
}
