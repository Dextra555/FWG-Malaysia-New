using System;
using System.Configuration;
using CrystalDecisions.Shared;
using CrystalDecisions.Web;


public partial class Finance_ClientInvoiceReport : System.Web.UI.Page
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

            ParameterDiscreteValue paramUserName = new ParameterDiscreteValue();
            paramUserName.Value = Request.QueryString["LoginID"];

            ParameterDiscreteValue paramReportTitle = new ParameterDiscreteValue();

            CrystalReportViewer.ParameterFieldInfo["CompanyName"].CurrentValues.Add(paramCompanyName);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress1"].CurrentValues.Add(paramAddress1);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress2"].CurrentValues.Add(paramAddress2);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress3"].CurrentValues.Add(paramPostCodeCity);
            CrystalReportViewer.ParameterFieldInfo["CompanyAddress4"].CurrentValues.Add(paramState);
            CrystalReportViewer.ParameterFieldInfo["CompanyRegistration"].CurrentValues.Add(paramRegistration);
            CrystalReportViewer.ParameterFieldInfo["CompanyPhone"].CurrentValues.Add(paramPhone);
            //CrystalReportViewer.ParameterFieldInfo["LoginID"].CurrentValues.Add(paramUserName);

            //if (Request.QueryString["ID"].IndexOf(',') > 0)
            //{
            //    CrystalReportSource.ReportDocument.ParameterFields[0].CurrentValues.AddValue(1);
            //    CrystalReportSource.ReportDocument.RecordSelectionFormula = "";
            //    string[] sArrID = Request.QueryString["ID"].Split(',');
            //    for (int i = 0; i < sArrID.Length; i++)
            //    {
            //        if (i == 0)
            //            CrystalReportSource.ReportDocument.RecordSelectionFormula = " {ClientInvoice.ID} = " + sArrID[i];
            //        else
            //            CrystalReportSource.ReportDocument.RecordSelectionFormula += " OR {ClientInvoice.ID} = " + sArrID[i];
            //    }
            //}
            //else
            //    CrystalReportViewer.ParameterFieldInfo["InvoiceID"].CurrentValues.Add(int.Parse(Request.QueryString["ID"]));

            string[] sArrID = Request.QueryString["ID"].Split(',');
            for (int i = 0; i < sArrID.Length; i++)
            {
                if (i == 0)
                {
                    ParameterDiscreteValue invoiceID = new ParameterDiscreteValue();
                    invoiceID.Value = Convert.ToInt32(sArrID[i]);
                    CrystalReportViewer.ParameterFieldInfo["InvoiceID"].CurrentValues.Add(invoiceID);
                    CrystalReportSource.ReportDocument.RecordSelectionFormula = " {ClientInvoice.ID} = " + sArrID[i];
                }                    
                else
                {
                    CrystalReportSource.ReportDocument.RecordSelectionFormula += " OR {ClientInvoice.ID} = " + sArrID[i];
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
