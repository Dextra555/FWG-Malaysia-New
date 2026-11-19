using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_NetPayReportGuard2 : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptNetpay.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                crptNetpayList.ToolPanelView = ToolPanelViewType.None;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }
            //constant header fields
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

            //parameter fields

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = Request.QueryString["Branch"];

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

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

            crptNetpayList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptNetpayList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptNetpayList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptNetpayList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptNetpayList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptNetpayList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptNetpayList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptNetpayList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptNetpayList.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            crptNetpayList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);



            if (!string.IsNullOrEmpty(Request.QueryString["PaymentType"]) && Request.QueryString["PaymentType"].ToUpper() != "ALL")
            {
                crptNetpay.ReportDocument.RecordSelectionFormula += " AND {@SalaryPaymode}= '" + Request.QueryString["PaymentType"] + "'";
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
