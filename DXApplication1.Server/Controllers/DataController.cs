#nullable enable
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
        /// <summary>
        /// Serves JSON data for a named data source.
        /// Referenced by Data/api-connections.json connection strings and called by DevExpress
        /// server-side infrastructure when rendering reports in both the designer and viewer.
        /// </summary>
        [HttpGet("data")]
        [SecurityDomain(["NG.Homepage.Access1"], Operation.View)]
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

            var filtered = new List<Dictionary<string, object?>>();
            foreach (var element in dataDoc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, object?>();
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
