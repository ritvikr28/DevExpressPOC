//using DevExpress.DataAccess.ConnectionParameters;
//using DevExpress.DataAccess.Web;
//using DXApplication1.Data;
//using System.Collections.Generic;
//using System.Linq;

//namespace DXApplication1.Services
//{
//    public class CustomSqlDataSourceWizardConnectionStringsProvider : IDataSourceWizardConnectionStringsProvider
//    {
//        readonly ReportDbContext reportDataContext;

//        public CustomSqlDataSourceWizardConnectionStringsProvider(ReportDbContext reportDataContext)
//        {
//            this.reportDataContext = reportDataContext;
//        }
//        Dictionary<string, string> IDataSourceWizardConnectionStringsProvider.GetConnectionDescriptions()
//        {
//            return reportDataContext.SqlDataConnections.ToDictionary(x => x.Name, x => x.DisplayName);
//        }

//        DataConnectionParametersBase IDataSourceWizardConnectionStringsProvider.GetDataConnectionParameters(string name)
//        {
//            return null;
//        }
//    }
//}
