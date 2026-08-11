using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Database Login
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourcePaySheet.ReportDocument.Database.Tables)
            {
                CrystalReportViewerPaySheet.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewerPaySheet.Zoom(100);

                TableLogOnInfo logonInfo = table.LogOnInfo;

                logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
                logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];

                table.ApplyLogOnInfo(logonInfo);
            }

            // Company Parameters
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyName"].CurrentValues.Clear();
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Clear();
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Clear();
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Clear();
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Clear();
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Clear();
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyPhone"].CurrentValues.Clear();

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyName"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = ConfigurationManager.AppSettings["CompanyName"] });

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = ConfigurationManager.AppSettings["Address1"] });

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = ConfigurationManager.AppSettings["Address2"] });

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(
                new ParameterDiscreteValue
                {
                    Value = ConfigurationManager.AppSettings["PostCode"] + " " +
                            ConfigurationManager.AppSettings["City"]
                });

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = ConfigurationManager.AppSettings["State"] });

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = ConfigurationManager.AppSettings["Registration"] });

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = ConfigurationManager.AppSettings["Phone"] });

            // Date Parameters
            DateTime startDate = DateTime.Parse(Request.QueryString["StartDate"]);
            DateTime endDate = DateTime.Parse(Request.QueryString["EndDate"]);

            CrystalReportViewerPaySheet.ParameterFieldInfo["StartDate"].CurrentValues.Clear();
            CrystalReportViewerPaySheet.ParameterFieldInfo["EndDate"].CurrentValues.Clear();

            CrystalReportViewerPaySheet.ParameterFieldInfo["StartDate"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = startDate });

            CrystalReportViewerPaySheet.ParameterFieldInfo["EndDate"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = endDate });

            // Login ID
            string loginID = Request.QueryString["LoginID"];

            CrystalReportViewerPaySheet.ParameterFieldInfo["LoginID"].CurrentValues.Clear();

            CrystalReportViewerPaySheet.ParameterFieldInfo["LoginID"].CurrentValues.Add(
                new ParameterDiscreteValue { Value = loginID });

            // Branch Filter
            string branch = Request.QueryString["Branch"];

            CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Clear();

            if (!string.IsNullOrEmpty(branch) && branch != "0")
            {
                CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(
                    new ParameterDiscreteValue { Value = branch });

                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                    "{BranchMaster.Code} = '" + branch.Replace("'", "''") + "'";
            }
            else
            {
                CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(
                    new ParameterDiscreteValue { Value = "ALL BRANCHES" });

                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula = "";
            }
        }
        catch (Exception ex)
        {
            Response.Write("Error : " + ex.Message);
        }
    }
}