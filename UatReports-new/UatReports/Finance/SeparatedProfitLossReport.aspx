<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SeparatedProfitLossReport.aspx.cs" Inherits="Finance_SeparatedProfitLossReport" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Profit &amp; Loss (Separated)</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <CR:CrystalReportViewer ID="CrystalReportViewer" runat="server" AutoDataBind="True"
                Height="1039px" Width="1200px" ReportSourceID="CrystalReportSource" ToolPanelView="None" />
            <CR:CrystalReportSource ID="CrystalReportSource" runat="server">
                <Report FileName="SeparatedProfitLoss.rpt">
                    <DataSources>
                        <CR:DataSourceRef TableName="Command" />
                    </DataSources>
                </Report>
            </CR:CrystalReportSource>
        </div>
    </form>
</body>
</html>
