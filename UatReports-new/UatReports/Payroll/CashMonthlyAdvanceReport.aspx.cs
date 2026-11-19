using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_CashMonthlyAdvanceReport : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptBankAdvance.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                crptBankAdvanceList.ToolPanelView = ToolPanelViewType.None;
                crptBankAdvanceList.Zoom(100);
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

            string period = Request.QueryString["AdvancePeriod"];
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



            crptBankAdvanceList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptBankAdvanceList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptBankAdvanceList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptBankAdvanceList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            crptBankAdvanceList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptBankAdvanceList.ParameterFieldInfo["AdvancePeriod"].CurrentValues.Add(paramPeriod);
            crptBankAdvanceList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
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
