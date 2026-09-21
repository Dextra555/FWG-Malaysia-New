using System;
using System.Configuration;
using System.Text;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Web;

public partial class PayRoll_PaySlipReport : System.Web.UI.Page
{
    // Diagnostic log — collects entries throughout the lifecycle
    private StringBuilder _log = new StringBuilder();

    private void Log(string msg)
    {
        _log.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg);
    }

    // ── LIFECYCLE: Page_Init fires BEFORE Page_Load ───────────────────────────
    protected void Page_Init(object sender, EventArgs e)
    {
        Log("=== Page_Init START ===");
        Log("IsPostBack=" + IsPostBack);
        Log("ClientCode from QS = '" + (Request.QueryString["ClientCode"] ?? "(null)") + "'");

        // Log viewer state at Init time (before Page_Load)
        try { Log("Viewer.EnableParameterPrompt at Init = " + crptPaySlipList.EnableParameterPrompt); }
        catch (Exception ex) { Log("Viewer.EnableParameterPrompt read error: " + ex.Message); }

        // Count parameters in ParameterFieldInfo at Init time
        try
        {
            int count = crptPaySlipList.ParameterFieldInfo.Count;
            Log("ParameterFieldInfo.Count at Init = " + count);
            foreach (ParameterField pf in crptPaySlipList.ParameterFieldInfo)
                Log("  PARAM at Init: '" + pf.Name + "'  HasCurrentValue=" + (pf.CurrentValues.Count > 0));
        }
        catch (Exception ex) { Log("ParameterFieldInfo at Init error: " + ex.Message); }

        Log("=== Page_Init END ===");
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Log("=== Page_Load START ===");
        Log("IsPostBack=" + IsPostBack);
        Log("ClientCode from QS = '" + (Request.QueryString["ClientCode"] ?? "(null)") + "'");

        try
        {
            // ── STEP 1: Suppress prompts ──────────────────────────────────────
            Log("STEP 1: Setting EnableParameterPrompt=false and ToolPanelView=None");
            crptPaySlipList.EnableParameterPrompt = false;
            crptPaySlipList.ToolPanelView = ToolPanelViewType.None;
            Log("  EnableParameterPrompt is now: " + crptPaySlipList.EnableParameterPrompt);

            // ── STEP 2: DB Login ──────────────────────────────────────────────
            Log("STEP 2: DB Login");
            int tableCount = 0;
            foreach (Table table in crptPaySlip.ReportDocument.Database.Tables)
            {
                tableCount++;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }
            Log("  Tables logged in: " + tableCount);

            // ── STEP 3: List ALL parameters in ReportDocument.ParameterFields ─
            Log("STEP 3: Parameters in ReportDocument.ParameterFields:");
            try
            {
                foreach (ParameterFieldDefinition pfd in crptPaySlip.ReportDocument.DataDefinition.ParameterFields)
                    Log("  RPT PARAM: '" + pfd.Name + "'  Type=" + pfd.ParameterValueKind);
            }
            catch (Exception ex) { Log("  ReportDocument.ParameterFields error: " + ex.Message); }

            // ── STEP 4: List ALL parameters in Viewer.ParameterFieldInfo ──────
            Log("STEP 4: Parameters in Viewer.ParameterFieldInfo (BEFORE AddParam):");
            try
            {
                foreach (ParameterField pf in crptPaySlipList.ParameterFieldInfo)
                    Log("  VIEWER PARAM: '" + pf.Name + "'  CurrentValues.Count=" + pf.CurrentValues.Count);
            }
            catch (Exception ex) { Log("  ParameterFieldInfo enumeration error: " + ex.Message); }

            // ── STEP 5: Set parameters ────────────────────────────────────────
            Log("STEP 5: Setting parameters via AddParam...");
            AddParam("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            AddParam("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            AddParam("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            AddParam("CompanyAddress3",
                (ConfigurationManager.AppSettings["PostCode"] ?? "") + " " +
                (ConfigurationManager.AppSettings["City"]     ?? ""));
            AddParam("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            AddParam("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            AddParam("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);
            AddParam("LoginID",             Request.QueryString["LoginID"]      ?? "");
            AddParam("Branch",              Request.QueryString["Branch"]       ?? "");
            AddParam("EmployeeType",        Request.QueryString["EmployeeType"] ?? "Guard");

            DateTime periodDate;
            if (!DateTime.TryParse(Request.QueryString["Period"], out periodDate))
                periodDate = DateTime.Now;
            AddParam("Period", periodDate);
            Log("  Period value set: " + periodDate.ToString("yyyy-MM-dd"));

            string clientCode = Request.QueryString["ClientCode"] ?? "";
            Log("  About to AddParam ClientCode = '" + clientCode + "'");
            AddParam("ClientCode", clientCode);
            Log("  AddParam ClientCode DONE");

            // ── STEP 6: Verify ClientCode in viewer AFTER setting ─────────────
            Log("STEP 6: Viewer.ParameterFieldInfo AFTER AddParam:");
            try
            {
                foreach (ParameterField pf in crptPaySlipList.ParameterFieldInfo)
                {
                    string vals = "";
                    foreach (ParameterValue pv in pf.CurrentValues)
                        vals += (pv is ParameterDiscreteValue ? ((ParameterDiscreteValue)pv).Value?.ToString() : "?") + "; ";
                    Log("  VIEWER PARAM: '" + pf.Name + "'  Count=" + pf.CurrentValues.Count + "  Values=[" + vals + "]");
                }
            }
            catch (Exception ex) { Log("  ParameterFieldInfo post-set error: " + ex.Message); }

            // ── STEP 7: Record selection formula extras ───────────────────────
            Log("STEP 7: RecordSelectionFormula = '" + crptPaySlip.ReportDocument.RecordSelectionFormula + "'");

            string employeeTypeCheck = Request.QueryString["EmployeeType"] ?? "";
            if (employeeTypeCheck == "FGuard" || employeeTypeCheck == "Foreign Guard")
            {
                crptPaySlip.ReportDocument.RecordSelectionFormula =
                    AppendFormula(crptPaySlip.ReportDocument.RecordSelectionFormula,
                        "{Employee.EMP_CITIZEN} = 1");
                Log("  Appended ForeignGuard condition");
            }
            else if (!string.IsNullOrEmpty(employeeTypeCheck)
                && employeeTypeCheck != "Guard"
                && employeeTypeCheck != "Staff")
            {
                crptPaySlipList.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue pdEmpOverride = new ParameterDiscreteValue();
                pdEmpOverride.Value = "Guard";
                crptPaySlipList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(pdEmpOverride);
                Log("  OtherGuards: overrode EmployeeType to Guard");

                string connStr = ConfigurationManager.ConnectionStrings["obms"]?.ConnectionString
                    ?? string.Format("Server={0};Database={1};User Id={2};Password={3};",
                        ConfigurationManager.AppSettings["Server"],
                        ConfigurationManager.AppSettings["Database"],
                        ConfigurationManager.AppSettings["UserID"],
                        ConfigurationManager.AppSettings["Password"]);
                var empIds = new System.Collections.Generic.List<string>();
                using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new System.Data.SqlClient.SqlCommand(
                        "SELECT EMP_ID FROM Employee WHERE EMP_ROLE = @role", conn))
                    {
                        cmd.Parameters.AddWithValue("@role", employeeTypeCheck);
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read()) empIds.Add(reader.GetInt32(0).ToString());
                    }
                }
                string empCond = empIds.Count > 0
                    ? "{Payslip.EmployeeID} IN (" + string.Join(",", empIds) + ")"
                    : "{Payslip.EmployeeID} = 0";
                crptPaySlip.ReportDocument.RecordSelectionFormula =
                    AppendFormula(crptPaySlip.ReportDocument.RecordSelectionFormula, empCond);
                Log("  Appended OtherGuards EmpID condition, count=" + empIds.Count);
            }

            string employeeCode = Request.QueryString["Employee"] ?? "";
            if (!string.IsNullOrEmpty(employeeCode) && employeeCode != "0")
            {
                crptPaySlip.ReportDocument.RecordSelectionFormula =
                    AppendFormula(crptPaySlip.ReportDocument.RecordSelectionFormula,
                        "{vwPaySheet.EMP_CODE} = '" + employeeCode.Replace("'", "''") + "'");
                Log("  Appended specific Employee condition: " + employeeCode);
            }

            Log("STEP 7 final formula: '" + crptPaySlip.ReportDocument.RecordSelectionFormula + "'");
            Log("=== Page_Load END — about to render viewer ===");
        }
        catch (Exception ex)
        {
            Log("EXCEPTION: " + ex.ToString());
            RenderDiagnostic();
            Response.End();
        }
    }

    // ── Page_PreRender: fires AFTER Page_Load, just before HTML is written ────
    protected void Page_PreRender(object sender, EventArgs e)
    {
        Log("=== Page_PreRender ===");
        Log("  EnableParameterPrompt = " + crptPaySlipList.EnableParameterPrompt);
        try
        {
            foreach (ParameterField pf in crptPaySlipList.ParameterFieldInfo)
            {
                string vals = "";
                foreach (ParameterValue pv in pf.CurrentValues)
                    vals += (pv is ParameterDiscreteValue ? ((ParameterDiscreteValue)pv).Value?.ToString() : "?") + "; ";
                Log("  PARAM at PreRender: '" + pf.Name + "'  Count=" + pf.CurrentValues.Count + "  Values=[" + vals + "]");
            }
        }
        catch (Exception ex) { Log("  PreRender param check error: " + ex.Message); }
    }

    // ── Render: write diagnostic output ABOVE the Crystal viewer ─────────────
    protected override void Render(System.Web.UI.HtmlTextWriter writer)
    {
        Log("=== Render called ===");

        // Write the diagnostic log as a collapsible block above the report
        writer.Write("<details open style='font-family:monospace;font-size:11px;background:#f0f8ff;" +
                     "border:1px solid #99c;padding:8px;margin:4px;max-height:300px;overflow:auto'>");
        writer.Write("<summary style='font-weight:bold;color:#004;cursor:pointer'>▶ PaySlipReport Diagnostic Log (click to collapse)</summary><pre>");
        writer.Write(System.Web.HttpUtility.HtmlEncode(_log.ToString()));
        writer.Write("</pre></details>");

        // Now render the actual Crystal Reports viewer
        base.Render(writer);
    }

    private void RenderDiagnostic()
    {
        Response.Clear();
        Response.Write("<html><body style='font-family:monospace;font-size:12px;padding:16px'>");
        Response.Write("<h3 style='color:red'>PaySlipReport — Diagnostic Log</h3><pre style='background:#fff8f8;border:1px solid red;padding:10px'>");
        Response.Write(System.Web.HttpUtility.HtmlEncode(_log.ToString()));
        Response.Write("</pre></body></html>");
    }

    private void AddParam(string name, object value)
    {
        ParameterDiscreteValue pd = new ParameterDiscreteValue();
        pd.Value = value;
        crptPaySlipList.ParameterFieldInfo[name].CurrentValues.Add(pd);
        Log("  AddParam OK: '" + name + "' = '" + (value ?? "(null)") + "'");
    }

    private string AppendFormula(string existing, string newCondition)
    {
        existing     = (existing     ?? "").Trim();
        newCondition = (newCondition ?? "").Trim();
        if (string.IsNullOrEmpty(existing))     return newCondition;
        if (string.IsNullOrEmpty(newCondition)) return existing;
        return "(" + existing + ") AND (" + newCondition + ")";
    }
}
