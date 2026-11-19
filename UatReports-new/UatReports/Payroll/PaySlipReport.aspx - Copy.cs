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

            // ================= RECORD SELECTION FORMULA =================
            string formula = crptPaySlip.ReportDocument.RecordSelectionFormula ?? "";

            // NOTE: EmployeeType filtering is handled by the {?EmployeeType} parameter set above.
            // The .rpt base formula uses {vwPaySheet.EMP_ROLE} = {?EmployeeType} — no extra
            // IN-list condition needed for any role type including "Others".

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
