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
        private readonly string _reportsDirectory = Path.Combine("Data", "Reports");

        public ReportController(
            IAzureBlobStorageService azureBlobStorageService,
            ILogger<ReportController> logger)
        {
            _azureBlobStorageService = azureBlobStorageService;
            _logger = logger;
        }

        /// <summary>
        /// List all available reports from both local storage and Azure
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReportInfo>>> ListReports()
        {
            var reports = new List<ReportInfo>();

            // Get local reports
            if (Directory.Exists(_reportsDirectory))
            {
                var localReports = Directory.GetFiles(_reportsDirectory, "*.repx")
                    .Select(f => new ReportInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        StorageLocation = "Local",
                        LastModified = System.IO.File.GetLastWriteTimeUtc(f)
                    });
                reports.AddRange(localReports);
            }

            // Get Azure reports
            if (_azureBlobStorageService.IsEnabled)
            {
                var azureReports = await _azureBlobStorageService.ListReportsAsync();
                foreach (var reportName in azureReports)
                {
                    // Check if not already in local list
                    if (!reports.Any(r => r.Name == reportName))
                    {
                        reports.Add(new ReportInfo
                        {
                            Name = reportName,
                            StorageLocation = "Azure",
                            LastModified = null
                        });
                    }
                    else
                    {
                        // Mark as both local and Azure
                        var existing = reports.First(r => r.Name == reportName);
                        existing.StorageLocation = "Both";
                    }
                }
            }

            return Ok(reports);
        }

        /// <summary>
        /// Save a report to Azure Storage
        /// </summary>
        [HttpPost("{reportName}/save-to-azure")]
        [Authorize]
        public async Task<ActionResult> SaveToAzure(string reportName)
        {
            if (!_azureBlobStorageService.IsEnabled)
            {
                return BadRequest(new { error = "Azure Storage is not configured" });
            }

            var filePath = GetReportFilePath(reportName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = $"Report '{reportName}' not found locally" });
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var success = await _azureBlobStorageService.UploadReportAsync(reportName, stream);

            if (success)
            {
                return Ok(new { message = $"Report '{reportName}' saved to Azure Storage" });
            }
            return StatusCode(500, new { error = "Failed to save report to Azure Storage" });
        }

        /// <summary>
        /// Generate multiple reports from templates with different parameters
        /// </summary>
        [HttpPost("generate-multiple")]
        [Authorize]
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
                    // Load the template report
                    var templatePath = GetReportFilePath(reportRequest.TemplateName);
                    if (!System.IO.File.Exists(templatePath))
                    {
                        result.Success = false;
                        result.Error = $"Template '{reportRequest.TemplateName}' not found";
                        results.Add(result);
                        continue;
                    }

                    using var report = new XtraReport();
                    using var templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
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

                    // Save the generated report
                    var outputPath = GetReportFilePath(reportRequest.OutputName);
                    using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                    report.SaveLayoutToXml(outputStream);

                    result.Success = true;
                    result.LocalPath = outputPath;

                    // Also save to Azure if requested
                    if (request.SaveToAzure && _azureBlobStorageService.IsEnabled)
                    {
                        using var azureStream = new MemoryStream();
                        report.SaveLayoutToXml(azureStream);
                        azureStream.Position = 0;
                        var azureSuccess = await _azureBlobStorageService.UploadReportAsync(reportRequest.OutputName, azureStream);
                        result.SavedToAzure = azureSuccess;
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
            [FromQuery] string format = "pdf",
            [FromQuery] bool saveToAzure = false)
        {
            var filePath = GetReportFilePath(reportName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = $"Report '{reportName}' not found" });
            }

            try
            {
                using var report = new XtraReport();
                using var templateStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                report.LoadLayoutFromXml(templateStream);

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

                outputStream.Position = 0;

                // Save to Azure if requested
                if (saveToAzure && _azureBlobStorageService.IsEnabled)
                {
                    var exportedFileName = $"{reportName}_exported.{extension}";
                    await _azureBlobStorageService.UploadReportAsync(exportedFileName, outputStream);
                    outputStream.Position = 0;
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
        [Authorize]
        public async Task<ActionResult> DeleteFromAzure(string reportName)
        {
            if (!_azureBlobStorageService.IsEnabled)
            {
                return BadRequest(new { error = "Azure Storage is not configured" });
            }

            var success = await _azureBlobStorageService.DeleteReportAsync(reportName);
            if (success)
            {
                return Ok(new { message = $"Report '{reportName}' deleted from Azure Storage" });
            }
            return StatusCode(500, new { error = "Failed to delete report from Azure Storage" });
        }

        /// <summary>
        /// Check Azure Storage status
        /// </summary>
        [HttpGet("azure-status")]
        public ActionResult<AzureStorageStatus> GetAzureStatus()
        {
            return Ok(new AzureStorageStatus
            {
                IsEnabled = _azureBlobStorageService.IsEnabled
            });
        }

        private string GetReportFilePath(string reportName)
        {
            var safeReportName = Path.GetFileNameWithoutExtension(reportName);
            return Path.Combine(_reportsDirectory, $"{safeReportName}.repx");
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
        public bool SaveToAzure { get; set; }
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
        public string? LocalPath { get; set; }
        public bool SavedToAzure { get; set; }
    }

    public class AzureStorageStatus
    {
        public bool IsEnabled { get; set; }
    }

    #endregion
}
