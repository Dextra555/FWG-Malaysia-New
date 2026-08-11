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

            // Load report correctly
            crptPaySlip.ReportDocument.Load(Server.MapPath(rptFile));

            // Apply DB LOGIN TO MAIN REPORT
            ApplyLogin(crptPaySlip.ReportDocument);

            // Apply DB LOGIN TO SUBREPORTS
            foreach (ReportDocument sub in crptPaySlip.ReportDocument.Subreports)
            {
                ApplyLogin(sub);
            }

            // Hide tool panel
            crptPaySlipList.ToolPanelView = ToolPanelViewType.None;
            crptPaySlipList.Zoom(100);

            // ================= PARAMETERS =================
            SetParameter("CompanyName", ConfigurationManager.AppSettings["CompanyName"]);
            SetParameter("CompanyAddress1", ConfigurationManager.AppSettings["Address1"]);
            SetParameter("CompanyAddress2", ConfigurationManager.AppSettings["Address2"]);
            SetParameter("CompanyAddress3", ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            SetParameter("CompanyAddress4", ConfigurationManager.AppSettings["State"]);
            SetParameter("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            SetParameter("CompanyPhone", ConfigurationManager.AppSettings["Phone"]);
            SetParameter("LoginID", Request.QueryString["LoginID"]);
            SetParameter("Branch", Request.QueryString["Branch"]);
            SetParameter("EmployeeType", Request.QueryString["EmployeeType"]);

            // PERIOD
            DateTime periodDate;
            if (!DateTime.TryParse(Request.QueryString["Period"], out periodDate))
                periodDate = DateTime.Now;

            SetParameter("Period", periodDate);

            // For 'Others' (Other Guards) type, override the EMP_ROLE filter via record selection formula
            string employeeTypeCheck = Request.QueryString["EmployeeType"];
            if (!string.IsNullOrEmpty(employeeTypeCheck) && employeeTypeCheck != "Guard" && employeeTypeCheck != "Staff" && employeeTypeCheck != "Foreign Guard" && employeeTypeCheck != "FGuard")
            {
                // Fully replace formula to bypass Crystal parameter restriction for non-standard types
            string existingFormula = crptPaySlip.ReportDocument.RecordSelectionFormula;
            // Remove any existing {?EmployeeType} based conditions
            string cleanFormula = System.Text.RegularExpressions.Regex.Replace(
                existingFormula ?? "",
                @"\{Employee\.EMP_ROLE\}\s*=\s*\{[^}]+\}",
                "{Employee.EMP_ROLE} = '" + employeeTypeCheck + "'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            if (string.IsNullOrWhiteSpace(cleanFormula) || cleanFormula == existingFormula)
string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["obms"]?.ConnectionString
                    ?? string.Format("Server={0};Database={1};User Id={2};Password={3};",
                        ConfigurationManager.AppSettings["Server"],
                        ConfigurationManager.AppSettings["Database"],
                        ConfigurationManager.AppSettings["UserID"],
                        ConfigurationManager.AppSettings["Password"]);

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

                if (empIds.Count > 0)
                    crptPaySlip.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")";
                else
                    crptPaySlip.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} = 0";
            else
                crptPaySlip.ReportDocument.RecordSelectionFormula = cleanFormula;
            }

            // Record selection for specific Employee
            if (this.Page.Request.QueryString["Employee"] != "0")
            {
                crptPaySlip.ReportDocument.RecordSelectionFormula +=
                    " AND {vwPaySheet.EMP_CODE} = '" + Request.QueryString["Employee"] + "'";
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error Found. " + ex.Message);
        }
    }

    // Helper: Apply DB Login
    private void ApplyLogin(ReportDocument rpt)
    {
        TableLogOnInfo logon = new TableLogOnInfo();
        logon.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
        logon.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
        logon.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
        logon.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];

        foreach (Table table in rpt.Database.Tables)
        {
            table.ApplyLogOnInfo(logon);
        }
    }

    // Helper: Add parameter
    private void SetParameter(string name, object value)
    {
        ParameterDiscreteValue pd = new ParameterDiscreteValue();
        pd.Value = value;
        crptPaySlipList.ParameterFieldInfo[name].CurrentValues.Add(pd);
    }

    protected void ShowMessage(string Message)
    {
        Response.Write("Error: " + Message);
    }
}
