using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_PaySlipIIReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptPaySlip.ReportDocument.Database.Tables)
            {
                crptPaySlipList.ToolPanelView = ToolPanelViewType.None;
                crptPaySlipList.Zoom(100);
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // Constant header fields
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

            // Parameter fields
            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = Request.QueryString["Branch"];
            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            string period = Request.QueryString["Period"];
            DateTime periodDate;
            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();
            paramPeriod.Value = DateTime.TryParse(period, out periodDate) ? periodDate : DateTime.Now;

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = Request.QueryString["EmployeeType"];

            crptPaySlipList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptPaySlipList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptPaySlipList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptPaySlipList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptPaySlipList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptPaySlipList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptPaySlipList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptPaySlipList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            //crptPaySlipList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptPaySlipList.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            crptPaySlipList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);

            // Pass ClientCode to rpt parameter only if parameter exists in .rpt (avoids popup)
            TrySetParameter("ClientCode", Request.QueryString["ClientCode"] ?? "");

            string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["obms"]?.ConnectionString
                ?? string.Format("Server={0};Database={1};User Id={2};Password={3};",
                    ConfigurationManager.AppSettings["Server"],
                    ConfigurationManager.AppSettings["Database"],
                    ConfigurationManager.AppSettings["UserID"],
                    ConfigurationManager.AppSettings["Password"]);

            // Build record selection formula
            string formula = crptPaySlip.ReportDocument.RecordSelectionFormula ?? "";

            string employeeTypeCheck = Request.QueryString["EmployeeType"];
            bool isForeignGuard = (employeeTypeCheck == "FGuard" || employeeTypeCheck == "Foreign Guard");
            if (isForeignGuard)
            {
                formula = BuildFormula(formula, "{Employee.EMP_CITIZEN} = 1");
            }
            else if (!string.IsNullOrEmpty(employeeTypeCheck) && employeeTypeCheck != "Guard" && employeeTypeCheck != "Staff")
            {
                crptPaySlipList.ParameterFieldInfo["EmployeeType"].CurrentValues.Clear();
                ParameterDiscreteValue paramEmpTypeOverride = new ParameterDiscreteValue();
                paramEmpTypeOverride.Value = "Guard";
                crptPaySlipList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmpTypeOverride);

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
                string empIdCondition = empIds.Count > 0
                    ? "{PaySlip.EmployeeID} IN (" + string.Join(",", empIds) + ")"
                    : "{PaySlip.EmployeeID} = 0";
                formula = BuildFormula(formula, empIdCondition);
            }

            // Filter by specific Employee
            string employeeCode = Request.QueryString["Employee"];
            if (!string.IsNullOrEmpty(employeeCode) && employeeCode != "0")
            {
                formula = BuildFormula(formula, "{vwPaySheet.EMP_CODE} = '" + employeeCode.Replace("'", "''") + "'");
            }

            // Filter by ClientCode
            string clientCode = Request.QueryString["ClientCode"] ?? "";
            if (!string.IsNullOrEmpty(clientCode))
            {
                var empCodes = new System.Collections.Generic.List<string>();
                using (var clientConn = new System.Data.SqlClient.SqlConnection(connStr))
                {
                    clientConn.Open();
                    var clientCmd = new System.Data.SqlClient.SqlCommand(
                        "SELECT EMP_CODE FROM Employee WHERE EMP_CLIENT = @client", clientConn);
                    clientCmd.Parameters.AddWithValue("@client", clientCode);
                    using (var clientReader = clientCmd.ExecuteReader())
                        while (clientReader.Read()) empCodes.Add("'" + clientReader.GetString(0).Replace("'", "''") + "'");
                }
                string clientCondition = empCodes.Count > 0
                    ? "{vwPaySheet.EMP_CODE} IN (" + string.Join(",", empCodes) + ")"
                    : "{vwPaySheet.EMP_CODE} = '__NO_MATCH__'";
                formula = BuildFormula(formula, clientCondition);
            }

            if (!string.IsNullOrWhiteSpace(formula))
                crptPaySlip.ReportDocument.RecordSelectionFormula = formula;
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

    private string BuildFormula(string existing, string newCondition)
    {
        existing = (existing ?? "").Trim();
        newCondition = (newCondition ?? "").Trim();
        if (string.IsNullOrEmpty(existing)) return newCondition;
        if (string.IsNullOrEmpty(newCondition)) return existing;
        return "(" + existing + ") AND (" + newCondition + ")";
    }

    private void TrySetParameter(string name, object value)
    {
        try
        {
            ParameterDiscreteValue pd = new ParameterDiscreteValue();
            pd.Value = value;
            crptPaySlipList.ParameterFieldInfo[name].CurrentValues.Add(pd);
        }
        catch { /* parameter not in this .rpt — skip */ }
    }

    protected void ShowMessage(string Message)
    {
        Response.Write("Error: " + Message);
    }
}
