using DevExpress.XtraReports.UI;
using DXApplication1.PredefinedReports;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXApplication1.Services
{
    public class CustomReportStorageWebExtension : DevExpress.XtraReports.Web.Extensions.ReportStorageWebExtension
    {
        private readonly string reportsDirectory = Path.Combine("Data", "Reports");

        public CustomReportStorageWebExtension()
        {
            if (!Directory.Exists(reportsDirectory))
                Directory.CreateDirectory(reportsDirectory);
        }

        public override bool CanSetData(string url) => true;

        public override bool IsValidUrl(string url) => !string.IsNullOrWhiteSpace(url);

        public override byte[] GetData(string url)
        {
            var filePath = GetReportFilePath(url);
            if (File.Exists(filePath))
                return File.ReadAllBytes(filePath);

            if (ReportsFactory.Reports.ContainsKey(url))
            {
                using var ms = new MemoryStream();
                using XtraReport report = ReportsFactory.Reports[url]();
                report.SaveLayoutToXml(ms);
                return ms.ToArray();
            }
            throw new DevExpress.XtraReports.Web.ClientControls.FaultException($"Could not find report '{url}'.");
        }

        public override Dictionary<string, string> GetUrls()
        {
            var fileReports = Directory.Exists(reportsDirectory)
                ? Directory.GetFiles(reportsDirectory, "*.repx")
                    .Select(Path.GetFileNameWithoutExtension)
                : Enumerable.Empty<string>();

            return fileReports
                .Union(ReportsFactory.Reports.Select(x => x.Key))
                .ToDictionary(x => x, x => x);
        }

        public override void SetData(XtraReport report, string url)
        {
            var filePath = GetReportFilePath(url);
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            report.SaveLayoutToXml(stream);
        }

        public override string SetNewData(XtraReport report, string defaultUrl)
        {
            SetData(report, defaultUrl);
            return defaultUrl;
        }

        private string GetReportFilePath(string url)
        {
            var safeUrl = Path.GetFileNameWithoutExtension(url);
            return Path.Combine(reportsDirectory, $"{safeUrl}.repx");
        }
    }
}