using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Accounting_GeneralLedgerReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptCrystalReportViewerSource.ReportDocument.Database.Tables)
            {
                crptCrystalReportViewer.ToolPanelView = ToolPanelViewType.None;
                crptCrystalReportViewer.Zoom(100);
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

            ParameterDiscreteValue paramStartDate = new ParameterDiscreteValue();
            paramStartDate.Value = Request.QueryString["StartDate"];            

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = Request.QueryString["Branch"];

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            crptCrystalReportViewer.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptCrystalReportViewer.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptCrystalReportViewer.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptCrystalReportViewer.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptCrystalReportViewer.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptCrystalReportViewer.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptCrystalReportViewer.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);

            crptCrystalReportViewer.ParameterFieldInfo["StartDate"].CurrentValues.Add(paramStartDate);
            crptCrystalReportViewer.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptCrystalReportViewer.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);

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
