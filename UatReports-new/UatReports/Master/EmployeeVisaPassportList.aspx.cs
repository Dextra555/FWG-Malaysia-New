using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Master_EmployeeVisaPassportList : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourceVisaPassportList.ReportDocument.Database.Tables)
            {
                CrystalReportViewerVisaPassportList.ToolPanelView = ToolPanelViewType.None;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // Company header parameters
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

            // Report-specific parameters
            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = !string.IsNullOrEmpty(Request.QueryString["Branch"])
                ? Request.QueryString["Branch"] : "All";

            ParameterDiscreteValue paramExpiryType = new ParameterDiscreteValue();
            paramExpiryType.Value = !string.IsNullOrEmpty(Request.QueryString["ExpiryType"])
                ? Request.QueryString["ExpiryType"] : "Visa";

            ParameterDiscreteValue paramExpiryStatus = new ParameterDiscreteValue();
            paramExpiryStatus.Value = !string.IsNullOrEmpty(Request.QueryString["ExpiryStatus"])
                ? Request.QueryString["ExpiryStatus"] : "Yes";

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            // Apply all parameters
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["ExpiryType"].CurrentValues.Add(paramExpiryType);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["ExpiryStatus"].CurrentValues.Add(paramExpiryStatus);
            CrystalReportViewerVisaPassportList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
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
