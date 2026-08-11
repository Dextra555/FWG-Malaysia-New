using System;
using System.Configuration;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Finance_BranchPaymentSummaryReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack) return;

        try
        {
            string startDateStr = Request.QueryString["StartDate"];
            string endDateStr   = Request.QueryString["EndDate"];
            string branch       = Request.QueryString["Branch"]  ?? "0";
            string loginID      = Request.QueryString["LoginID"] ?? "";

            // BankId is optional — 0 means "All Banks"
            int bankId = 0;
            int.TryParse(Request.QueryString["BankId"] ?? "0", out bankId);

            if (string.IsNullOrEmpty(startDateStr))
                throw new ArgumentNullException("StartDate", "StartDate is required.");
            if (string.IsNullOrEmpty(endDateStr))
                throw new ArgumentNullException("EndDate", "EndDate is required.");

            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate   = DateTime.Parse(endDateStr);

            // Apply DB login
            foreach (Table table in CrystalReportSource.ReportDocument.Database.Tables)
            {
                CrystalReportViewer.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewer.Zoom(100);
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // Set parameters — use index-safe helper to avoid "Invalid index" crash
            SetParam("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            SetParam("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            SetParam("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            SetParam("CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetParam("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            SetParam("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            SetParam("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);
            SetParam("Branch",              branch);
            SetIntParam("BankId",           bankId);
            SetParam("LoginID",             loginID);

            // Date params set separately (DateTime type)
            SetDateParam("StartDate", startDate);
            SetDateParam("EndDate",   endDate);
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

    private void SetParam(string name, string value)
    {
        try
        {
            var pf = CrystalReportViewer.ParameterFieldInfo[name];
            if (pf != null)
            {
                var pdv = new ParameterDiscreteValue { Value = value ?? "" };
                pf.CurrentValues.Clear();
                pf.CurrentValues.Add(pdv);
            }
        }
        catch { /* parameter not in RPT — skip */ }
    }

    private void SetDateParam(string name, DateTime value)
    {
        try
        {
            var pf = CrystalReportViewer.ParameterFieldInfo[name];
            if (pf != null)
            {
                var pdv = new ParameterDiscreteValue { Value = value };
                pf.CurrentValues.Clear();
                pf.CurrentValues.Add(pdv);
            }
        }
        catch { /* parameter not in RPT — skip */ }
    }

    private void SetIntParam(string name, int value)
    {
        try
        {
            var pf = CrystalReportViewer.ParameterFieldInfo[name];
            if (pf != null)
            {
                var pdv = new ParameterDiscreteValue { Value = value };
                pf.CurrentValues.Clear();
                pf.CurrentValues.Add(pdv);
            }
        }
        catch { /* parameter not in RPT — skip */ }
    }
}
