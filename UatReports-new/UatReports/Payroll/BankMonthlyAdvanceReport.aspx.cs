using CrystalDecisions.Shared;
using CrystalDecisions.Web;
using System;
using System.Configuration;

public partial class Payroll_BankMonthlyAdvanceReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {

            foreach (CrystalDecisions.CrystalReports.Engine.Table table in crptBankAdvance.ReportDocument.Database.Tables)
            {
                // Optionally, ensure the Tool Panel is hidden
                crptBankAdvanceList.ToolPanelView = ToolPanelViewType.None;
                crptBankAdvanceList.Zoom(100);
                TableLogOnInfo logonInfo = table.LogOnInfo;               
                logonInfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings["Server"];
                logonInfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings["Database"];
                logonInfo.ConnectionInfo.Password = ConfigurationManager.AppSettings["Password"];
                logonInfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings["UserID"];
                table.ApplyLogOnInfo(logonInfo);

                //testing purpose added here
                //Response.Write("Table: " + table.Name + "<br>");
                //Response.Write("Server: " + logonInfo.ConnectionInfo.ServerName + "<br>");
                //Response.Write("Database: " + logonInfo.ConnectionInfo.DatabaseName + "<br>");
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

            //parameter fields

            ParameterDiscreteValue paramBranch = new ParameterDiscreteValue();
            paramBranch.Value = Request.QueryString["Branch"];

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            ParameterDiscreteValue paramEmployeeType = new ParameterDiscreteValue();
            paramEmployeeType.Value = Request.QueryString["EmployeeType"];

            ParameterDiscreteValue paramBank = new ParameterDiscreteValue();
            paramBank.Value = Request.QueryString["Bank"];

            string period = Request.QueryString["AdvancePeriod"];
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


            crptBankAdvanceList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptBankAdvanceList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptBankAdvanceList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptBankAdvanceList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            crptBankAdvanceList.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);
            crptBankAdvanceList.ParameterFieldInfo["AdvancePeriod"].CurrentValues.Add(paramPeriod);
            crptBankAdvanceList.ParameterFieldInfo["EmployeeType"].CurrentValues.Add(paramEmployeeType);

            //if (!string.IsNullOrEmpty(this.Page.Request.QueryString["Branch"]))
            //{
            //    // Use the provided Branch parameter
            //    crptBankAdvanceList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramBranch);
            //}
            //else
            //{
            //    // Create a new ParameterDiscreteValue for "ALL BRANCHES"
            //    ParameterDiscreteValue paramAllBranches = new ParameterDiscreteValue();
            //    paramAllBranches.Value = "ALL BRANCHES";
            //    crptBankAdvanceList.ParameterFieldInfo["Branch"].CurrentValues.Add(paramAllBranches);
            //}
            //if (!string.IsNullOrEmpty(this.Page.Request.QueryString["Bank"]))
            //{
            //    // Add the value from the QueryString
            //    crptBankAdvanceList.ParameterFieldInfo["Bank"].CurrentValues.Add(paramBank);
            //}
            //else
            //{
            //    // Create a new ParameterDiscreteValue for "ALL BANK"
            //    ParameterDiscreteValue paramAllBanks = new ParameterDiscreteValue();
            //    paramAllBanks.Value = "ALL BANK";
            //    crptBankAdvanceList.ParameterFieldInfo["Bank"].CurrentValues.Add(paramAllBanks);
            //}

            //if (this.Page.Request.QueryString["Branch"] != "")
            //    crptBankAdvance.ReportDocument.RecordSelectionFormula += " AND {Employee.EMP_BRANCH_CODE}= '" + Request.QueryString["Branch"] + "'";


            //if (!string.IsNullOrEmpty(this.Page.Request.QueryString["Bank"]))
            //    crptBankAdvance.ReportDocument.RecordSelectionFormula += " AND {EmployeeSalaryDetails.EMPFL_BANK}= '" + Request.QueryString["Bank"] + "'";

            // ---------- BRANCH ----------
            // ======================= BRANCH ==========================
            string branchValue = this.Page.Request.QueryString["Branch"];

            if (!string.IsNullOrWhiteSpace(branchValue))
            {
                crptBankAdvanceList.ParameterFieldInfo["Branch"]
                    .CurrentValues.Add(paramBranch);

                // Record selection for Crystal
                crptBankAdvance.ReportDocument.RecordSelectionFormula +=
                    " AND {Employee.EMP_BRANCH_CODE} = '" + branchValue + "'";
            }
            else
            {
                // Default ALL BRANCHES
                ParameterDiscreteValue paramAllBranches = new ParameterDiscreteValue();
                paramAllBranches.Value = "ALL BRANCHES";

                crptBankAdvanceList.ParameterFieldInfo["Branch"]
                    .CurrentValues.Add(paramAllBranches);
            }


            // ======================= BANK ==========================
            string bankValue = this.Page.Request.QueryString["Bank"];

            if (!string.IsNullOrWhiteSpace(bankValue))
            {

                crptBankAdvanceList.ParameterFieldInfo["Bank"]
                    .CurrentValues.Add(paramBank);

                // Record selection for Crystal
                crptBankAdvance.ReportDocument.RecordSelectionFormula +=
                    " AND {EmployeeSalaryDetails.EMPFL_BANK} = '" + bankValue + "'";
            }
            else
            {
                // Default ALL BANK
                ParameterDiscreteValue paramAllBanks = new ParameterDiscreteValue();
                paramAllBanks.Value = "ALL BANK";

                crptBankAdvanceList.ParameterFieldInfo["Bank"]
                    .CurrentValues.Add(paramAllBanks);
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
