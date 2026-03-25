using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DXApplication1.Server.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        private readonly string _metadataPath = "Data/columns-metadata.json";
        private readonly string _connectionsPath = "Data/api-connections.json";

        /// <summary>
        /// Gets a list of all available data sources.
        /// </summary>
        [HttpGet("datasources")]
        public IActionResult GetDataSources()
        {
            if (!System.IO.File.Exists(_connectionsPath))
                return NotFound("Connections file not found.");

            var json = System.IO.File.ReadAllText(_connectionsPath);
            using var jsonDoc = JsonDocument.Parse(json);

            var dataSources = jsonDoc.RootElement.EnumerateArray()
                .Select(item => item.GetProperty("Name").GetString())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            return Ok(dataSources);
        }

        /// <summary>
        /// Gets the schema (column metadata) for a specific data source.
        /// </summary>
        [HttpGet("schema")]
        public async Task<IActionResult> GetSchema([FromQuery] string dataSourceName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return BadRequest("dataSourceName is required.");

            if (!System.IO.File.Exists(_metadataPath))
                return NotFound("Metadata file not found.");

            await using var stream = System.IO.File.OpenRead(_metadataPath);
            var jsonDoc = await JsonDocument.ParseAsync(stream);

            if (!jsonDoc.RootElement.TryGetProperty(dataSourceName, out var columnsElement))
                return NotFound($"No schema found for '{dataSourceName}'.");

            var columns = columnsElement.EnumerateArray()
                .Select(col => new
                {
                    Name = col.GetProperty("name").GetString(),
                    Type = col.GetProperty("type").GetString()
                })
                .ToList();

            return Ok(columns);
        }

        [HttpGet("column")]
        public async Task<IActionResult> GetColumns([FromQuery] string dataSourceName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return BadRequest("columnName is required.");

            if (!System.IO.File.Exists(_metadataPath))
                return NotFound("Metadata file not found.");

            using var stream = System.IO.File.OpenRead(_metadataPath);
            var jsonDoc = await JsonDocument.ParseAsync(stream);

            if (!jsonDoc.RootElement.TryGetProperty(dataSourceName, out var columnsElement))
                return NotFound($"No columns found for '{dataSourceName}'.");

            var columnNames = columnsElement.EnumerateArray()
                .Select(col => col.GetProperty("name").GetString())
                .ToList();

            return Ok(columnNames);
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetDetails([FromQuery] string dataSourceName, [FromQuery] string[] columns)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return BadRequest("dataSourceName is required.");
            if (columns == null || columns.Length == 0)
                return BadRequest("At least one column must be specified.");

            // Map dataSourceName to file name
            var fileName = dataSourceName.ToLower() switch
            {
                "pupil" => "Data/pupil-data.json",
                "staff" => "Data/staff-data.json",
                "assessment" => "Data/assessment-data.json",
                _ => null
            };

            if (fileName == null || !System.IO.File.Exists(fileName))
                return NotFound("Data file not found for the specified dataSourceName.");

            using var stream = System.IO.File.OpenRead(fileName);
            var jsonDoc = await JsonDocument.ParseAsync(stream);

            if (jsonDoc.RootElement.ValueKind != JsonValueKind.Array)
                return NotFound("No data found.");

            var filtered = new List<Dictionary<string, object>>();

            foreach (var element in jsonDoc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, object>();
                foreach (var col in columns)
                {
                    if (element.TryGetProperty(col, out var value))
                    {
                        dict[col] = value.ValueKind switch
                        {
                            JsonValueKind.String => value.GetString(),
                            JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => value.ToString()
                        };
                    }
                }
                filtered.Add(dict);
            }

            return Ok(filtered);
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetData(
            [FromQuery] string dataSourceName,
            [FromQuery] string[] columns)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return BadRequest("dataSourceName is required.");

            var fileName = dataSourceName.ToLower() switch
            {
                "pupil" => "Data/pupil-data.json",
                "staff" => "Data/staff-data.json",
                "assessment" => "Data/assessment-data.json",
                _ => null
            };

            if (fileName == null || !System.IO.File.Exists(fileName))
                return NotFound("Data file not found for the specified dataSourceName.");

            using var dataStream = System.IO.File.OpenRead(fileName);
            var dataDoc = await JsonDocument.ParseAsync(dataStream);

            if (dataDoc.RootElement.ValueKind != JsonValueKind.Array)
                return NotFound("No data found.");

            // When no columns are specified, return all fields so DevExpress can infer the schema.
            var columnSet = columns != null && columns.Length > 0
                ? new HashSet<string>(columns, StringComparer.Ordinal)
                : null;

            var filtered = new List<Dictionary<string, object>>();
            foreach (var element in dataDoc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, object>();
                var props = columnSet != null
                    ? element.EnumerateObject().Where(p => columnSet.Contains(p.Name))
                    : element.EnumerateObject();

                foreach (var prop in props)
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.ToString()
                    };
                }
                filtered.Add(dict);
            }

            return Ok(filtered);
        }
    }
}
