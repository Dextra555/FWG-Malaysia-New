using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Finance_ReceiptSummaryCustomReport : System.Web.UI.Page
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


            string startDateStr = Request.QueryString["StartDate"];
            string endDateStr = Request.QueryString["EndDate"];


            if (string.IsNullOrEmpty(startDateStr))
            {
                throw new ArgumentNullException("StartDate", "StartDate query string parameter is required.");
            }

            if (string.IsNullOrEmpty(endDateStr))
            {
                throw new ArgumentNullException("EndDate", "EndDate query string parameter is required.");
            }

            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);

            ParameterDiscreteValue paramStartDate = new ParameterDiscreteValue();
            paramStartDate.Value = startDate;

            ParameterDiscreteValue paramEndDate = new ParameterDiscreteValue();
            paramEndDate.Value = endDate;


            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            CrystalReportViewer.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewer.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewer.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);

            CrystalReportViewer.ParameterFieldInfo["StartDate"].CurrentValues.Add(paramStartDate);

            CrystalReportViewer.ParameterFieldInfo["EndDate"].CurrentValues.Add(paramEndDate);

            CrystalReportViewer.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);

            //if (Request.QueryString["Bank"] != "0" && Request.QueryString["Bank"] != "")
            //{
            //    CrystalReportSource.ReportDocument.RecordSelectionFormula += " AND {Receipts.BankID}=" + Request.QueryString["Bank"];
            //}

            if (!string.IsNullOrEmpty(Request.QueryString["Bank"]) && Request.QueryString["Bank"] != "0")
            {
                // Validate Bank QueryString is a valid number
                int bankId;
                if (int.TryParse(Request.QueryString["Bank"], out bankId))
                {
                    CrystalReportSource.ReportDocument.RecordSelectionFormula +=
                        string.Format(" AND {{Receipts.BankID}}={0}", bankId);
                }                
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
