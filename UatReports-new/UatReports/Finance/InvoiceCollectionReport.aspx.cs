using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class Finance_InvoiceCollectionReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Crystal Report DB Connection
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

            // Company Details
            ParameterDiscreteValue paramCompanyName = new ParameterDiscreteValue();
            paramCompanyName.Value = ConfigurationManager.AppSettings["CompanyName"];

            ParameterDiscreteValue paramAddress1 = new ParameterDiscreteValue();
            paramAddress1.Value = ConfigurationManager.AppSettings["Address1"];

            ParameterDiscreteValue paramAddress2 = new ParameterDiscreteValue();
            paramAddress2.Value = ConfigurationManager.AppSettings["Address2"];

            ParameterDiscreteValue paramPostCodeCity = new ParameterDiscreteValue();
            paramPostCodeCity.Value =
                ConfigurationManager.AppSettings["PostCode"] + " " +
                ConfigurationManager.AppSettings["City"];

            ParameterDiscreteValue paramState = new ParameterDiscreteValue();
            paramState.Value = ConfigurationManager.AppSettings["State"];

            ParameterDiscreteValue paramRegistration = new ParameterDiscreteValue();
            paramRegistration.Value = ConfigurationManager.AppSettings["Registration"];

            ParameterDiscreteValue paramPhone = new ParameterDiscreteValue();
            paramPhone.Value = ConfigurationManager.AppSettings["Phone"];

            // URL Parameters
            string StartDate = Request.QueryString["StartDate"];
            string EndDate = Request.QueryString["EndDate"];
            string LoginID = Request.QueryString["LoginID"];
            string Branch = Request.QueryString["Branch"];

            // Start Date
            ParameterDiscreteValue paramStartDate = new ParameterDiscreteValue();
            paramStartDate.Value = DateTime.Parse(StartDate);

            // End Date
            ParameterDiscreteValue paramEndDate = new ParameterDiscreteValue();
            paramEndDate.Value = DateTime.Parse(EndDate);

            // Login User
            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = LoginID;

            // Assign Parameters
            CrystalReportViewer.ParameterFieldInfo["CompanyName"]
                .CurrentValues.Add(paramCompanyName);

            CrystalReportViewer.ParameterFieldInfo["CompanyAddress1"]
                .CurrentValues.Add(paramAddress1);

            CrystalReportViewer.ParameterFieldInfo["CompanyAddress2"]
                .CurrentValues.Add(paramAddress2);

            CrystalReportViewer.ParameterFieldInfo["CompanyAddress3"]
                .CurrentValues.Add(paramPostCodeCity);

            CrystalReportViewer.ParameterFieldInfo["CompanyAddress4"]
                .CurrentValues.Add(paramState);

            CrystalReportViewer.ParameterFieldInfo["CompanyRegistration"]
                .CurrentValues.Add(paramRegistration);

            CrystalReportViewer.ParameterFieldInfo["CompanyPhone"]
                .CurrentValues.Add(paramPhone);

            CrystalReportViewer.ParameterFieldInfo["StartDate"]
                .CurrentValues.Add(paramStartDate);

            CrystalReportViewer.ParameterFieldInfo["EndDate"]
                .CurrentValues.Add(paramEndDate);

            CrystalReportViewer.ParameterFieldInfo["LoginID"]
                .CurrentValues.Add(paramUserName);

            // Branch Optional
            // If branch empty => show all branches
            if (!string.IsNullOrEmpty(Branch))
            {
                ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
                paramBranch.Value = Branch;

                CrystalReportViewer.ParameterFieldInfo["Branch"]
                    .CurrentValues.Add(paramBranch);
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