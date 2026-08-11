using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_BranchCheckRoleReport : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptCheckRole.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                crptCheckRoleList.ToolPanelView = ToolPanelViewType.None;
                crptCheckRoleList.Zoom(135);
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
            //int periodYear;
            DateTime periodDate;
            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();

            if (DateTime.TryParse(period, out periodDate))
            {
                // Extract the year from the parsed date
                paramPeriod.Value = periodDate;
            }
            else
            {
                // Extract the year from the current date
                paramPeriod.Value = DateTime.Now;
            }

            // Assign the year value to the parameter
            //paramPeriod.Value = periodYear;



            crptCheckRoleList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptCheckRoleList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptCheckRoleList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptCheckRoleList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptCheckRoleList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptCheckRoleList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptCheckRoleList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptCheckRoleList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            crptCheckRoleList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptCheckRoleList.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
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
