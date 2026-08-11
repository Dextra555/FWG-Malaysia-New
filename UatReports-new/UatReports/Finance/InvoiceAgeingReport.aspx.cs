using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Finance_InvoiceAgeingReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            string loginID      = Request.QueryString["LoginID"]      ?? "superadmin";
            string startDate    = Request.QueryString["StartDate"]    ?? DateTime.Now.ToString("yyyy-MM-dd");
            string endDate      = Request.QueryString["EndDate"]      ?? DateTime.Now.ToString("yyyy-MM-dd");
            string branch       = Request.QueryString["Branch"]       ?? "";
            string masterClient = Request.QueryString["MasterClient"] ?? "";
            string client       = Request.QueryString["Client"]       ?? "";

            DateTime sd, ed;
            DateTime periodStartDate = DateTime.TryParse(startDate, out sd) ? sd : DateTime.Now;
            DateTime periodEndDate   = DateTime.TryParse(endDate,   out ed) ? ed : DateTime.Now;

            string sd_str = periodStartDate.ToString("yyyy-MM-dd");
            string ed_str = periodEndDate.ToString("yyyy-MM-dd");

            // Build safe filter strings
            // Priority: if a specific Branch was selected in the UI, use it directly.
            // Otherwise fall back to LoginID-based branch restriction (non-superadmin users).
            string branchFilter;
            if (!string.IsNullOrEmpty(branch) && branch != "0")
            {
                // Specific branch selected from dropdown
                branchFilter = string.Format("ci.Branch = '{0}'", branch.Replace("'", "''"));
            }
            else if (loginID.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
            {
                // Superadmin with no branch selected — show all branches
                branchFilter = "1=1";
            }
            else
            {
                // Non-superadmin with no branch selected — restrict to user's allowed branches
                branchFilter = string.Format("ci.Branch IN (SELECT BranchCode FROM OBMSBranches WHERE Name = '{0}')", loginID.Replace("'", "''"));
            }

            // Filter by master client (HQ): include clients whose SuperClientCode matches the HQ,
            // OR the HQ client itself. Use a subquery on ClientMaster to avoid depending on the
            // per-branch join alias (cm) which may miss cross-branch HQ relationships.
            string masterClientFilter = string.IsNullOrEmpty(masterClient)
                ? "1=1"
                : string.Format(
                    @"ci.Client IN (
                        SELECT Code FROM ClientMaster
                        WHERE SuperClientCode = '{0}' OR Code = '{0}'
                    )",
                    masterClient.Replace("'", "''"));

            string clientFilter = string.IsNullOrEmpty(client)
                ? "1=1"
                : string.Format("ci.Client = '{0}'", client.Replace("'", "''"));

            // Build the full SQL to replace Command
            string newSQL = string.Format(@"
select A.Branch,BranchName,A.Client,ClientName,CurrentMonth,Month1,Month2,
Month3,Month4,Month5,Month6,Month7,Year1,Year3,Year5,ActionTaken
from(
SELECT Branch,BranchName,Client,ClientName,
SUM(CurrentMonth) as CurrentMonth,SUM(Month1) as Month1,SUM(Month2) as Month2,
SUM(Month3) as Month3,SUM(Month4) as Month4,SUM(Month5) as Month5,
SUM(Month6) as Month6,SUM(Month7) as Month7,SUM(Year1) as Year1,
SUM(Year3) as Year3,SUM(Year5) as Year5
FROM (
SELECT ci.ID,ci.Branch,bm.Name as BranchName,ci.Client,cm.Name as ClientName,
CASE DateDiff(month,ci.InvoiceDate,'{1}') WHEN 0 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS CurrentMonth,
CASE DateDiff(month,ci.InvoiceDate,'{1}') WHEN 1 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Month1,
CASE DateDiff(month,ci.InvoiceDate,'{1}') WHEN 2 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Month2,
CASE DateDiff(month,ci.InvoiceDate,'{1}') WHEN 3 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Month3,
CASE DateDiff(month,ci.InvoiceDate,'{1}') WHEN 4 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Month4,
CASE DateDiff(month,ci.InvoiceDate,'{1}') WHEN 5 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Month5,
CASE DateDiff(month,ci.InvoiceDate,'{1}') WHEN 6 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Month6,
CASE WHEN DateDiff(month,ci.InvoiceDate,'{1}')>6  AND DateDiff(month,ci.InvoiceDate,'{1}')<12 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Month7,
CASE WHEN DateDiff(month,ci.InvoiceDate,'{1}')>11 AND DateDiff(month,ci.InvoiceDate,'{1}')<36 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Year1,
CASE WHEN DateDiff(month,ci.InvoiceDate,'{1}')>35 AND DateDiff(month,ci.InvoiceDate,'{1}')<60 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Year3,
CASE WHEN DateDiff(month,ci.InvoiceDate,'{1}')>59 THEN ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0) ELSE 0 END AS Year5
FROM dbo.ClientInvoice ci
LEFT OUTER JOIN (
SELECT rd.InvoiceID,rd.Amount,rd.BalanceStatus,rd.BalanceAmount
FROM Receipts r INNER JOIN ReceiptDetails rd ON rd.ReceiptID=r.ID
WHERE r.ReceiptDate BETWEEN '{0}' AND '{1}' AND rd.IsInvoiceAdjustment=1 AND r.IsDeleted=0
) rt ON rt.InvoiceID=ci.ID
INNER JOIN ClientMaster cm ON cm.Code=ci.Client AND cm.Branch=ci.Branch
INNER JOIN BranchMaster bm ON bm.Code=ci.Branch
WHERE ci.InvoiceDate BETWEEN '{0}' AND '{1}'
AND ci.IsDeleted='N'
AND {2}
AND {3}
AND {4}
GROUP BY ci.ID,ci.InvoiceNo,ci.InvoiceDate,ci.Branch,bm.Name,ci.Client,cm.Name,ci.ServiceCharges,ci.Discount,ci.TaxAmount
HAVING ci.ServiceCharges-ci.Discount+ci.TaxAmount-ISNULL(SUM(rt.Amount+CASE rt.BalanceStatus WHEN 2 THEN rt.BalanceAmount ELSE 0 END),0)>0
) InvoiceAging
GROUP BY Branch,Client,BranchName,ClientName
) A
LEFT OUTER JOIN (
select distinct ib.Branch,ib.Client,ActionTaken
from ClientLegalDemandAction ia
inner join (
select Branch,Client,max(DateIssue) as DateIssue
from ClientLegalDemandAction
where DateIssue Between '{0}' and '{1}' and IsDeleted=0
Group by Branch,Client
) ib on ia.Branch=ib.Branch and ia.Client=ib.Client and ia.DateIssue=ib.DateIssue
) B on a.Branch=b.Branch and a.Client=b.Client",
                sd_str,           // {0} start date
                ed_str,           // {1} end date
                branchFilter,     // {2}
                masterClientFilter, // {3}
                clientFilter      // {4}
            );

            // Load report and replace Command SQL at runtime
            ReportDocument reportDoc = CrystalReportSource.ReportDocument;

            // Apply DB login
            ConnectionInfo connInfo = new ConnectionInfo
            {
                ServerName   = ConfigurationManager.AppSettings["Server"],
                DatabaseName = ConfigurationManager.AppSettings["Database"],
                UserID       = ConfigurationManager.AppSettings["UserID"],
                Password     = ConfigurationManager.AppSettings["Password"]
            };

            // Set parameters FIRST before table loop to prevent Crystal Reports prompting
            // Use ParameterFieldInfo on the Viewer (correct pattern for this ASP.NET Crystal Reports setup)
            ParameterDiscreteValue pAgingStart = new ParameterDiscreteValue();
            pAgingStart.Value = periodStartDate;
            CrystalReportViewer.ParameterFieldInfo["AgingStartDate"].CurrentValues.Add(pAgingStart);

            ParameterDiscreteValue pAgingDate = new ParameterDiscreteValue();
            pAgingDate.Value = periodEndDate;
            CrystalReportViewer.ParameterFieldInfo["AgingDate"].CurrentValues.Add(pAgingDate);

            SetViewerParam("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            SetViewerParam("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            SetViewerParam("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            SetViewerParam("CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetViewerParam("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            SetViewerParam("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            SetViewerParam("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);
            SetViewerParam("LoginID",             loginID);
            SetViewerParam("Branch",              (!string.IsNullOrEmpty(branch) && branch != "0") ? branch : "All");
            SetViewerParam("BranchCode",          (!string.IsNullOrEmpty(branch) && branch != "0") ? branch : "All");
            SetViewerParam("ClientHQ",            !string.IsNullOrEmpty(masterClient) ? masterClient : "All");

            // Replace the Command SQL with our dynamic SQL
            reportDoc.DataDefinition.FormulaFields["dperiod"].Text =
                string.Format("'{0} - {1}'",
                    periodStartDate.ToString("dd.MM.yyyy"),
                    periodEndDate.ToString("dd.MM.yyyy"));

            // Apply RecordSelectionFormula to filter by MasterClient and Client
            // This works regardless of CommandTable replacement
            System.Text.StringBuilder selFormula = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(masterClient))
            {
                // Get all client codes under this HQ from DB
                string clientCodes = GetClientCodesForHQ(masterClient,
                    ConfigurationManager.AppSettings["Server"],
                    ConfigurationManager.AppSettings["Database"],
                    ConfigurationManager.AppSettings["UserID"],
                    ConfigurationManager.AppSettings["Password"]);

                if (!string.IsNullOrEmpty(clientCodes))
                {
                    selFormula.AppendFormat("{{command.Client}} IN [{0}]", clientCodes);
                }
            }
            if (!string.IsNullOrEmpty(client))
            {
                if (selFormula.Length > 0) selFormula.Append(" AND ");
                selFormula.AppendFormat("{{command.Client}} = '{0}'", client.Replace("'", "''"));
            }
            if (selFormula.Length > 0)
            {
                reportDoc.RecordSelectionFormula = selFormula.ToString();
            }

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in reportDoc.Database.Tables)
            {
                CrystalReportViewer.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewer.Zoom(83);
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo = connInfo;
                table.ApplyLogOnInfo(logonInfo);
            }
        }
        catch (Exception ex)
        {
            Response.Write("Error: " + ex.Message);
        }
    }

    private string GetClientCodesForHQ(string hqCode, string server, string database, string userId, string password)
    {
        System.Text.StringBuilder codes = new System.Text.StringBuilder();
        try
        {
            string connStr = string.Format("Server={0};Database={1};User Id={2};Password={3};", server, database, userId, password);
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT Code FROM ClientMaster WHERE SuperClientCode = @hq OR Code = @hq";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hq", hqCode);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (codes.Length > 0) codes.Append(", ");
                            codes.AppendFormat("'{0}'", rdr[0].ToString().Replace("'", "''"));
                        }
                    }
                }
            }
        }
        catch { }
        return codes.ToString();
    }

    private void SetViewerParam(string name, string value)
    {
        try
        {
            ParameterDiscreteValue p = new ParameterDiscreteValue();
            p.Value = value ?? "";
            CrystalReportViewer.ParameterFieldInfo[name].CurrentValues.Add(p);
        }
        catch { }
    }

    private void SetParam(ReportDocument reportDoc, string name, string value)
    {
        try { reportDoc.SetParameterValue(name, value ?? ""); } catch { }
    }
}
