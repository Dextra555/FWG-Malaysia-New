<%@ page language="C#" autoeventwireup="true" inherits="Master_ClientMasterReport, App_Web_vhuikgtt" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>    
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <CR:CrystalReportViewer ID="CrystalReportViewerClientList" runat="server" AutoDataBind="True"
                Height="1039px" Width="901px" ReportSourceID="CrystalReportSourceClientList" ToolPanelView="None" />
            <CR:CrystalReportSource ID="CrystalReportSourceClientList" runat="server">
                <Report FileName="ClientReport.rpt">
                    <DataSources>
                        <CR:DataSourceRef TableName="ClientMaster" />
                    </DataSources>
                </Report>
            </CR:CrystalReportSource>

        </div>
    </form>
</body>
</html>
