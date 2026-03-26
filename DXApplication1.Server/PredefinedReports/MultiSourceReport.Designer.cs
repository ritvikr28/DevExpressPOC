using DevExpress.XtraReports.UI;
using System.ComponentModel;
using System.Drawing;

namespace DXApplication1.PredefinedReports
{
    partial class MultiSourceReport
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            this.components = new Container();
            
            // TopMargin
            this.TopMargin = new TopMarginBand();
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name = "TopMargin";

            // BottomMargin
            this.BottomMargin = new BottomMarginBand();
            this.BottomMargin.HeightF = 50F;
            this.BottomMargin.Name = "BottomMargin";

            // Report Header
            this.ReportHeader = new ReportHeaderBand();
            this.ReportHeader.HeightF = 60F;
            this.ReportHeader.Name = "ReportHeader";
            
            var titleLabel = new XRLabel();
            titleLabel.Text = "Multi-Source Report";
            titleLabel.SizeF = new SizeF(650F, 30F);
            titleLabel.LocationF = new PointF(0F, 10F);
            titleLabel.Font = new Font("Arial", 18F, FontStyle.Bold);
            this.ReportHeader.Controls.Add(titleLabel);

            var infoLabel = new XRLabel();
            infoLabel.Text = "This report demonstrates data from multiple JSON sources (Pupil, Staff, Assessment)";
            infoLabel.SizeF = new SizeF(650F, 20F);
            infoLabel.LocationF = new PointF(0F, 40F);
            infoLabel.Font = new Font("Arial", 10F, FontStyle.Italic);
            this.ReportHeader.Controls.Add(infoLabel);

            // Detail band
            this.Detail = new DetailBand();
            this.Detail.HeightF = 25F;
            this.Detail.Name = "Detail";

            // Page Footer with instructions
            this.PageFooter = new PageFooterBand();
            this.PageFooter.HeightF = 50F;
            this.PageFooter.Name = "PageFooter";

            var pageInfo = new XRPageInfo();
            pageInfo.Format = "Page {0} of {1}";
            pageInfo.LocationF = new PointF(0F, 10F);
            pageInfo.SizeF = new SizeF(300F, 20F);
            this.PageFooter.Controls.Add(pageInfo);

            // Configure report
            this.Bands.AddRange(new Band[] {
                this.TopMargin,
                this.ReportHeader,
                this.Detail,
                this.PageFooter,
                this.BottomMargin
            });

            this.PageWidth = 850;
            this.PageHeight = 1100;
            this.Version = "24.1";
        }

        #endregion

        private TopMarginBand TopMargin;
        private BottomMarginBand BottomMargin;
        private ReportHeaderBand ReportHeader;
        private DetailBand Detail;
        private PageFooterBand PageFooter;
    }
}
