using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_MiscTransReport : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptMiscTrans.ReportDocument.Database.Tables)
            {
                crptMiscTransList.ToolPanelView = ToolPanelViewType.None;
                crptMiscTransList.Zoom(140);
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // ── Company header parameters ─────────────────────────────────
            ParameterDiscreteValue paramCompanyName = new ParameterDiscreteValue();
            paramCompanyName.Value = ConfigurationManager.AppSettings["CompanyName"];

            ParameterDiscreteValue paramAddress1 = new ParameterDiscreteValue();
            paramAddress1.Value = ConfigurationManager.AppSettings["Address1"];

            ParameterDiscreteValue paramAddress2 = new ParameterDiscreteValue();
            paramAddress2.Value = ConfigurationManager.AppSettings["Address2"];

            ParameterDiscreteValue paramPostCodeCity = new ParameterDiscreteValue();
            paramPostCodeCity.Value = ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"];

            ParameterDiscreteValue paramState = new ParameterDiscreteValue();
            paramState.Value = ConfigurationManager.AppSettings["State"];

            ParameterDiscreteValue paramRegistration = new ParameterDiscreteValue();
            paramRegistration.Value = ConfigurationManager.AppSettings["Registration"];

            ParameterDiscreteValue paramPhone = new ParameterDiscreteValue();
            paramPhone.Value = ConfigurationManager.AppSettings["Phone"];

            // ── Query string values ───────────────────────────────────────
            string branch       = Request.QueryString["Branch"]       ?? "";
            string employeeType = Request.QueryString["EmployeeType"] ?? "Guard";
            string transTypeStr = Request.QueryString["TransType"]    ?? "1";
            string loginID      = Request.QueryString["LoginID"]      ?? "";
            string period       = Request.QueryString["Period"]       ?? "";

            DateTime periodDate;
            if (!DateTime.TryParse(period, out periodDate))
                periodDate = DateTime.Now;

            int transTypeInt;
            if (!int.TryParse(transTypeStr, out transTypeInt))
                transTypeInt = 1;

            // ── NOTE: RecordSelectionFormula is NOT set here because the   ──
            // ── .rpt already has its own formula using {?Branch} parameter: ──
            // ── ({?Branch} = "ALL BRANCHES" OR {BranchMaster.Code} = {?Branch}) ──
            // ── Passing "ALL BRANCHES" when no branch selected handles all  ──
            // ── branches automatically via the .rpt formula.                ──

            // When branch is empty = All Branches → pass "ALL BRANCHES"
            // so the .rpt record selection formula shows all records.
            string branchParam = string.IsNullOrEmpty(branch) ? "ALL BRANCHES" : branch;

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = branchParam;

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = loginID;

            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();
            paramPeriod.Value = periodDate;

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = employeeType;

            ParameterDiscreteValue paramTransType = new ParameterDiscreteValue();
            paramTransType.Value = transTypeInt;

            // ── Assign parameters to viewer ───────────────────────────────
            crptMiscTransList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptMiscTransList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptMiscTransList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptMiscTransList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptMiscTransList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptMiscTransList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptMiscTransList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptMiscTransList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            crptMiscTransList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptMiscTransList.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            crptMiscTransList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
            crptMiscTransList.ParameterFieldInfo["TransType"].CurrentValues.Add(paramTransType);

            // ── Report title ──────────────────────────────────────────────
            ParameterDiscreteValue paramTitle = new ParameterDiscreteValue();
            paramTitle.Value = (transTypeStr == "1" ? "MISCELLENEOUS EARNINGS STATEMENT FOR " : "MISCELLENEOUS DEDUCTIONS STATEMENT FOR ")
                + periodDate.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            crptMiscTransList.ParameterFieldInfo["Title"].CurrentValues.Add(paramTitle);
        }
        catch (ArgumentNullException ex)
        {
            ShowMessage("Data Cannot be null. " + ex.Message);
        }
        catch (Exception ex)
        {
            ShowMessage("Error Found. " + ex.Message);
        }
	}

    protected void ShowMessage(string Message)
    {
        Response.Write("Error: " + Message);
    }
}
