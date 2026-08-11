<%@ page language="C#" autoeventwireup="true" inherits="Finance_SupplierStatementReport, App_Web_zojcqgwf" %>
<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <CR:CrystalReportViewer ID="CrystalReportViewer" runat="server" AutoDataBind="True"
                Height="1039px" Width="901px" ReportSourceID="CrystalReportSource" />
            <CR:CrystalReportSource ID="CrystalReportSource" runat="server">
                <Report FileName="SupplierStatement.rpt">
                    <DataSources>
                        <CR:DataSourceRef TableName="Receipts" />
                        <CR:DataSourceRef TableName="BankMaster" />
                    </DataSources>
                </Report>
            </CR:CrystalReportSource>

        </div>
    </form>
</body>
</html>
