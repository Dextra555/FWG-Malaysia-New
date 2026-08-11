using System;
using System.Configuration;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Finance_SalesInvoiceCollectionSummaryReport : System.Web.UI.Page
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

            if (string.IsNullOrEmpty(startDateStr))
                throw new ArgumentNullException("StartDate", "StartDate is required.");
            if (string.IsNullOrEmpty(endDateStr))
                throw new ArgumentNullException("EndDate", "EndDate is required.");

            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate   = DateTime.Parse(endDateStr);

            string server   = ConfigurationManager.AppSettings["Server"];
            string database = ConfigurationManager.AppSettings["Database"];
            string userID   = ConfigurationManager.AppSettings["UserID"];
            string password = ConfigurationManager.AppSettings["Password"];

            // Load the report manually so we can apply credentials before binding
            ReportDocument report = new ReportDocument();
            report.Load(Server.MapPath("~/Finance/SalesInvoiceCollectionSummary.rpt"));

            // SetDatabaseLogon works for both SQL Command and named-table reports
            report.SetDatabaseLogon(userID, password, server, database);

            // Also iterate tables in case the report has named tables or subreports
            foreach (Table table in report.Database.Tables)
            {
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = server;
                logonInfo.ConnectionInfo.DatabaseName = database;
                logonInfo.ConnectionInfo.UserID       = userID;
                logonInfo.ConnectionInfo.Password     = password;
                table.ApplyLogOnInfo(logonInfo);
            }

            // Company header parameters
            SetReportParam(report, "CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            SetReportParam(report, "CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            SetReportParam(report, "CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            SetReportParam(report, "CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetReportParam(report, "CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            SetReportParam(report, "CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            SetReportParam(report, "CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);

            // Report filter parameters
            SetReportParam(report, "Branch",  branch);
            SetReportParam(report, "LoginID", loginID);
            SetReportDateParam(report, "StartDate", startDate);
            SetReportDateParam(report, "EndDate",   endDate);

            // Bind the report to the viewer
            CrystalReportViewer.ToolPanelView = ToolPanelViewType.None;
            CrystalReportViewer.Zoom(100);
            CrystalReportViewer.ReportSource = report;
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

    private void SetReportParam(ReportDocument report, string name, string value)
    {
        try
        {
            ParameterFields pf = report.ParameterFields;
            if (pf[name] != null)
            {
                ParameterDiscreteValue pdv = new ParameterDiscreteValue();
                pdv.Value = value ?? "";
                report.SetParameterValue(name, pdv.Value);
            }
        }
        catch { /* parameter not in RPT — skip */ }
    }

    private void SetReportDateParam(ReportDocument report, string name, DateTime value)
    {
        try
        {
            if (report.ParameterFields[name] != null)
            {
                report.SetParameterValue(name, value);
            }
        }
        catch { /* parameter not in RPT — skip */ }
    }
}
