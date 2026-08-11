using System;
using System.Configuration;
using System.IO;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;

public partial class Accounting_ProfitAndLostReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            string startDateStr = Request.QueryString["StartDate"];
            string endDateStr   = Request.QueryString["EndDate"];
            string branch       = Request.QueryString["Branch"] ?? "";
            string format       = (Request.QueryString["format"] ?? "pdf").ToLower();

            if (string.IsNullOrEmpty(startDateStr) || string.IsNullOrEmpty(endDateStr))
            {
                Response.Write("<p style='color:red'>Error: StartDate and EndDate are required.</p>");
                return;
            }

            DateTime startDate;
            DateTime endDate;
            if (!DateTime.TryParse(startDateStr, out startDate)) startDate = DateTime.Now;
            if (!DateTime.TryParse(endDateStr,   out endDate))   endDate   = DateTime.Now;

            // Load report
            ReportDocument rpt = new ReportDocument();
            rpt.Load(Server.MapPath("~/Accounting/ProfitAndLost.rpt"));

            // Apply DB login
            ApplyLogin(rpt);

            // Set parameters
            rpt.SetParameterValue("CompanyName",         ConfigurationManager.AppSettings["CompanyName"]);
            rpt.SetParameterValue("CompanyAddress1",     ConfigurationManager.AppSettings["Address1"]);
            rpt.SetParameterValue("CompanyAddress2",     ConfigurationManager.AppSettings["Address2"]);
            rpt.SetParameterValue("CompanyAddress3",     ConfigurationManager.AppSettings["PostCode"] + " " + ConfigurationManager.AppSettings["City"]);
            rpt.SetParameterValue("CompanyAddress4",     ConfigurationManager.AppSettings["State"]);
            rpt.SetParameterValue("CompanyRegistration", ConfigurationManager.AppSettings["Registration"]);
            rpt.SetParameterValue("CompanyPhone",        ConfigurationManager.AppSettings["Phone"]);
            rpt.SetParameterValue("StartDate",           startDate);
            rpt.SetParameterValue("EndDate",             endDate);
            rpt.SetParameterValue("Branch",              branch);

            Stream exportStream;
            string contentType;
            string fileName;

            if (format == "excel")
            {
                exportStream = rpt.ExportToStream(ExportFormatType.Excel);
                contentType  = "application/vnd.ms-excel";
                fileName     = "ProfitAndLoss.xls";
            }
            else
            {
                exportStream = rpt.ExportToStream(ExportFormatType.PortableDocFormat);
                contentType  = "application/pdf";
                fileName     = "ProfitAndLoss.pdf";
            }

            rpt.Close();
            rpt.Dispose();

            Response.Buffer = false;
            Response.ClearHeaders();
            Response.ContentType = contentType;
            Response.AddHeader("Content-Disposition",
                (format == "excel" ? "attachment" : "inline") + "; filename=" + fileName);

            byte[] buffer = new byte[32768];
            int bytesRead;
            while ((bytesRead = exportStream.Read(buffer, 0, buffer.Length)) > 0)
                Response.OutputStream.Write(buffer, 0, bytesRead);

            exportStream.Close();
            Response.Flush();
            Response.End();
        }
        catch (Exception ex)
        {
            Response.Write("<p style='color:red'>Error: " + Server.HtmlEncode(ex.Message) + "</p>");
        }
    }

    private void ApplyLogin(ReportDocument rpt)
    {
        ConnectionInfo connInfo = new ConnectionInfo
        {
            ServerName         = ConfigurationManager.AppSettings["Server"],
            DatabaseName       = ConfigurationManager.AppSettings["Database"],
            UserID             = ConfigurationManager.AppSettings["UserID"],
            Password           = ConfigurationManager.AppSettings["Password"],
            IntegratedSecurity = false
        };

        foreach (Table table in rpt.Database.Tables)
        {
            TableLogOnInfo tli = table.LogOnInfo;
            tli.ConnectionInfo = connInfo;
            table.ApplyLogOnInfo(tli);
        }

        foreach (ReportDocument sub in rpt.Subreports)
        {
            foreach (Table table in sub.Database.Tables)
            {
                TableLogOnInfo tli = table.LogOnInfo;
                tli.ConnectionInfo = connInfo;
                table.ApplyLogOnInfo(tli);
            }
        }
    }
}
