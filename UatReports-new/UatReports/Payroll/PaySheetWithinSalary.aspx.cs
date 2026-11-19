using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_PaySheetWithinSalary : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Set .rpt file path dynamically — same as "Show" button (PaySheet.rpt)
            CrystalReportSourcePaySheet.Report.FileName =
                Server.MapPath("~/Payroll/Withinsalary.rpt");

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourcePaySheet.ReportDocument.Database.Tables)
            {
                CrystalReportViewerPaySheet.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewerPaySheet.Zoom(100);
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // Company header parameters
            AddParameter("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            AddParameter("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            AddParameter("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            AddParameter("CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            AddParameter("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            AddParameter("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            AddParameter("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);

            // Login ID
            AddParameter("LoginID", Request.QueryString["LoginID"]);

            // Period
            string period = Request.QueryString["Period"];
            DateTime periodDate;
            if (!DateTime.TryParse(period, out periodDate))
                periodDate = DateTime.Now;
            AddParameter("Period", periodDate);

            // Employee Type
            string employeeTypeCheck = Request.QueryString["EmployeeType"];
            AddParameter("EmployeeType", employeeTypeCheck);

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
                // Override EmployeeType to "Guard" so .rpt formulas work
                CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                AddParameter("EmployeeType", "Guard");

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
                AddParameter("Branch", branch);
                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                    BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                 "{BranchMaster.Code} = '" + branch.Replace("'", "''") + "'");
            }
            else
            {
                AddParameter("Branch", "ALL BRANCHES");
            }

            // Pay type (RepOption) filter
            int repOption = 0;
            int.TryParse(Request.QueryString["RepOption"], out repOption);
            string payTypeValue = "ALL";

            switch (repOption)
            {
                case 1:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.PayMode} = 'Bank'");
                    payTypeValue = "BANK";
                    break;
                case 2:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.PayMode} = 'Cash'");
                    payTypeValue = "CASH";
                    break;
                case 3:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.PayMode} = 'Cheque'");
                    payTypeValue = "CHEQUE";
                    break;
                case 4:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{EmployeeSalaryDetails.TMPGUARD} = true");
                    payTypeValue = "Temporary Guard";
                    break;
                case 5:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{Employee.EMP_CITIZEN} = 0");
                    payTypeValue = "Local Guard";
                    break;
                case 6:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula =
                        BuildFormula(CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula,
                                     "{Employee.EMP_CITIZEN} = 1");
                    payTypeValue = "Foreigner Guard";
                    break;
                default:
                    payTypeValue = "ALL";
                    break;
            }
            AddParameter("PayType", payTypeValue);
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

    private void AddParameter(string name, object value)
    {
        ParameterDiscreteValue param = new ParameterDiscreteValue();
        param.Value = value ?? "";
        CrystalReportViewerPaySheet.ParameterFieldInfo[name].CurrentValues.Add(param);
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
