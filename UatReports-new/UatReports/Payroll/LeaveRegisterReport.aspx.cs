using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_LeaveRegisterReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Apply DB login to all tables
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptLeaveRegister.ReportDocument.Database.Tables)
            {
                crptLeaveRegisterList.ToolPanelView = ToolPanelViewType.None;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            string loginID      = Request.QueryString["LoginID"]     ?? "";
            string periodStr    = Request.QueryString["Period"]       ?? "";
            string employeeType = Request.QueryString["EmployeeType"] ?? "Guard";
            string branch       = Request.QueryString["Branch"]       ?? "";

            // {LeaveRegister.PeriodYear} is a numeric field — pass as integer
            int periodYear;
            if (!int.TryParse(periodStr, out periodYear))
                periodYear = DateTime.Now.Year;

            // Branch: RPT formula => ({?Branch} = "ALL BRANCHES" OR {BranchMaster.Code} = {?Branch})
            string branchParam = string.IsNullOrEmpty(branch) ? "ALL BRANCHES" : branch;

            // Company header
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

            // Report params
            ParameterDiscreteValue paramLoginID = new ParameterDiscreteValue();
            paramLoginID.Value = loginID;

            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();
            paramPeriod.Value = periodYear;   // integer — matches {LeaveRegister.PeriodYear} number field

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = employeeType;

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = branchParam;

            // Assign all parameters via the viewer
            crptLeaveRegisterList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptLeaveRegisterList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptLeaveRegisterList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptLeaveRegisterList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptLeaveRegisterList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptLeaveRegisterList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptLeaveRegisterList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptLeaveRegisterList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramLoginID);
            crptLeaveRegisterList.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            crptLeaveRegisterList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
            crptLeaveRegisterList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
        }
        catch (ArgumentNullException ex)
        {
            Response.Write("Error: Data Cannot be null. " + ex.Message);
        }
        catch (Exception ex)
        {
            Response.Write("Error Found. " + ex.Message);
        }
    }
}
