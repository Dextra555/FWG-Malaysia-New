using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Web;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Force Crystal to use runtime connection instead of design-time
            CrystalReportSourcePaymentDueList.ReportDocument.SetDatabaseLogon(
                ConfigurationManager.AppSettings["UserID"],
                ConfigurationManager.AppSettings["Password"],
                ConfigurationManager.AppSettings["Server"],
                ConfigurationManager.AppSettings["Database"]
            );

            // Apply connection info to all main report tables
            foreach (Table table in CrystalReportSourcePaymentDueList.ReportDocument.Database.Tables)
            {
                ApplyTableLogin(table);
            }

            // Apply connection info to all subreport tables
            foreach (ReportDocument subreport in CrystalReportSourcePaymentDueList.ReportDocument.Subreports)
            {
                foreach (Table table in subreport.Database.Tables)
                {
                    ApplyTableLogin(table);
                }
            }

            // Verify the report structure is up to date
            CrystalReportSourcePaymentDueList.ReportDocument.VerifyDatabase();

            // Viewer setup
            CrystalReportViewerPaymentDueList.ToolPanelView = ToolPanelViewType.None;
            CrystalReportViewerPaymentDueList.Zoom(100);
            CrystalReportViewerPaymentDueList.ReportSource = CrystalReportSourcePaymentDueList;

            // Apply parameters
            SetReportParameters();
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

    private void ApplyTableLogin(Table table)
    {
        TableLogOnInfo logonInfo = table.LogOnInfo;
        logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
        logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
        logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
        logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];
        logonInfo.ConnectionInfo.IntegratedSecurity = false;

        table.ApplyLogOnInfo(logonInfo);

        // Reset location to avoid design-time DB references
        if (!string.IsNullOrEmpty(table.Location))
        {
            table.Location = "dbo." + table.Name;
        }
    }

    private void SetReportParameters()
    {
        // Company info parameters
        ParameterDiscreteValue paramCompanyName = new ParameterDiscreteValue
        {
            Value = ConfigurationManager.AppSettings["CompanyName"]
        };
        ParameterDiscreteValue paramAddress1 = new ParameterDiscreteValue
        {
            Value = ConfigurationManager.AppSettings["Address1"]
        };
        ParameterDiscreteValue paramAddress2 = new ParameterDiscreteValue
        {
            Value = ConfigurationManager.AppSettings["Address2"]
        };
        ParameterDiscreteValue paramPostCodeCity = new ParameterDiscreteValue
        {
            Value = ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]
        };
        ParameterDiscreteValue paramState = new ParameterDiscreteValue
        {
            Value = ConfigurationManager.AppSettings["State"]
        };
        ParameterDiscreteValue paramRegistration = new ParameterDiscreteValue
        {
            Value = ConfigurationManager.AppSettings["Registration"]
        };
        ParameterDiscreteValue paramPhone = new ParameterDiscreteValue
        {
            Value = ConfigurationManager.AppSettings["Phone"]
        };

        // User info
        ParameterDiscreteValue paramUserName = new ParameterDiscreteValue
        {
            Value = Request.QueryString["LoginID"]
        };

        // Apply all parameters to the report viewer
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);

        CrystalReportViewerPaymentDueList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
        CrystalReportViewerPaymentDueList.ParameterFieldInfo["UserId"].CurrentValues.Add(paramUserName);
    }
}
