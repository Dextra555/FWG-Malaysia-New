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
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourcePaySheet.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                CrystalReportViewerPaySheet.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewerPaySheet.Zoom(80);
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


            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            //ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();

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

            // For 'Others' (Other Guards) type, override the EMP_ROLE filter via record selection formula
            // since Crystal Report parameter may only allow Guard/Staff values
            string employeeTypeCheck = Request.QueryString["EmployeeType"];
            bool isForeignGuard = (employeeTypeCheck == "FGuard" || employeeTypeCheck == "Foreign Guard");
            if (isForeignGuard)
            {
                crptPaySlip.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 1";
            }
            else             if (!string.IsNullOrEmpty(employeeTypeCheck) && employeeTypeCheck != "Guard" && employeeTypeCheck != "Staff" && employeeTypeCheck != "Foreign Guard" && employeeTypeCheck != "FGuard")
            {
                // Fully replace formula to bypass Crystal parameter restriction for non-standard types
            string existingFormula = CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula;
            // Remove any existing {?EmployeeType} based conditions
            string cleanFormula = System.Text.RegularExpressions.Regex.Replace(
                existingFormula ?? "",
                @"\{Employee\.EMP_ROLE\}\s*=\s*\{[^}]+\}",
                "{Employee.EMP_ROLE} = '" + employeeTypeCheck + "'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            if (string.IsNullOrWhiteSpace(cleanFormula) || cleanFormula == existingFormula)
                CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
                CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                CrystalReportViewerPaySheet.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
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
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")";
                else
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} = 0";
            else
                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula = cleanFormula;
            }

            string branch = Request.QueryString["Branch"];
            if (branch != "" && branch != "0")
            {
                ParameterDiscreteValue paramBranch = new ParameterDiscreteValue { Value = branch };
                CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);

                // Apply record selection formula
                CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {BranchMaster.Code} = '" + branch + "'";
            }
            else
            {
                ParameterDiscreteValue paramBranchAll = new ParameterDiscreteValue { Value = "ALL BRANCHES" };
                CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranchAll);
            }



            //if (this.Page.Request.QueryString["Branch"] != "0")
            //{
            //    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {BranchMaster.Code}={?Branch} ";

            //    paramBranch.Value = Request.QueryString["Branch"];
            //    CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            //}
            //else
            //{
            //    paramBranch.Value = "ALL BRANCHES";
            //    CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            //}


            switch (int.Parse(Request.QueryString["RepOption"]))
            {
                case 0:
                    paramPayType.Value = "ALL";
                    CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);

                    break;
                case 1:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Bank'";
          
                    paramPayType.Value = "BANK";
                    CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);

                    break;
                case 2:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Cash'";
                    paramPayType.Value = "CASH";
                    CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);

                    break;
                case 3:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Cheque'";
                    paramPayType.Value = "CHEQUE";
                    CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);

                    break;
                case 4:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.TMPGUARD} = true";
                    paramPayType.Value = "Temporary Guard";
                    CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);

                    break;
                case 5:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 0";
                    paramPayType.Value = "Local Guard";
                    CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);

                    break;
                case 6:
                    CrystalReportSourcePaySheet.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 1";
                    paramPayType.Value = "Foreigner Guard";
                    CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);

                    break;
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
}