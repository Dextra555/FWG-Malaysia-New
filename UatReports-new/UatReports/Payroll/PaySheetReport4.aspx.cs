using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Payroll_PaySheetReport4 : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourcePaySheet.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                CrystalReportViewerPaySheet.ToolPanelView = ToolPanelViewType.None;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                CrystalReportViewerPaySheet.Zoom(100);
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

            paramBranch.Value = Request.QueryString["Branch"];
            CrystalReportViewerPaySheet.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);

            paramPayType.Value = "ALL";
            CrystalReportViewerPaySheet.ParameterFieldInfo["PayType"].CurrentValues.Add(paramPayType);
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