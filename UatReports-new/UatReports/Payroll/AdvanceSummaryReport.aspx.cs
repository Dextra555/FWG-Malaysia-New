using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Payroll_AdvanceSummaryReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourceData.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                CrystalReportViewerView.ToolPanelView = ToolPanelViewType.None;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            ParameterDiscreteValue paramCompanyName = new ParameterDiscreteValue();
            paramCompanyName.Value = ConfigurationManager.AppSettings["CompanyName"];

            ParameterDiscreteValue paramAddress1 = new ParameterDiscreteValue();
            paramAddress1.Value = ConfigurationManager.AppSettings["Address1"];

            ParameterDiscreteValue paramAddress2 = new ParameterDiscreteValue();
            paramAddress2.Value = ConfigurationManager.AppSettings["Address2"];

            ParameterDiscreteValue paramPostCodeCity = new ParameterDiscreteValue();
            paramPostCodeCity.Value = ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"];

            ParameterDiscreteValue paramState = new ParameterDiscreteValue();
            paramState.Value = ConfigurationManager.AppSettings["State"];

            ParameterDiscreteValue paramRegistration = new ParameterDiscreteValue();
            paramRegistration.Value = ConfigurationManager.AppSettings["Registration"];

            ParameterDiscreteValue paramPhone = new ParameterDiscreteValue();
            paramPhone.Value = ConfigurationManager.AppSettings["Phone"];

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = Request.QueryString["EmployeeType"];

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            string period = Request.QueryString["Period"];
            DateTime periodDate;
            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();

            if (DateTime.TryParse(period, out periodDate))
            {

                paramPeriod.Value = periodDate;
            }
            else
            {
                paramPeriod.Value = DateTime.Now;
            }

            CrystalReportViewerView.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerView.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerView.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
            CrystalReportViewerView.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            CrystalReportViewerView.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
        }
        catch (ArgumentNullException ex)
        {
            Response.Write("Error: Data cannot be null. " + ex.Message);
        }
        catch (Exception ex)
        {
            Response.Write("Error: " + ex.Message);
        }
    }   
}
