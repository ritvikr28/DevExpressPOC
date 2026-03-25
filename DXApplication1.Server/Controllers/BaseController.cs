using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DXApplication1.Server.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        private readonly string _metadataPath = "Data/columns-metadata.json";

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

            var metadataPath = "Data/columns-metadata.json";
            if (!System.IO.File.Exists(metadataPath))
                return NotFound("Metadata file not found.");

            using var metaStream = System.IO.File.OpenRead(metadataPath);
            var jsonDoc = await JsonDocument.ParseAsync(metaStream);

            if (!jsonDoc.RootElement.TryGetProperty(dataSourceName, out var columnsElement))
                return NotFound($"No columns found for '{dataSourceName}'.");

            // If columns is null or empty, return column names
            if (columns == null || columns.Length == 0)
            {
                var columnNames = columnsElement.EnumerateArray()
                    .Select(col => col.GetProperty("name").GetString())
                    .ToList();
                return Ok(columnNames);
            }

            // Otherwise, return filtered data
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

            var filtered = new List<Dictionary<string, object>>();
            foreach (var element in dataDoc.RootElement.EnumerateArray())
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
    }
}
