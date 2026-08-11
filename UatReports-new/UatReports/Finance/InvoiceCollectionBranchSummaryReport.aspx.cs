using System;
using System.Configuration;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

/// <summary>
/// Code-behind for the Invoice Collection Branch Summary Report.
/// Renders InvoiceCollectionBranchSummary.rpt — a Crystal Report that
/// shows invoice totals grouped by Branch with the following columns:
///   BRANCH | Service Charges | Adjustment | Actual Amount | Tax Amount |
///   Invoice Amount | Collection Amount | CN Amount | Balance
///
/// Required Crystal Report parameters:
///   CompanyName, CompanyAddress1–4, CompanyRegistration, CompanyPhone
///   StartDate (DateTime), EndDate (DateTime), Branch (String), LoginID (String)
///
/// Crystal Report SQL Command (use as the "Command" data source in the .rpt):
/// Full query is in: Database\InvoiceCollectionBranchSummary_ReportQuery.sql
/// ============================================================
/// SELECT
///     ci.Branch,
///     ISNULL(SUM(ci.ServiceCharges), 0)                                             AS ServiceCharges,
///     ISNULL(SUM(ci.Discount), 0)                                                   AS Adjustment,
///     ISNULL(SUM(ci.ServiceCharges - ISNULL(ci.Discount,0)), 0)                     AS ActualAmount,
///     ISNULL(SUM(ci.TaxAmount), 0)                                                  AS TaxAmount,
///     ISNULL(SUM(ci.ServiceCharges - ISNULL(ci.Discount,0) + ISNULL(ci.TaxAmount,0)),0) AS InvoiceAmount,
///     ISNULL((SELECT SUM(rd.Amount) FROM ReceiptDetails rd
///             INNER JOIN Receipts r ON r.ID=rd.ReceiptID AND r.IsDeleted=0
///             WHERE rd.InvoiceID IN (SELECT ID FROM ClientInvoice ci2
///                 WHERE ci2.Branch=ci.Branch AND ci2.IsDeleted='N'
///                   AND ci2.InvoiceDate>={?StartDate} AND ci2.InvoiceDate<={?EndDate})),0) AS CollectionAmount,
///     ISNULL((SELECT SUM(r.CreditNoteAmount) FROM Receipts r
///             WHERE r.Branch=ci.Branch AND r.IsDeleted=0
///               AND r.ReceiptDate>={?StartDate} AND r.ReceiptDate<={?EndDate}),0)    AS CNAmount,
///     ... (InvoiceAmount - CollectionAmount - CNAmount)                              AS Balance
/// FROM ClientInvoice ci
/// WHERE ci.IsDeleted='N'
///   AND ci.InvoiceDate>={?StartDate} AND ci.InvoiceDate<={?EndDate}
///   AND ('{?Branch}'='0' OR '{?Branch}'='' OR ci.Branch='{?Branch}')
/// GROUP BY ci.Branch ORDER BY ci.Branch
/// ============================================================
/// </summary>
public partial class Finance_InvoiceCollectionBranchSummaryReport : System.Web.UI.Page
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

            // Apply database login credentials to all report tables
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

            // Company header parameters
            SetParam("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            SetParam("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            SetParam("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            SetParam("CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetParam("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            SetParam("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            SetParam("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);

            // Report filter parameters
            SetParam("Branch",  string.IsNullOrEmpty(branch) ? "0" : branch);
            SetParam("LoginID", loginID);
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
}
