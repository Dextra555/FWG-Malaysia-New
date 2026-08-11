<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SalesInvoiceCollectionSummaryReport.aspx.cs" Inherits="Finance_SalesInvoiceCollectionSummaryReport" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sales Invoice Collection Summary Report</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <CR:CrystalReportViewer ID="CrystalReportViewer" runat="server" AutoDataBind="True"
                Height="1039px" Width="1400px" ToolPanelView="None" />
        </div>
    </form>
</body>
</html>
