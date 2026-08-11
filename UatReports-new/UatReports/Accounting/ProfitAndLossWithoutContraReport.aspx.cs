using System;
using System.Configuration;
using System.IO;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;

public partial class Accounting_ProfitAndLossWithoutContraReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            string fromDateStr = Request.QueryString["FromDate"];
            string toDateStr   = Request.QueryString["ToDate"];
            string format      = (Request.QueryString["format"] ?? "pdf").ToLower();

            if (string.IsNullOrEmpty(fromDateStr) || string.IsNullOrEmpty(toDateStr))
            {
                Response.Write("<p style='color:red'>Error: FromDate and ToDate are required.</p>");
                return;
            }

            DateTime fromDate = DateTime.Parse(fromDateStr);
            DateTime toDate   = DateTime.Parse(toDateStr);

            // Load report
            ReportDocument rpt = new ReportDocument();
            rpt.Load(Server.MapPath("~/Accounting/ProfitAndLossWithoutContra.rpt"));

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
            rpt.SetParameterValue("FromDate",            fromDate);
            rpt.SetParameterValue("ToDate",              toDate);

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
