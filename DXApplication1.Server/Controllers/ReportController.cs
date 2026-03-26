#nullable enable
using DevExpress.XtraReports.UI;
using DXApplication1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DXApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IAzureBlobStorageService _azureBlobStorageService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IAzureBlobStorageService azureBlobStorageService,
            ILogger<ReportController> logger)
        {
            _azureBlobStorageService = azureBlobStorageService;
            _logger = logger;
        }

        /// <summary>
        /// List all available reports from Azure Storage
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReportInfo>>> ListReports()
        {
            var azureReports = await _azureBlobStorageService.ListReportsAsync();
            var reports = azureReports.Select(name => new ReportInfo
            {
                Name = name,
                StorageLocation = "Azure",
                LastModified = null
            });

            return Ok(reports);
        }

        /// <summary>
        /// Generate multiple reports from templates with different parameters
        /// </summary>
        [HttpPost("generate-multiple")]
        public async Task<ActionResult<MultipleReportGenerationResult>> GenerateMultipleReports(
            [FromBody] MultipleReportGenerationRequest request)
        {
            if (request?.Reports == null || !request.Reports.Any())
            {
                return BadRequest(new { error = "No reports specified" });
            }

            var results = new List<ReportGenerationResult>();

            foreach (var reportRequest in request.Reports)
            {
                var result = new ReportGenerationResult
                {
                    ReportName = reportRequest.OutputName,
                    TemplateName = reportRequest.TemplateName
                };

                try
                {
                    // Load the template report from Azure
                    using var templateStream = await _azureBlobStorageService.DownloadReportAsync(reportRequest.TemplateName);
                    if (templateStream == null)
                    {
                        result.Success = false;
                        result.Error = $"Template '{reportRequest.TemplateName}' not found";
                        results.Add(result);
                        continue;
                    }

                    using var report = new XtraReport();
                    report.LoadLayoutFromXml(templateStream);

                    // Apply parameters if provided
                    if (reportRequest.Parameters != null)
                    {
                        foreach (var param in reportRequest.Parameters)
                        {
                            var reportParam = report.Parameters.Cast<DevExpress.XtraReports.Parameters.Parameter>()
                                .FirstOrDefault(p => p.Name == param.Key);
                            if (reportParam != null)
                            {
                                reportParam.Value = ConvertParameterValue(param.Value, reportParam.Type);
                            }
                        }
                    }

                    // Save the generated report to Azure
                    using var azureStream = new MemoryStream();
                    report.SaveLayoutToXml(azureStream);
                    azureStream.Position = 0;
                    var azureSuccess = await _azureBlobStorageService.UploadReportAsync(reportRequest.OutputName, azureStream);

                    result.Success = azureSuccess;
                    if (!azureSuccess)
                    {
                        result.Error = "Failed to save report to Azure Storage";
                    }

                    _logger.LogInformation("Generated report: {OutputName} from template: {TemplateName}",
                        reportRequest.OutputName, reportRequest.TemplateName);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Error = ex.Message;
                    _logger.LogError(ex, "Failed to generate report: {OutputName}", reportRequest.OutputName);
                }

                results.Add(result);
            }

            return Ok(new MultipleReportGenerationResult
            {
                TotalRequested = request.Reports.Count,
                SuccessCount = results.Count(r => r.Success),
                FailureCount = results.Count(r => !r.Success),
                Results = results
            });
        }

        /// <summary>
        /// Export a report to a specific format (PDF, XLSX, DOCX)
        /// </summary>
        [HttpPost("{reportName}/export")]
        public async Task<ActionResult> ExportReport(
            string reportName,
            [FromQuery] string format = "pdf")
        {
            using var reportStream = await _azureBlobStorageService.DownloadReportAsync(reportName);
            if (reportStream == null)
            {
                return NotFound(new { error = $"Report '{reportName}' not found" });
            }

            try
            {
                using var report = new XtraReport();
                report.LoadLayoutFromXml(reportStream);
                using var outputStream = new MemoryStream();
                string contentType;
                string extension;

                switch (format.ToLowerInvariant())
                {
                    case "pdf":
                        report.ExportToPdf(outputStream);
                        contentType = "application/pdf";
                        extension = "pdf";
                        break;
                    case "xlsx":
                    case "excel":
                        report.ExportToXlsx(outputStream);
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        extension = "xlsx";
                        break;
                    case "docx":
                    case "word":
                        report.ExportToDocx(outputStream);
                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        extension = "docx";
                        break;
                    case "html":
                        report.ExportToHtml(outputStream);
                        contentType = "text/html";
                        extension = "html";
                        break;
                    default:
                        return BadRequest(new { error = $"Unsupported format: {format}. Supported formats: pdf, xlsx, docx, html" });
                }

                return File(outputStream.ToArray(), contentType, $"{reportName}.{extension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export report: {ReportName}", reportName);
                return StatusCode(500, new { error = "Failed to export report" });
            }
        }

        /// <summary>
        /// Delete a report from Azure Storage
        /// </summary>
        [HttpDelete("{reportName}/azure")]
        public async Task<ActionResult> DeleteFromAzure(string reportName)
        {
            var success = await _azureBlobStorageService.DeleteReportAsync(reportName);
            if (success)
            {
                return Ok(new { message = $"Report '{reportName}' deleted from Azure Storage" });
            }
            return StatusCode(500, new { error = "Failed to delete report from Azure Storage" });
        }

        private static object? ConvertParameterValue(object value, Type targetType)
        {
            if (value == null) return null;

            try
            {
                if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                {
                    // Use InvariantCulture for consistent parsing across different cultures
                    return DateTime.Parse(value.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (targetType == typeof(int) || targetType == typeof(int?))
                {
                    return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                {
                    return Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (targetType == typeof(bool) || targetType == typeof(bool?))
                {
                    return Convert.ToBoolean(value);
                }
                return value.ToString();
            }
            catch (FormatException)
            {
                // Return the string value if conversion fails
                return value.ToString();
            }
        }
    }

    #region DTOs

    public class ReportInfo
    {
        public string Name { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public DateTime? LastModified { get; set; }
    }

    public class MultipleReportGenerationRequest
    {
        public List<ReportGenerationRequestItem> Reports { get; set; } = new();
    }

    public class ReportGenerationRequestItem
    {
        public string TemplateName { get; set; } = string.Empty;
        public string OutputName { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
    }

    public class MultipleReportGenerationResult
    {
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ReportGenerationResult> Results { get; set; } = new();
    }

    public class ReportGenerationResult
    {
        public string ReportName { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    #endregion
}
