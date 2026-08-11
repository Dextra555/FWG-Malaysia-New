using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_PSheetForGuard2Report : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptPSheetGuard1.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                crptPSheetGuard.ToolPanelView = ToolPanelViewType.None;
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

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
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


            crptPSheetGuard.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptPSheetGuard.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptPSheetGuard.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptPSheetGuard.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptPSheetGuard.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptPSheetGuard.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptPSheetGuard.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptPSheetGuard.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptPSheetGuard.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
            // For 'Others' (Other Guards) type, override the EMP_ROLE filter via record selection formula
            string employeeTypeCheck = Request.QueryString["EmployeeType"];
            bool isForeignGuard = (employeeTypeCheck == "FGuard" || employeeTypeCheck == "Foreign Guard");
            if (isForeignGuard)
            {
                crptPaySlip.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 1";
            }
            else             if (!string.IsNullOrEmpty(employeeTypeCheck) && employeeTypeCheck != "Guard" && employeeTypeCheck != "Staff" && employeeTypeCheck != "Foreign Guard" && employeeTypeCheck != "FGuard")
            {
                // Override Crystal parameter to 'Guard' so passport length condition works correctly for Others
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramEmpTypeOverride = new ParameterDiscreteValue();
                paramEmpTypeOverride.Value = "Guard";
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmpTypeOverride);
                // Append EMP_ROLE filter to restrict to Others employees only
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                crptPSheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
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
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")";
                else
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} = 0";
            }

            if (this.Page.Request.QueryString["Branch"] != "0")
            {
                crptPSheetGuard1.ReportDocument.RecordSelectionFormula += " AND {BranchMaster.Code}={?Branch} "; 
                crptPSheetGuard.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            }
            else
            {
                paramBranch.Value = "ALL BRANCHES";
                crptPSheetGuard.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);//All Branch
            }

            switch (int.Parse(Request.QueryString["RepOption"]))
            {
                case 0:
                    paramPayType.Value = "ALL";
                    crptPSheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add(paramPayType);
                    break;
                case 1:
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Bank'";
                    paramPayType.Value = "BANK";
                    crptPSheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add(paramPayType);
                    break;
                case 2:
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Cash'";
                    paramPayType.Value = "CASH";
                    crptPSheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add(paramPayType);
                    break;
                case 3:
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Cheque'";
                    paramPayType.Value = "CHEQUE";
                    crptPSheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add(paramPayType);
                    break;
                case 4:
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.TMPGUARD} = true";
                    paramPayType.Value = "Temporary Guard";
                    crptPSheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add(paramPayType);
                    break;
                case 5:
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 0";
                    paramPayType.Value = "Local Guard";
                    crptPSheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add(paramPayType);
                    break;
                case 6:
                    crptPSheetGuard1.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 1";
                    paramPayType.Value = "Foreigner Guard";
                    crptPSheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add(paramPayType);
                    break;
            }
        }
        catch (ArgumentNullException ex)
        {
            ShowMessage("Data Cannot be null." + ex.Message);
        }
        catch (Exception ex)
        {
            ShowMessage("Error Found." + ex.Message);
        }
	}
    protected void ShowMessage(string Message)
    {
        Response.Write("Error: " + Message);
    }
}