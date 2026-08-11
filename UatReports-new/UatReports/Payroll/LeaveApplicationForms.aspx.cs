using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;

public partial class PayRoll_LeaveApplicationForms : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            // Optionally, ensure the Tool Panel is hidden
            crptLeaveApplicationFormList.ToolPanelView = ToolPanelViewType.None;
            //constant header fields
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

            crptLeaveApplicationFormList.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            crptLeaveApplicationFormList.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            crptLeaveApplicationFormList.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            crptLeaveApplicationFormList.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            crptLeaveApplicationFormList.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            crptLeaveApplicationFormList.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            crptLeaveApplicationFormList.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
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
