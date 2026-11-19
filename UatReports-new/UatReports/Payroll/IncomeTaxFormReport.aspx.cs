using System;
using System.Configuration;
using System.IO;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_IncometaxFormReport : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptIncometaxForm.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                crptIncometaxFormList.ToolPanelView = ToolPanelViewType.None;
                crptIncometaxFormList.Zoom(100);
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

            //parameter fields
            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = Request.QueryString["Branch"];

            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();
            paramPeriod.Value = Request.QueryString["Period"];

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = Request.QueryString["EmployeeType"];

            crptIncometaxFormList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptIncometaxFormList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            crptIncometaxFormList.ParameterFieldInfo["TaxYear"].CurrentValues.Add(paramPeriod);
            crptIncometaxFormList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);

            // Add selection formula if Employee is specified 
            if (!string.IsNullOrEmpty(this.Page.Request.QueryString["Employee"]))
            {
                crptIncometaxForm.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_CODE}='" + Request.QueryString["Employee"] + "'";
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
