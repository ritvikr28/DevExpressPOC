#nullable enable
using DXApplication1.Server.Models;
using ESS.Platform.Authorization.Attributes;
using ESS.Platform.Authorization.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DXApplication1.Server.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private static readonly string ColumnsMetadataPath = "Data/columns-metadata.json";

        /// <summary>
        /// Maps data source name to the corresponding JSON data file path (case-insensitive).
        /// </summary>
        private static string? GetDataFilePath(string dataSourceName)
        {
            return dataSourceName.ToLowerInvariant() switch
            {
                "pupil" => "Data/pupil-data.json",
                "staff" => "Data/staff-data.json",
                "assessment" => "Data/assessment-data.json",
                _ => null
            };
        }

        /// <summary>
        /// Parses a JSON element value to a CLR type.
        /// </summary>
        private static object? ParseJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => element.ToString()
            };
        }

        /// <summary>
        /// Parses column metadata from a JSON array element.
        /// </summary>
        private static List<ColumnMetadata> ParseColumnMetadata(JsonElement columnsArray)
        {
            var columns = new List<ColumnMetadata>();
            foreach (var col in columnsArray.EnumerateArray())
            {
                columns.Add(new ColumnMetadata
                {
                    Name = col.GetProperty("name").GetString() ?? string.Empty,
                    Type = col.GetProperty("type").GetString() ?? string.Empty
                });
            }
            return columns;
        }

        /// <summary>
        /// Checks if the columns metadata file exists and returns an appropriate error result if not.
        /// </summary>
        private IActionResult? ValidateMetadataFileExists()
        {
            if (!System.IO.File.Exists(ColumnsMetadataPath))
                return NotFound("Columns metadata file not found.");
            return null;
        }

        /// <summary>
        /// Retrieves data from a single source with optional column filtering.
        /// </summary>
        private async Task<(List<Dictionary<string, object?>>? data, string? error)> GetDataFromSourceAsync(
            string dataSourceName,
            IEnumerable<string>? columns)
        {
            var fileName = GetDataFilePath(dataSourceName);
            if (fileName == null || !System.IO.File.Exists(fileName))
                return (null, $"Data file not found for data source '{dataSourceName}'.");

            using var dataStream = System.IO.File.OpenRead(fileName);
            var dataDoc = await JsonDocument.ParseAsync(dataStream);

            if (dataDoc.RootElement.ValueKind != JsonValueKind.Array)
                return (null, $"Data source '{dataSourceName}' does not contain array data.");

            var columnSet = columns != null && columns.Any()
                ? new HashSet<string>(columns, StringComparer.Ordinal)
                : null;

            var result = new List<Dictionary<string, object?>>();
            foreach (var element in dataDoc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, object?>();
                var props = columnSet != null
                    ? element.EnumerateObject().Where(p => columnSet.Contains(p.Name))
                    : element.EnumerateObject();

                foreach (var prop in props)
                {
                    dict[prop.Name] = ParseJsonValue(prop.Value);
                }
                result.Add(dict);
            }

            return (result, null);
        }

        /// <summary>
        /// Serves JSON data for a named data source.
        /// Referenced by Data/api-connections.json connection strings and called by DevExpress
        /// server-side infrastructure when rendering reports in both the designer and viewer.
        /// </summary>
        [HttpGet]
        [Route("data")]
        [SecurityDomain(["NG.Homepage.Access"], Operation.View)]
        public async Task<IActionResult> GetData(
            [FromQuery] string dataSourceName,
            [FromQuery] string[] columns)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return BadRequest("dataSourceName is required.");

            var (data, error) = await GetDataFromSourceAsync(dataSourceName, columns);
            if (error != null)
                return NotFound(error);

            return Ok(data);
        }

        /// <summary>
        /// Lists all available data sources with their schemas (column metadata only, no data).
        /// </summary>
        [HttpGet]
        [Route("data/sources")]
        [SecurityDomain(["NG.Homepage.Access"], Operation.View)]
        public async Task<IActionResult> GetDataSources()
        {
            var validationError = ValidateMetadataFileExists();
            if (validationError != null)
                return validationError;

            using var stream = System.IO.File.OpenRead(ColumnsMetadataPath);
            var metadataDoc = await JsonDocument.ParseAsync(stream);

            var dataSources = new List<DataSourceSchema>();

            foreach (var prop in metadataDoc.RootElement.EnumerateObject())
            {
                var schema = new DataSourceSchema
                {
                    Name = prop.Name,
                    Columns = ParseColumnMetadata(prop.Value)
                };

                dataSources.Add(schema);
            }

            return Ok(new DataSourcesListResponse { DataSources = dataSources });
        }

        /// <summary>
        /// Returns the schema (column names and types) for a specific data source without loading the actual data.
        /// </summary>
        [HttpGet]
        [Route("data/schema")]
        [SecurityDomain(["NG.Homepage.Access"], Operation.View)]
        public async Task<IActionResult> GetSchema([FromQuery] string dataSourceName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return BadRequest("dataSourceName is required.");

            var validationError = ValidateMetadataFileExists();
            if (validationError != null)
                return validationError;

            using var stream = System.IO.File.OpenRead(ColumnsMetadataPath);
            var metadataDoc = await JsonDocument.ParseAsync(stream);

            // Find the matching data source (case-insensitive, consistent with GetDataFilePath)
            foreach (var prop in metadataDoc.RootElement.EnumerateObject())
            {
                if (string.Equals(prop.Name, dataSourceName, StringComparison.OrdinalIgnoreCase))
                {
                    var schema = new DataSourceSchema
                    {
                        Name = prop.Name,
                        Columns = ParseColumnMetadata(prop.Value)
                    };

                    return Ok(schema);
                }
            }

            return NotFound($"Schema not found for data source '{dataSourceName}'.");
        }

        /// <summary>
        /// Fetches data from multiple data sources with column selection.
        /// This is a POST endpoint that accepts a request body specifying which data sources
        /// and which columns to retrieve.
        /// </summary>
        /// <example>
        /// Request body example:
        /// {
        ///   "sources": [
        ///     { "dataSourceName": "Pupil", "columns": ["PupilId", "FirstName"] },
        ///     { "dataSourceName": "Staff", "columns": ["StaffId", "Role"] }
        ///   ]
        /// }
        /// </example>
        [HttpPost]
        [Route("data/multi")]
        [SecurityDomain(["NG.Homepage.Access"], Operation.View)]
        public async Task<IActionResult> GetMultiSourceData([FromBody] MultiSourceDataRequest request)
        {
            if (request.Sources == null || request.Sources.Count == 0)
                return BadRequest("At least one data source must be specified.");

            var response = new MultiSourceDataResponse();

            foreach (var source in request.Sources)
            {
                if (string.IsNullOrWhiteSpace(source.DataSourceName))
                {
                    response.Results.Add(new DataSourceResult
                    {
                        DataSourceName = source.DataSourceName ?? string.Empty,
                        Error = "Data source name is required."
                    });
                    continue;
                }

                var (data, error) = await GetDataFromSourceAsync(source.DataSourceName, source.Columns);
                
                response.Results.Add(new DataSourceResult
                {
                    DataSourceName = source.DataSourceName,
                    Data = data ?? new List<Dictionary<string, object?>>(),
                    Error = error
                });
            }

            return Ok(response);
        }
    }
}
