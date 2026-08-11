using System;
using System.Configuration;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Finance_DebitNoteList : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack) return;

        try
        {
            // Parameters from Angular Report page:
            // ?LoginID=admin&Branch=0&Client=&StartDate=2026-06-01&EndDate=2026-06-30
            string branch       = Request.QueryString["Branch"]    ?? "0";
            string client       = Request.QueryString["Client"]    ?? "";
            string loginID      = Request.QueryString["LoginID"]   ?? "";
            string startDateStr = Request.QueryString["StartDate"] ?? "";
            string endDateStr   = Request.QueryString["EndDate"]   ?? "";

            // Default: current month
            DateTime startDate = string.IsNullOrEmpty(startDateStr)
                ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                : DateTime.Parse(startDateStr);

            DateTime endDate = string.IsNullOrEmpty(endDateStr)
                ? new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
                : DateTime.Parse(endDateStr);

            // Apply DB login to all tables
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

            // Company header
            SetParam("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            SetParam("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            SetParam("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            SetParam("CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetParam("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            SetParam("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            SetParam("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);

            // Filter parameters
            SetParam("Branch",  branch);
            SetParam("Client",  client);
            SetParam("LoginID", loginID);
            SetDateParam("StartDate", startDate);
            SetDateParam("EndDate",   endDate);
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
}
