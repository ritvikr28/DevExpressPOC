using DevExpress.DataAccess.Json;
using DevExpress.XtraReports.UI;
using System.Drawing;

namespace DXApplication1.PredefinedReports
{
    /// <summary>
    /// A report that displays a single pupil's subject results and total marks.
    /// 
    /// The report accepts a PupilId parameter and filters the PupilResult data source
    /// to show only that pupil's records. Results are grouped by pupil so that the
    /// pupil name appears in the group header and the total marks appear in the footer.
    /// 
    /// Data is currently sourced from Data/pupil-results.json via the "PupilResult"
    /// API connection. This will be replaced by a live API call in future.
    /// </summary>
    public partial class PupilResultReport : XtraReport
    {
        public PupilResultReport()
        {
            InitializeComponent();
            SetupReport();
        }

        private void SetupReport()
        {
            // ======================================================================
            // DATA SOURCE
            // ======================================================================
            var dataSource = new JsonDataSource
            {
                Name = "PupilResultDataSource",
                ConnectionName = "PupilResult"
            };
            this.ComponentStorage.Add(dataSource);
            this.DataSource = dataSource;
            this.DataMember = string.Empty;

            // ======================================================================
            // PARAMETER - PupilId
            // ======================================================================
            var pupilIdParam = new Parameter
            {
                Name = "PupilId",
                Type = typeof(int),
                Value = 1,
                Description = "Pupil ID"
            };
            this.Parameters.Add(pupilIdParam);

            // Filter the data source to only rows matching the selected PupilId
            this.FilterString = "[PupilId] = ?PupilId";

            SetupReportBands();
        }

        private void SetupReportBands()
        {
            // ======================================================================
            // REPORT HEADER - title
            // ======================================================================
            var reportHeader = new ReportHeaderBand
            {
                HeightF = 60F,
                Name = "ReportHeader"
            };

            var titleLabel = new XRLabel
            {
                Text = "Pupil Result Report",
                SizeF = new SizeF(750F, 40F),
                LocationF = new PointF(0F, 5F),
                Font = new Font("Arial", 18F, FontStyle.Bold),
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter
            };
            reportHeader.Controls.Add(titleLabel);
            this.Bands.Add(reportHeader);

            // ======================================================================
            // GROUP HEADER - pupil name + column headings
            // ======================================================================
            var groupHeader = new GroupHeaderBand
            {
                HeightF = 55F,
                Name = "GroupHeader",
                RepeatEveryPage = true
            };
            groupHeader.GroupFields.Add(new GroupField("PupilId", XRColumnSortOrder.Ascending));

            var pupilNameLabel = new XRLabel
            {
                SizeF = new SizeF(750F, 25F),
                LocationF = new PointF(0F, 0F),
                Font = new Font("Arial", 13F, FontStyle.Bold),
                BackColor = Color.LightSteelBlue,
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft,
                Padding = new DevExpress.XtraPrinting.PaddingInfo(8, 0, 0, 0)
            };
            pupilNameLabel.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "Concat('Pupil: ', [PupilName])"));
            groupHeader.Controls.Add(pupilNameLabel);

            var colHeaderTable = new XRTable
            {
                LocationF = new PointF(0F, 30F),
                SizeF = new SizeF(400F, 25F),
                Name = "ColHeaderTable"
            };
            var colHeaderRow = new XRTableRow { HeightF = 25F };
            colHeaderRow.Cells.Add(new XRTableCell
            {
                Text = "Subject",
                Font = new Font("Arial", 10F, FontStyle.Bold),
                BackColor = Color.LightGray,
                Borders = DevExpress.XtraPrinting.BorderSide.All
            });
            colHeaderRow.Cells.Add(new XRTableCell
            {
                Text = "Marks",
                Font = new Font("Arial", 10F, FontStyle.Bold),
                BackColor = Color.LightGray,
                Borders = DevExpress.XtraPrinting.BorderSide.All
            });
            colHeaderTable.Rows.Add(colHeaderRow);
            groupHeader.Controls.Add(colHeaderTable);

            this.Bands.Add(groupHeader);

            // ======================================================================
            // DETAIL BAND - one row per subject
            // ======================================================================
            var detailBand = new DetailBand
            {
                HeightF = 25F,
                Name = "Detail"
            };

            var dataTable = new XRTable
            {
                LocationF = new PointF(0F, 0F),
                SizeF = new SizeF(400F, 25F),
                Name = "DataTable"
            };
            var dataRow = new XRTableRow { HeightF = 25F };

            var subjectCell = new XRTableCell { Borders = DevExpress.XtraPrinting.BorderSide.All };
            subjectCell.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Subject]"));

            var marksCell = new XRTableCell { Borders = DevExpress.XtraPrinting.BorderSide.All };
            marksCell.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Marks]"));

            dataRow.Cells.AddRange(new XRTableCell[] { subjectCell, marksCell });
            dataTable.Rows.Add(dataRow);
            detailBand.Controls.Add(dataTable);
            this.Bands.Add(detailBand);

            // ======================================================================
            // GROUP FOOTER - total marks for the pupil
            // ======================================================================
            var groupFooter = new GroupFooterBand
            {
                HeightF = 35F,
                Name = "GroupFooter"
            };

            var totalLabelCaption = new XRLabel
            {
                Text = "Total Marks:",
                SizeF = new SizeF(200F, 25F),
                LocationF = new PointF(0F, 5F),
                Font = new Font("Arial", 10F, FontStyle.Bold),
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight
            };
            groupFooter.Controls.Add(totalLabelCaption);

            var totalValueLabel = new XRLabel
            {
                SizeF = new SizeF(200F, 25F),
                LocationF = new PointF(200F, 5F),
                Font = new Font("Arial", 10F, FontStyle.Bold),
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft,
                Padding = new DevExpress.XtraPrinting.PaddingInfo(8, 0, 0, 0),
                BackColor = Color.LightYellow,
                Borders = DevExpress.XtraPrinting.BorderSide.All,
                Summary = new XRSummary
                {
                    Func = SummaryFunc.Sum,
                    Running = SummaryRunning.Group,
                    FormatString = "{0:N0}"
                }
            };
            totalValueLabel.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Marks]"));
            groupFooter.Controls.Add(totalValueLabel);

            this.Bands.Add(groupFooter);

            // ======================================================================
            // PAGE FOOTER - page numbers
            // ======================================================================
            var pageFooter = new PageFooterBand
            {
                HeightF = 25F,
                Name = "PageFooter"
            };
            var pageInfo = new XRPageInfo
            {
                Format = "Page {0} of {1}",
                SizeF = new SizeF(200F, 20F),
                LocationF = new PointF(275F, 2F),
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter
            };
            pageFooter.Controls.Add(pageInfo);
            this.Bands.Add(pageFooter);
        }
    }
}
