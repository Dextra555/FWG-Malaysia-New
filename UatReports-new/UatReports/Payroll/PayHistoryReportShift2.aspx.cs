using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Web;

public partial class Payroll_PayHistoryReportShift2 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Apply DB login to all tables
            foreach (Table table in CrystalReportSourceData.ReportDocument.Database.Tables)
            {
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // Hide tool panel
            CrystalReportViewerView.ToolPanelView = ToolPanelViewType.None;

            // ================= PARAMETERS =================
            SetParameter("CompanyName",        ConfigurationManager.AppSettings["CompanyName"]);
            SetParameter("CompanyAddress1",    ConfigurationManager.AppSettings["Address1"]);
            SetParameter("CompanyAddress2",    ConfigurationManager.AppSettings["Address2"]);
            SetParameter("CompanyAddress3",    ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetParameter("CompanyAddress4",    ConfigurationManager.AppSettings["State"]);
            SetParameter("CompanyRegistration",ConfigurationManager.AppSettings["Registration"]);
            SetParameter("CompanyPhone",       ConfigurationManager.AppSettings["Phone"]);
            SetParameter("LoginID",            Request.QueryString["LoginID"]);
            SetParameter("Branch",             Request.QueryString["Branch"]);
            SetParameter("EmployeeType",       Request.QueryString["EmployeeType"]);

            // StartDate
            DateTime startDate;
            SetParameter("StartDate",
                DateTime.TryParse(Request.QueryString["StartDate"], out startDate) ? startDate : DateTime.Now);

            // EndDate
            DateTime endDate;
            SetParameter("EndDate",
                DateTime.TryParse(Request.QueryString["EndDate"], out endDate) ? endDate : DateTime.Now);

            // Connection string
            string connStr = ConfigurationManager.ConnectionStrings["obms"] != null
                ? ConfigurationManager.ConnectionStrings["obms"].ConnectionString
                : string.Format("Server={0};Database={1};User Id={2};Password={3};",
                    ConfigurationManager.AppSettings["Server"],
                    ConfigurationManager.AppSettings["Database"],
                    ConfigurationManager.AppSettings["UserID"],
                    ConfigurationManager.AppSettings["Password"]);

            // ================= RECORD SELECTION FORMULA =================
            string formula = CrystalReportSourceData.ReportDocument.RecordSelectionFormula ?? "";

            // --- EmployeeType filter (for 'Others' role) ---
            string employeeTypeCheck = Request.QueryString["EmployeeType"] ?? "";
            if (!string.IsNullOrEmpty(employeeTypeCheck)
                && employeeTypeCheck != "Guard"
                && employeeTypeCheck != "Staff"
                && employeeTypeCheck != "Foreign Guard"
                && employeeTypeCheck != "FGuard")
            {
                // Override parameter to 'Guard' so .rpt internal conditions work
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue guardOverride = new ParameterDiscreteValue();
                guardOverride.Value = "Guard";
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(guardOverride);

                // Get employee IDs for the specific role
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
                formula = BuildFormula(formula, "{Employee.EMP_CODE} = '" + empParam.Replace("'", "''") + "'");
            }

            // Apply final formula
            if (!string.IsNullOrWhiteSpace(formula))
                CrystalReportSourceData.ReportDocument.RecordSelectionFormula = formula;
        }
        catch (Exception ex)
        {
            Response.Write("Error: " + ex.Message);
        }
    }

    private string BuildFormula(string existing, string newCondition)
    {
        existing     = (existing     ?? "").Trim();
        newCondition = (newCondition ?? "").Trim();
        if (string.IsNullOrEmpty(existing))     return newCondition;
        if (string.IsNullOrEmpty(newCondition)) return existing;
        return "(" + existing + ") AND (" + newCondition + ")";
    }

    private void SetParameter(string name, object value)
    {
        ParameterDiscreteValue pd = new ParameterDiscreteValue();
        pd.Value = value;
        CrystalReportViewerView.ParameterFieldInfo[name].CurrentValues.Add(pd);
    }
}
