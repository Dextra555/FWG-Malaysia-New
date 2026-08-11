using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;


public partial class Finance_BranchPaymentReport : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{       

        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSource.ReportDocument.Database.Tables)
            {
                CrystalReportViewer.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewer.Zoom(100);
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

            ParameterDiscreteValue paramCategory = new ParameterDiscreteValue();
            paramCategory.Value = Request.QueryString["Category"];

            ParameterDiscreteValue paramASN = new ParameterDiscreteValue();
            paramASN.Value = Request.QueryString["ASN"];

            ParameterDiscreteValue paramPaymentID = new ParameterDiscreteValue();
            paramPaymentID.Value = Request.QueryString["ID"];

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            CrystalReportViewer.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewer.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewer.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);

            CrystalReportViewer.ParameterFieldInfo["ItemCategory"].CurrentValues.Add(paramCategory);
            CrystalReportViewer.ParameterFieldInfo["AccShortName"].CurrentValues.Add(paramASN);
            CrystalReportViewer.ParameterFieldInfo["UserName"].CurrentValues.Add(paramUserName);
            CrystalReportViewer.ParameterFieldInfo["PaymentID"].CurrentValues.Add(paramPaymentID);

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
