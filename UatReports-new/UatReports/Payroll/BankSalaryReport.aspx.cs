using CrystalDecisions.Shared;
using CrystalDecisions.Web;
using System.Configuration;
using System;
using System.Web.Security;

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
                CrystalReportViewerView.Zoom(140);
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

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = Request.QueryString["EmployeeType"];

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = Request.QueryString["Branch"];

            ParameterDiscreteValue paramBank = new ParameterDiscreteValue();
            paramBank.Value = Request.QueryString["Bank"];

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


            CrystalReportViewerView.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerView.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerView.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
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
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramEmpTypeOverride = new ParameterDiscreteValue();
                paramEmpTypeOverride.Value = "Guard";
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmpTypeOverride);
                // Append EMP_ROLE filter to restrict to Others employees only
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramGuardOverride = new ParameterDiscreteValue();
                paramGuardOverride.Value = "Guard";
                CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramGuardOverride);
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
                    CrystalReportSourceData.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")";
                else
                    CrystalReportSourceData.ReportDocument.RecordSelectionFormula +=
                        " AND {PaySlip.EmployeeID} = 0";
            }
            CrystalReportViewerView.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            CrystalReportViewerView.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);            
            if (this.Page.Request.QueryString["Branch"] != "0")
            {
                CrystalReportViewerView.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            }
            else
            {
                ParameterDiscreteValue paramBranchAll = new ParameterDiscreteValue();
                paramBranchAll.Value = "ALL BRANCHES";
                CrystalReportViewerView.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranchAll);
            }
            if (this.Page.Request.QueryString["Bank"] != "0")
                CrystalReportSourceData.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.EMPFL_BANK}= '" + paramBank.Value + "'";
            if (this.Page.Request.QueryString["Branch"] != "0")
                CrystalReportSourceData.ReportDocument.RecordSelectionFormula += " AND {BranchMaster.Code}= '" + paramBranch.Value + "'";


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