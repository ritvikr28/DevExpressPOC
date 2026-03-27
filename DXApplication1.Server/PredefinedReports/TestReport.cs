using DevExpress.DataAccess.Json;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.Parameters;
using System.Drawing;

namespace DXApplication1.PredefinedReports
{
    /// <summary>
    /// A report that demonstrates dynamic column selection from the UI.
    /// This report uses all 3 data sources (Pupil, Staff, Assessment) and allows
    /// users to specify which columns to fetch via report parameters.
    /// 
    /// Parameters:
    /// - PupilColumns: Comma-separated list of columns (e.g., "PupilId,FirstName,LastName")
    /// - StaffColumns: Comma-separated list of columns (e.g., "StaffId,FirstName,Role")
    /// - AssessmentColumns: Comma-separated list of columns (e.g., "AssessmentId,Subject,Score")
    /// 
    /// When previewed in the Report Designer or Viewer, users can fill in these parameters
    /// to fetch only the desired columns from each data source.
    /// </summary>
    public partial class TestReport : XtraReport
    {
        public TestReport()
        {
            InitializeComponent();
            SetupDynamicDataSources();
        }

        /// <summary>
        /// Sets up report parameters for dynamic column selection and configures
        /// all 3 data sources (Pupil, Staff, Assessment) with StoreConnectionNameOnly pattern.
        /// </summary>
        private void SetupDynamicDataSources()
        {
            // ======================================================================
            // REPORT PARAMETERS for dynamic column selection
            // These parameters allow the UI to specify which columns to fetch
            // ======================================================================
            
            // Pupil columns parameter - default to all columns
            var pupilColumnsParam = new Parameter
            {
                Name = "PupilColumns",
                Description = "Columns to fetch for Pupil data (comma-separated)",
                Type = typeof(string),
                Value = "PupilId,FirstName,LastName,DateOfBirth,Class",
                Visible = true
            };
            
            // Staff columns parameter
            var staffColumnsParam = new Parameter
            {
                Name = "StaffColumns", 
                Description = "Columns to fetch for Staff data (comma-separated)",
                Type = typeof(string),
                Value = "StaffId,FirstName,LastName,Role,Department",
                Visible = true
            };
            
            // Assessment columns parameter
            var assessmentColumnsParam = new Parameter
            {
                Name = "AssessmentColumns",
                Description = "Columns to fetch for Assessment data (comma-separated)",
                Type = typeof(string),
                Value = "AssessmentId,PupilId,Subject,Score,Date",
                Visible = true
            };

            this.Parameters.AddRange(new Parameter[] { pupilColumnsParam, staffColumnsParam, assessmentColumnsParam });

            // ======================================================================
            // DATA SOURCES - Using Dynamic connections with parameter-based columns
            // These connections use {?ParameterName} syntax for dynamic column filtering
            // ======================================================================

            // Create Pupil data source - uses PupilDynamic connection with PupilColumns parameter
            var pupilDataSource = new JsonDataSource
            {
                Name = "PupilDataSource",
                ConnectionName = "PupilDynamic"
            };

            // Create Staff data source - uses StaffDynamic connection with StaffColumns parameter
            var staffDataSource = new JsonDataSource
            {
                Name = "StaffDataSource",
                ConnectionName = "StaffDynamic"
            };

            // Create Assessment data source - uses AssessmentDynamic connection with AssessmentColumns parameter
            var assessmentDataSource = new JsonDataSource
            {
                Name = "AssessmentDataSource",
                ConnectionName = "AssessmentDynamic"
            };

            // Add all data sources to the report's component container
            this.ComponentStorage.Add(pupilDataSource);
            this.ComponentStorage.Add(staffDataSource);
            this.ComponentStorage.Add(assessmentDataSource);

            // Set Pupil as the primary data source (for the main Detail band)
            this.DataSource = pupilDataSource;
            this.DataMember = string.Empty;

            // Create report layout with all three data sources
            SetupReportLayout(staffDataSource, assessmentDataSource);
        }

        /// <summary>
        /// Creates the visual layout of the report with sections for all 3 data sources.
        /// </summary>
        private void SetupReportLayout(JsonDataSource staffDataSource, JsonDataSource assessmentDataSource)
        {
            // ======================================================================
            // REPORT HEADER
            // ======================================================================
            var reportHeader = new ReportHeaderBand();
            reportHeader.HeightF = 60F;
            reportHeader.Name = "DynamicReportHeader";

            var titleLabel = new XRLabel();
            titleLabel.Text = "DYNAMIC COLUMN SELECTION REPORT";
            titleLabel.SizeF = new SizeF(700F, 35F);
            titleLabel.LocationF = new PointF(0F, 5F);
            titleLabel.Font = new Font("Arial", 16F, FontStyle.Bold);
            titleLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            reportHeader.Controls.Add(titleLabel);

            var instructionLabel = new XRLabel();
            instructionLabel.Text = "Set column parameters before preview to filter data";
            instructionLabel.SizeF = new SizeF(700F, 20F);
            instructionLabel.LocationF = new PointF(0F, 40F);
            instructionLabel.Font = new Font("Arial", 10F, FontStyle.Italic);
            instructionLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            reportHeader.Controls.Add(instructionLabel);
            this.Bands.Add(reportHeader);

            // ======================================================================
            // PUPIL SECTION (Primary Data Source - uses main Detail band)
            // ======================================================================
            var pupilHeader = new GroupHeaderBand();
            pupilHeader.HeightF = 50F;
            pupilHeader.Name = "PupilGroupHeader";
            pupilHeader.RepeatEveryPage = true;
            pupilHeader.Level = 1;

            var pupilSectionLabel = new XRLabel();
            pupilSectionLabel.Text = "PUPILS";
            pupilSectionLabel.SizeF = new SizeF(700F, 25F);
            pupilSectionLabel.LocationF = new PointF(0F, 0F);
            pupilSectionLabel.Font = new Font("Arial", 14F, FontStyle.Bold);
            pupilSectionLabel.BackColor = Color.LightBlue;
            pupilHeader.Controls.Add(pupilSectionLabel);

            // Pupil table header
            var pupilHeaderTable = new XRTable();
            pupilHeaderTable.LocationF = new PointF(0F, 25F);
            pupilHeaderTable.SizeF = new SizeF(700F, 25F);
            pupilHeaderTable.Name = "PupilHeaderTable";

            var pupilHeaderRow = new XRTableRow();
            pupilHeaderRow.HeightF = 25F;

            var headers = new[] { "Pupil ID", "First Name", "Last Name", "DOB", "Class" };
            foreach (var header in headers)
            {
                var cell = new XRTableCell
                {
                    Text = header,
                    Font = new Font("Arial", 10F, FontStyle.Bold),
                    BackColor = Color.LightGray,
                    Borders = DevExpress.XtraPrinting.BorderSide.All
                };
                pupilHeaderRow.Cells.Add(cell);
            }

            pupilHeaderTable.Rows.Add(pupilHeaderRow);
            pupilHeader.Controls.Add(pupilHeaderTable);
            this.Bands.Add(pupilHeader);

            // Pupil detail band
            var pupilDetail = new DetailBand();
            pupilDetail.HeightF = 25F;
            pupilDetail.Name = "PupilDetail";

            var pupilDataTable = new XRTable();
            pupilDataTable.LocationF = new PointF(0F, 0F);
            pupilDataTable.SizeF = new SizeF(700F, 25F);
            pupilDataTable.Name = "PupilDataTable";

            var pupilDataRow = new XRTableRow();
            pupilDataRow.HeightF = 25F;

            var pupilBindings = new[] { "PupilId", "FirstName", "LastName", "DateOfBirth", "Class" };
            foreach (var binding in pupilBindings)
            {
                var cell = new XRTableCell();
                cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", $"[{binding}]"));
                cell.Borders = DevExpress.XtraPrinting.BorderSide.All;
                pupilDataRow.Cells.Add(cell);
            }

            pupilDataTable.Rows.Add(pupilDataRow);
            pupilDetail.Controls.Add(pupilDataTable);
            this.Bands.Add(pupilDetail);

            // ======================================================================
            // STAFF SECTION - Using DetailReportBand with its own DataSource
            // ======================================================================
            var staffDetailReport = new DetailReportBand();
            staffDetailReport.Name = "StaffDetailReport";
            staffDetailReport.DataSource = staffDataSource;
            staffDetailReport.DataMember = string.Empty;
            staffDetailReport.Level = 0;

            var staffHeader = new GroupHeaderBand();
            staffHeader.HeightF = 50F;
            staffHeader.Name = "StaffGroupHeader";
            staffHeader.RepeatEveryPage = true;

            var staffSectionLabel = new XRLabel();
            staffSectionLabel.Text = "STAFF";
            staffSectionLabel.SizeF = new SizeF(700F, 25F);
            staffSectionLabel.LocationF = new PointF(0F, 0F);
            staffSectionLabel.Font = new Font("Arial", 14F, FontStyle.Bold);
            staffSectionLabel.BackColor = Color.LightGreen;
            staffHeader.Controls.Add(staffSectionLabel);

            var staffHeaderTable = new XRTable();
            staffHeaderTable.LocationF = new PointF(0F, 25F);
            staffHeaderTable.SizeF = new SizeF(700F, 25F);
            staffHeaderTable.Name = "StaffHeaderTable";

            var staffHeaderRow = new XRTableRow();
            staffHeaderRow.HeightF = 25F;

            var staffHeaders = new[] { "Staff ID", "First Name", "Last Name", "Role", "Department" };
            foreach (var header in staffHeaders)
            {
                var cell = new XRTableCell
                {
                    Text = header,
                    Font = new Font("Arial", 10F, FontStyle.Bold),
                    BackColor = Color.LightGray,
                    Borders = DevExpress.XtraPrinting.BorderSide.All
                };
                staffHeaderRow.Cells.Add(cell);
            }

            staffHeaderTable.Rows.Add(staffHeaderRow);
            staffHeader.Controls.Add(staffHeaderTable);

            var staffDetail = new DetailBand();
            staffDetail.HeightF = 25F;
            staffDetail.Name = "StaffDetail";

            var staffDataTable = new XRTable();
            staffDataTable.LocationF = new PointF(0F, 0F);
            staffDataTable.SizeF = new SizeF(700F, 25F);
            staffDataTable.Name = "StaffDataTable";

            var staffDataRow = new XRTableRow();
            staffDataRow.HeightF = 25F;

            var staffBindings = new[] { "StaffId", "FirstName", "LastName", "Role", "Department" };
            foreach (var binding in staffBindings)
            {
                var cell = new XRTableCell();
                cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", $"[{binding}]"));
                cell.Borders = DevExpress.XtraPrinting.BorderSide.All;
                staffDataRow.Cells.Add(cell);
            }

            staffDataTable.Rows.Add(staffDataRow);
            staffDetail.Controls.Add(staffDataTable);

            staffDetailReport.Bands.Add(staffHeader);
            staffDetailReport.Bands.Add(staffDetail);
            this.Bands.Add(staffDetailReport);

            // ======================================================================
            // ASSESSMENT SECTION - Using DetailReportBand with its own DataSource
            // ======================================================================
            var assessmentDetailReport = new DetailReportBand();
            assessmentDetailReport.Name = "AssessmentDetailReport";
            assessmentDetailReport.DataSource = assessmentDataSource;
            assessmentDetailReport.DataMember = string.Empty;
            assessmentDetailReport.Level = 1;

            var assessmentHeader = new GroupHeaderBand();
            assessmentHeader.HeightF = 50F;
            assessmentHeader.Name = "AssessmentGroupHeader";
            assessmentHeader.RepeatEveryPage = true;

            var assessmentSectionLabel = new XRLabel();
            assessmentSectionLabel.Text = "ASSESSMENTS";
            assessmentSectionLabel.SizeF = new SizeF(700F, 25F);
            assessmentSectionLabel.LocationF = new PointF(0F, 0F);
            assessmentSectionLabel.Font = new Font("Arial", 14F, FontStyle.Bold);
            assessmentSectionLabel.BackColor = Color.LightCoral;
            assessmentHeader.Controls.Add(assessmentSectionLabel);

            var assessmentHeaderTable = new XRTable();
            assessmentHeaderTable.LocationF = new PointF(0F, 25F);
            assessmentHeaderTable.SizeF = new SizeF(700F, 25F);
            assessmentHeaderTable.Name = "AssessmentHeaderTable";

            var assessmentHeaderRow = new XRTableRow();
            assessmentHeaderRow.HeightF = 25F;

            var assessmentHeaders = new[] { "Assessment ID", "Pupil ID", "Subject", "Score", "Date" };
            foreach (var header in assessmentHeaders)
            {
                var cell = new XRTableCell
                {
                    Text = header,
                    Font = new Font("Arial", 10F, FontStyle.Bold),
                    BackColor = Color.LightGray,
                    Borders = DevExpress.XtraPrinting.BorderSide.All
                };
                assessmentHeaderRow.Cells.Add(cell);
            }

            assessmentHeaderTable.Rows.Add(assessmentHeaderRow);
            assessmentHeader.Controls.Add(assessmentHeaderTable);

            var assessmentDetail = new DetailBand();
            assessmentDetail.HeightF = 25F;
            assessmentDetail.Name = "AssessmentDetail";

            var assessmentDataTable = new XRTable();
            assessmentDataTable.LocationF = new PointF(0F, 0F);
            assessmentDataTable.SizeF = new SizeF(700F, 25F);
            assessmentDataTable.Name = "AssessmentDataTable";

            var assessmentDataRow = new XRTableRow();
            assessmentDataRow.HeightF = 25F;

            var assessmentBindings = new[] { "AssessmentId", "PupilId", "Subject", "Score", "Date" };
            foreach (var binding in assessmentBindings)
            {
                var cell = new XRTableCell();
                cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", $"[{binding}]"));
                cell.Borders = DevExpress.XtraPrinting.BorderSide.All;
                assessmentDataRow.Cells.Add(cell);
            }

            assessmentDataTable.Rows.Add(assessmentDataRow);
            assessmentDetail.Controls.Add(assessmentDataTable);

            assessmentDetailReport.Bands.Add(assessmentHeader);
            assessmentDetailReport.Bands.Add(assessmentDetail);
            this.Bands.Add(assessmentDetailReport);

            // ======================================================================
            // PAGE FOOTER
            // ======================================================================
            var pageFooter = new PageFooterBand();
            pageFooter.HeightF = 30F;
            pageFooter.Name = "PageFooter";

            var pageInfo = new XRPageInfo();
            pageInfo.Format = "Page {0} of {1}";
            pageInfo.SizeF = new SizeF(200F, 20F);
            pageInfo.LocationF = new PointF(250F, 5F);
            pageInfo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            pageFooter.Controls.Add(pageInfo);
            this.Bands.Add(pageFooter);
        }
    }
}
