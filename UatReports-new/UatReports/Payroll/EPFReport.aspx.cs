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
            foreach (CrystalDecisions.CrystalReports.Engine.Table table in CrystalReportSourceData.ReportDocument.Database.Tables)
            {
                CrystalReportViewerView.ToolPanelView = ToolPanelViewType.None;
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName   = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password     = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID       = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);
            }

            // ── Company header parameters ──────────────────────────────────
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

            // ── Period ─────────────────────────────────────────────────────
            string period = Request.QueryString["Period"];
            DateTime periodDate;
            ParameterDiscreteValue paramPeriod = new ParameterDiscreteValue();
            if (DateTime.TryParse(period, out periodDate))
                paramPeriod.Value = periodDate;
            else
                paramPeriod.Value = DateTime.Now;

            // ── EmployeeType ───────────────────────────────────────────────
            string employeeTypeCheck = Request.QueryString["EmployeeType"] ?? "";
            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = employeeTypeCheck;

            // ── Pass all parameters to the viewer ──────────────────────────
            CrystalReportViewerView.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewerView.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewerView.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewerView.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            CrystalReportViewerView.ParameterFieldInfo["Period"].CurrentValues.Add(paramPeriod);
            CrystalReportViewerView.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);

            // ── Branch filter (appended to the .rpt's own formula) ─────────
            string branch = Request.QueryString["Branch"] ?? "";
            if (!string.IsNullOrEmpty(branch))
                CrystalReportSourceData.ReportDocument.RecordSelectionFormula += " AND {BranchMaster.Code} = '" + branch + "'";
            else
                CrystalReportSourceData.ReportDocument.RecordSelectionFormula += " AND {OBMSBranches.Name} = '" + Request.QueryString["LoginID"] + "'";

            // ── EPF No filter (ReportType 2 = has EPF, 3 = no EPF) ─────────
            switch (Request.QueryString["ReportType"])
            {
                case "3":
                    CrystalReportSourceData.ReportDocument.RecordSelectionFormula +=
                        " AND (ISNULL({EmployeeSalaryDetails.EMPFL_EPFNO})=true OR {EmployeeSalaryDetails.EMPFL_EPFNO}=\"\")";
                    break;
                case "2":
                    CrystalReportSourceData.ReportDocument.RecordSelectionFormula +=
                        " AND ({EmployeeSalaryDetails.EMPFL_EPFNO}<>\"\" AND ISNULL({EmployeeSalaryDetails.EMPFL_EPFNO})=false)";
                    break;
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

    private bool DoesParameterExist(string parameterName)
    {
        try
        {
            var paramField = CrystalReportViewerView.ParameterFieldInfo[parameterName];
            return paramField != null;
        }
        catch
        {
            return false;
        }
    }
}
