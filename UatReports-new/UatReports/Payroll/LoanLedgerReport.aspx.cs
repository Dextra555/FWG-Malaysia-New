using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Payroll_LoanLedgerReport : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourceData.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                CrystalReportViewerView.ToolPanelView = ToolPanelViewType.None;
                CrystalReportViewerView.Zoom(100);
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


            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = Request.QueryString["Branch"];

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = Request.QueryString["EmployeeType"];

            string periodStart = Request.QueryString["StartDate"];
            DateTime periodStartDate;
            ParameterDiscreteValue paramStartDate = new ParameterDiscreteValue();

            if (DateTime.TryParse(periodStart, out periodStartDate))
            {

                paramStartDate.Value = periodStartDate;
            }
            else
            {
                paramStartDate.Value = DateTime.Now;
            }

            string periodEnd = Request.QueryString["EndDate"];
            DateTime periodEndDate;
            ParameterDiscreteValue paramEndDate = new ParameterDiscreteValue();

            if (DateTime.TryParse(periodEnd, out periodEndDate))
            {

                paramEndDate.Value = periodEndDate;
            }
            else
            {
                paramEndDate.Value = DateTime.Now;
            }

            CrystalReportViewerView.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerView.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerView.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            CrystalReportViewerView.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);            
            CrystalReportViewerView.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);
            CrystalReportViewerView.ParameterFieldInfo["TransStartDate"].CurrentValues.Add(paramStartDate);
            CrystalReportViewerView.ParameterFieldInfo["TransEndDate"].CurrentValues.Add(paramEndDate);


            if (Request.QueryString["Employee"].Length > 0)
            {
                if (CrystalReportSourceData.ReportDocument.RecordSelectionFormula.Length > 0)
                    CrystalReportSourceData.ReportDocument.RecordSelectionFormula += " AND {command.EMP_CODE}='" + Request.QueryString["Employee"] + "'";
                else
                    CrystalReportSourceData.ReportDocument.RecordSelectionFormula += " {command.EMP_CODE}='" + Request.QueryString["Employee"] + "'";
            }
            if (Request.QueryString["AdvanceType"].Length > 0)
            {
                if ((int.Parse(Request.QueryString["AdvanceType"])) > 0)
                {
                    if (CrystalReportSourceData.ReportDocument.RecordSelectionFormula.Length > 0)
                        CrystalReportSourceData.ReportDocument.RecordSelectionFormula += " AND {command.TransType}=" + Request.QueryString["AdvanceType"] + "";
                    else
                        CrystalReportSourceData.ReportDocument.RecordSelectionFormula += "{command.TransType}=" + Request.QueryString["AdvanceType"] + "";
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