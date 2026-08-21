using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class _Default : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourceData.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                CrystalReportViewerView.ToolPanelView = ToolPanelViewType.None;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

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
           
            string employeeTypeRaw = Request.QueryString["EmployeeType"] ?? "Guard";
            bool isAllTypes = string.IsNullOrEmpty(employeeTypeRaw) || employeeTypeRaw.Equals("All", StringComparison.OrdinalIgnoreCase);

            // Crystal Report {?EmployeeType} parameter does not support "All" — pass "Guard" as a dummy value
            // and override the selection formula below when "All" is requested
            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = isAllTypes ? "Guard" : employeeTypeRaw;

            string period = Request.QueryString["Period"];
            DateTime periodDate;
            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();

            if (DateTime.TryParse(period, out periodDate))
            {
                paramPeriod.Value = periodDate;
            }
            else
            {
                paramPeriod.Value = DateTime.Now;
            }

            string branch = Request.QueryString["Branch"] ?? "";

            CrystalReportViewerView.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerView.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerView.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
            CrystalReportViewerView.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);

            // Get the existing formula set by the .rpt so we can append safely
            string existingFormula = CrystalReportSourceData.ReportDocument.RecordSelectionFormula ?? "";

            // Branch filter
            if (!string.IsNullOrEmpty(branch))
            {
                string prefix = string.IsNullOrEmpty(existingFormula) ? "" : " AND ";
                CrystalReportSourceData.ReportDocument.RecordSelectionFormula = existingFormula + prefix + "{BranchMaster.Code} = '" + branch + "'";
                existingFormula = CrystalReportSourceData.ReportDocument.RecordSelectionFormula;
            }
            // If branch is empty → no branch filter, show all branches

            // EmployeeType filter override
            // When "All" is selected we remove any EmployeeType restriction from the formula
            // so all employee types (Guard, Staff, Foreign Guard, Others) are included
            if (isAllTypes)
            {
                // Strip out existing EmployeeType condition added by the .rpt (e.g. {Employee.EMP_TYPE} = {?EmployeeType})
                // by replacing the formula with one that has no EmployeeType restriction.
                // Crystal Reports evaluates {?EmployeeType} at render time — we override by setting
                // a RecordSelectionFormula that does NOT filter on EMP_TYPE at all.
                string newFormula = existingFormula;
                // Remove any segment that filters on EMP_TYPE or EmployeeType parameter
                newFormula = System.Text.RegularExpressions.Regex.Replace(
                    newFormula,
                    @"\s*(AND\s+)?(\{[^}]*EMP_TYPE[^}]*\}\s*=\s*\{[^\}]*EmployeeType[^\}]*\}|\{[^}]*EmployeeType[^}]*\}\s*=\s*\{[^}]*EMP_TYPE[^}]*\})",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                // Clean up leading AND if it became the first condition
                newFormula = System.Text.RegularExpressions.Regex.Replace(newFormula.Trim(), @"^AND\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                CrystalReportSourceData.ReportDocument.RecordSelectionFormula = newFormula;
            }

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
    private bool DoesParameterExist(string parameterName)
    {
        try
        {
            var paramField = CrystalReportViewerView.ParameterFieldInfo[parameterName];
            return paramField != null;
        }
        catch
        {
            return false;
        }
    }
}
