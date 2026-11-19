using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_PaySheetWithSalary : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Set .rpt file path dynamically so Crystal Reports resolves it correctly
            CrystalReportSourcePaySheet.Report.FileName =
                Server.MapPath("~/Payroll/WithShift2Report.rpt");

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourcePaySheet.ReportDocument.Database.Tables)
            {
                CrystalReportViewerPaySheet.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewerPaySheet.Zoom(80);
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // Company header parameters
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

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            // Period
            string period = Request.QueryString["Period"];
            DateTime periodDate;
            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();
            if (DateTime.TryParse(period, out periodDate))
                paramPeriod.Value = periodDate;
            else
                paramPeriod.Value = DateTime.Now;

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = Request.QueryString["EmployeeType"];

            ParameterDiscreteValue paramPayType = new ParameterDiscreteValue();

            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerPaySheet.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            CrystalReportViewerPaySheet.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            CrystalReportViewerPaySheet.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);

            // EmployeeType: handle "Others / Other Guards"
            string employeeTypeCheck = Request.QueryString["EmployeeType"];
            bool isForeignGuard = (employeeTypeCheck == "FGuard" || employeeTypeCheck == "Foreign Guard");

            if (isForeignGuard)
            {
                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                    BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                 "{Employee.EMP_CITIZEN} = 1");
            }
            else if (!string.IsNullOrEmpty(employeeTypeCheck) &&
                     employeeTypeCheck != "Guard" &&
                     employeeTypeCheck != "Staff")
            {
                // Override Crystal EmployeeType param to "Guard" so .rpt formulas still work
                CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);

                string connStr = string.Format("Server={0};Database={1};User Id={2};Password={3};",
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

                string empCondition = empIds.Count > 0
                    ? "{PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")"
                    : "{PaySlip.EmployeeID} = 0";
                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                    BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula, empCondition);
            }

            // Branch filter
            string branch = Request.QueryString["Branch"];
            if (!string.IsNullOrEmpty(branch) && branch != "0")
            {
                ParameterDiscreteValue paramBranch = new ParameterDiscreteValue { Value = branch };
                CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                    BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                 "{BranchMaster.Code} = '" + branch.Replace("'", "''") + "'");
            }
            else
            {
                ParameterDiscreteValue paramBranchAll = new ParameterDiscreteValue { Value = "ALL BRANCHES" };
                CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranchAll);
            }

            // Pay type (RepOption) filter
            int repOption = 0;
            int.TryParse(Request.QueryString["RepOption"], out repOption);
            switch (repOption)
            {
                case 0:
                    paramPayType.Value = "ALL";
                    break;
                case 1:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.PayMode} = 'Bank'");
                    paramPayType.Value = "BANK";
                    break;
                case 2:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.PayMode} = 'Cash'");
                    paramPayType.Value = "CASH";
                    break;
                case 3:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.PayMode} = 'Cheque'");
                    paramPayType.Value = "CHEQUE";
                    break;
                case 4:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.TMPGUARD} = true");
                    paramPayType.Value = "Temporary Guard";
                    break;
                case 5:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{Employee.EMP_CITIZEN} = 0");
                    paramPayType.Value = "Local Guard";
                    break;
                case 6:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{Employee.EMP_CITIZEN} = 1");
                    paramPayType.Value = "Foreigner Guard";
                    break;
                default:
                    paramPayType.Value = "ALL";
                    break;
            }
            CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);
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

    private string BuildFormula(string existing, string newCondition)
    {
        existing     = (existing     ?? "").Trim();
        newCondition = (newCondition ?? "").Trim();
        if (string.IsNullOrEmpty(existing))     return newCondition;
        if (string.IsNullOrEmpty(newCondition)) return existing;
        return "(" + existing + ") AND (" + newCondition + ")";
    }
}
