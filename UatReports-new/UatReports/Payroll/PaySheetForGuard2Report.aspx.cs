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

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptPaySheetGuard1.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                crptPaySheetGuard.ToolPanelView = ToolPanelViewType.None;
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


            crptPaySheetGuard.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptPaySheetGuard.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptPaySheetGuard.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptPaySheetGuard.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptPaySheetGuard.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptPaySheetGuard.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptPaySheetGuard.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptPaySheetGuard.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptPaySheetGuard.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
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
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramEmpTypeOverride = new ParameterDiscreteValue();
                paramEmpTypeOverride.Value = "Guard";
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmpTypeOverride);
                // Append EMP_ROLE filter to restrict to Others employees only
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                crptPaySheetGuard.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
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
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")";
                else
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} = 0";
            }

            if (this.Page.Request.QueryString["Branch"] != "0")
            {
                crptPaySheetGuard1.ReportDocument.RecordSelectionFormula += " AND {BranchMaster.Code}={?Branch} ";
                crptPaySheetGuard.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            }
            else
            {
                crptPaySheetGuard.ParameterFieldInfo["Branch"].CurrentValues.Add("ALL BRANCHES");//All Branch
            }

            switch (int.Parse(Request.QueryString["RepOption"]))
            {
                case 0:
                    //crptPaySlip.ReportDocument.ParameterFields[11].CurrentValues.AddValue("ALL");
                    crptPaySheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add("ALL");
                    break;
                case 1:
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Bank'";
                    crptPaySheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add("BANK");
                    break;
                case 2:
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Cash'";
                    crptPaySheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add("CASH");
                    break;
                case 3:
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.PayMode} = 'Cheque'";
                    crptPaySheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add("CHEQUE");
                    break;
                case 4:
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.TMPGUARD} = true";
                    crptPaySheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add("Temporary Guard");
                    break;
                case 5:
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 0";
                    crptPaySheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add("Local Guard");
                    break;
                case 6:
                    crptPaySheetGuard1.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CITIZEN} = 1";
                    crptPaySheetGuard.ParameterFieldInfo["RepOption"].CurrentValues.Add("Foreigner Guard");
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