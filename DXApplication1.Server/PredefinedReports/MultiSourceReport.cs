using DevExpress.DataAccess.Json;
using DevExpress.XtraReports.UI;

namespace DXApplication1.PredefinedReports
{
    /// <summary>
    /// A report that demonstrates multiple JSON data sources.
    /// Each data source is configured to use the StoreConnectionNameOnly pattern,
    /// which means DevExpress will resolve the actual connection from the registered
    /// IDataSourceWizardJsonConnectionStorage (CustomApiDataConnectionStorage).
    /// When previewed, DevExpress will call each endpoint separately to fetch data.
    /// </summary>
    public partial class MultiSourceReport : XtraReport
    {
        public MultiSourceReport()
        {
            InitializeComponent();
            SetupMultipleDataSources();
        }

        /// <summary>
        /// Configures multiple JSON data sources for this report.
        /// Each JsonDataSource uses StoreConnectionNameOnly = true so that DevExpress
        /// resolves the actual URI from the registered connection storage.
        /// </summary>
        private void SetupMultipleDataSources()
        {
            // Create Pupil data source - uses connection name, resolved at runtime
            var pupilDataSource = new JsonDataSource
            {
                Name = "PupilDataSource",
                ConnectionName = "Pupil" // Matches the Name in api-connections.json
            };

            // Create Staff data source
            var staffDataSource = new JsonDataSource
            {
                Name = "StaffDataSource",
                ConnectionName = "Staff" // Matches the Name in api-connections.json
            };

            // Create Assessment data source
            var assessmentDataSource = new JsonDataSource
            {
                Name = "AssessmentDataSource",
                ConnectionName = "Assessment" // Matches the Name in api-connections.json
            };

            // Add all data sources to the report's component container
            this.ComponentStorage.Add(pupilDataSource);
            this.ComponentStorage.Add(staffDataSource);
            this.ComponentStorage.Add(assessmentDataSource);

            // Set Pupil as the primary data source
            // Users can bind controls to any of the data sources in the designer
            this.DataSource = pupilDataSource;
            this.DataMember = string.Empty;
        }
    }
}
