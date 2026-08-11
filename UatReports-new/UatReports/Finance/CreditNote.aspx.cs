using System;
using System.Configuration;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Finance_CreditNote : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack) return;

        try
        {
            // Called from:
            // 1. Print button (row)  : ?ID=4&Branch=KL&LoginID=admin
            // 2. Report page (filter): ?LoginID=admin&Branch=KL&Client=C001&StartDate=2026-06-01&EndDate=2026-06-30
            string id           = Request.QueryString["ID"]        ?? "0";
            string branch       = Request.QueryString["Branch"]    ?? "0";
            string client       = Request.QueryString["Client"]    ?? "";
            string loginID      = Request.QueryString["LoginID"]   ?? "";
            string startDateStr = Request.QueryString["StartDate"] ?? "";
            string endDateStr   = Request.QueryString["EndDate"]   ?? "";

            // Default date range: current month if not provided
            DateTime startDate = string.IsNullOrEmpty(startDateStr)
                ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                : DateTime.Parse(startDateStr);

            DateTime endDate = string.IsNullOrEmpty(endDateStr)
                ? new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
                : DateTime.Parse(endDateStr);

            // Build connection info from Web.config
            ConnectionInfo connInfo = new ConnectionInfo
            {
                ServerName   = ConfigurationManager.AppSettings["Server"],
                DatabaseName = ConfigurationManager.AppSettings["Database"],
                UserID       = ConfigurationManager.AppSettings["UserID"],
                Password     = ConfigurationManager.AppSettings["Password"]
            };

            CrystalReportViewer.ToolPanelView = ToolPanelViewType.None;
            CrystalReportViewer.Zoom(100);

            // Apply connection to ALL tables (covers both regular tables and SQL Command datasource)
            foreach (Table table in CrystalReportSource.ReportDocument.Database.Tables)
            {
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo = connInfo;
                table.ApplyLogOnInfo(logonInfo);
            }

            // Also set at ReportDocument level — required for SQL Command datasource
            ReportDocument rptDoc = CrystalReportSource.ReportDocument;
            rptDoc.SetDatabaseLogon(
                ConfigurationManager.AppSettings["UserID"],
                ConfigurationManager.AppSettings["Password"],
                ConfigurationManager.AppSettings["Server"],
                ConfigurationManager.AppSettings["Database"]
            );

            // Company header
            SetParam("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            SetParam("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            SetParam("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            SetParam("CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetParam("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            SetParam("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            SetParam("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);

            // Report filter parameters
            SetIntParam("CreditNoteID", int.Parse(id));
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
