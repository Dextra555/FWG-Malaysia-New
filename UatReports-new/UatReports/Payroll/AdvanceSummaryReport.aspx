<%@ Page Language="C#" AutoEventWireup="true" CodeFile="AdvanceSummaryReport.aspx.cs" Inherits="Payroll_AdvanceSummaryReport" %>
<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <CR:CrystalReportViewer ID="CrystalReportViewerView" runat="server" AutoDataBind="True"
                Height="1039px" Width="901px" ReportSourceID="CrystalReportSourceData" ToolPanelView="None"/>
            <CR:CrystalReportSource ID="CrystalReportSourceData" runat="server">
                <Report FileName="AdvanceSummary.rpt">                    
                </Report>
            </CR:CrystalReportSource>

        </div>
    </form>
</body>
</html>




