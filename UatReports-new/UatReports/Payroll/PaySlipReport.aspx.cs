using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Web;

public partial class PayRoll_PaySlipReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            string rptFile = Request.QueryString["Lang"] == "M"
                ? "../Payroll/PaySlipBM.rpt"
                : "../Payroll/PaySlip.rpt";

            // Load report
            crptPaySlip.ReportDocument.Load(Server.MapPath(rptFile));

            // Apply DB login to main report
            ApplyLogin(crptPaySlip.ReportDocument);

            // Apply DB login to subreports
            foreach (ReportDocument sub in crptPaySlip.ReportDocument.Subreports)
                ApplyLogin(sub);

            // Hide tool panel
            crptPaySlipList.ToolPanelView = ToolPanelViewType.None;
            crptPaySlipList.Zoom(100);

            // ================= PARAMETERS =================
            SetParameter("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            SetParameter("CompanyAddress1",      ConfigurationManager.AppSettings["Address1"]);
            SetParameter("CompanyAddress2",      ConfigurationManager.AppSettings["Address2"]);
            SetParameter("CompanyAddress3",      ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetParameter("CompanyAddress4",      ConfigurationManager.AppSettings["State"]);
            SetParameter("CompanyRegistration",  ConfigurationManager.AppSettings["Registration"]);
            SetParameter("CompanyPhone",         ConfigurationManager.AppSettings["Phone"]);
            SetParameter("LoginID",              Request.QueryString["LoginID"]);
            SetParameter("Branch",               Request.QueryString["Branch"]);
            SetParameter("EmployeeType",         Request.QueryString["EmployeeType"]);

            // Period
            DateTime periodDate;
            if (!DateTime.TryParse(Request.QueryString["Period"], out periodDate))
                periodDate = DateTime.Now;
            SetParameter("Period", periodDate);

            string connStr = ConfigurationManager.ConnectionStrings["obms"] != null
                ? ConfigurationManager.ConnectionStrings["obms"].ConnectionString
                : string.Format("Server={0};Database={1};User Id={2};Password={3};",
                    ConfigurationManager.AppSettings["Server"],
                    ConfigurationManager.AppSettings["Database"],
                    ConfigurationManager.AppSettings["UserID"],
                    ConfigurationManager.AppSettings["Password"]);

            // ================= RECORD SELECTION FORMULA =================
            string formula = crptPaySlip.ReportDocument.RecordSelectionFormula ?? "";

            // --- EmployeeType filter (for 'Others' role) ---
            string employeeTypeCheck = Request.QueryString["EmployeeType"] ?? "";
            if (!string.IsNullOrEmpty(employeeTypeCheck)
                && employeeTypeCheck != "Guard"
                && employeeTypeCheck != "Staff"
                && employeeTypeCheck != "Foreign Guard"
                && employeeTypeCheck != "FGuard")
            {
                var empIds = new System.Collections.Generic.List<string>();
                using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                {
                    conn.Open();
                    var cmd = new System.Data.SqlClient.SqlCommand(
                        "SELECT EMP_ID FROM Employee WHERE EMP_ROLE = @role", conn);
                    cmd.Parameters.AddWithValue("@role", employeeTypeCheck);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) empIds.Add(reader.GetInt32(0).ToString());
                }
                string empIdCondition = empIds.Count > 0
                    ? "{PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")"
                    : "{PaySlip.EmployeeID} = 0";
                formula = BuildFormula(formula, empIdCondition);
            }

            // --- Specific Employee filter (blank / "0" = show all) ---
            string empParam = (Request.QueryString["Employee"] ?? "").Trim();
            if (!string.IsNullOrEmpty(empParam) && empParam != "0")
            {
                formula = BuildFormula(formula, "{vwPaySheet.EMP_CODE} = '" + empParam.Replace("'", "''") + "'");
            }

            // --- ClientCode filter: directly use vwPaySheet.EMP_CLIENT ---
            // vwPaySheet already has EMP_CLIENT from the correct period — no extra query needed.
            // Also exclude NULL EMP_CLIENT rows when a client is selected.
            string clientCode  = (Request.QueryString["ClientCode"] ?? "").Trim();

            if (!string.IsNullOrEmpty(clientCode))
            {
                formula = BuildFormula(formula, 
                    "NOT ISNULL({vwPaySheet.EMP_CLIENT}) AND {vwPaySheet.EMP_CLIENT} = '" + clientCode.Replace("'", "''") + "'");
            }

            // Apply final formula
            if (!string.IsNullOrWhiteSpace(formula))
                crptPaySlip.ReportDocument.RecordSelectionFormula = formula;
        }
        catch (Exception ex)
        {
            ShowMessage("Error Found. " + ex.Message);
        }
    }

    // Build Crystal Reports formula safely (avoids leading AND issue)
    private string BuildFormula(string existing, string newCondition)
    {
        existing     = (existing     ?? "").Trim();
        newCondition = (newCondition ?? "").Trim();
        if (string.IsNullOrEmpty(existing))     return newCondition;
        if (string.IsNullOrEmpty(newCondition)) return existing;
        return "(" + existing + ") AND (" + newCondition + ")";
    }

    // Apply DB login to a report/subreport
    private void ApplyLogin(ReportDocument rpt)
    {
        TableLogOnInfo logon = new TableLogOnInfo();
        logon.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
        logon.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
        logon.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
        logon.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];

        foreach (Table table in rpt.Database.Tables)
            table.ApplyLogOnInfo(logon);
    }

    // Add parameter to report viewer
    private void SetParameter(string name, object value)
    {
        ParameterDiscreteValue pd = new ParameterDiscreteValue();
        pd.Value = value;
        crptPaySlipList.ParameterFieldInfo[name].CurrentValues.Add(pd);
    }

    protected void ShowMessage(string message)
    {
        Response.Write("Error: " + message);
    }
}
