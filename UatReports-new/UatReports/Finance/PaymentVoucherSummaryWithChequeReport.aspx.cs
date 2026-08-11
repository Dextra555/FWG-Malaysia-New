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
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourcePaymentVoucherSummary.ReportDocument.Database.Tables)
            {
                CrystalReportViewerPaymentVoucherSummary.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewerPaymentVoucherSummary.Zoom(100);
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

            

            ParameterDiscreteValue paramStartDate = new ParameterDiscreteValue();
            paramStartDate.Value = DateTime.Parse(Request.QueryString["StartDate"]);


            ParameterDiscreteValue paramEndDate = new ParameterDiscreteValue();
            paramEndDate.Value = DateTime.Parse(Request.QueryString["EndDate"]);

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);

            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["StartDate"].CurrentValues.Add(paramStartDate);

            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["EndDate"].CurrentValues.Add(paramEndDate);

            CrystalReportViewerPaymentVoucherSummary.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);


            if (Request.QueryString["Bank"] != "0")
                CrystalReportSourcePaymentVoucherSummary.ReportDocument.RecordSelectionFormula += " AND {BankMaster.BankID}=" + Request.QueryString["Bank"];

            if (Request.QueryString["Branch"] != "0")
            {
                CrystalReportSourcePaymentVoucherSummary.ReportDocument.RecordSelectionFormula += " AND {BranchPaymentDetails.Branch}='" + Request.QueryString["Branch"] + "'";
            }
            if (Request.QueryString["PaymentType"] != "0")
            {
                CrystalReportSourcePaymentVoucherSummary.ReportDocument.RecordSelectionFormula += " AND {BranchPayments.PaymentType}=" + Request.QueryString["PaymentType"];
            }
            if (Request.QueryString["Purpose"] != "0")
            {
                CrystalReportSourcePaymentVoucherSummary.ReportDocument.RecordSelectionFormula += " AND {BranchPayments.PaymentPurpose}=" + Request.QueryString["Purpose"];
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