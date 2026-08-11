<%@ page language="C#" autoeventwireup="true" inherits="EmployeeResignedList, App_Web_luwh1flm" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
    <style>
        #CrystalReportViewerEmployeeResignedList {
            width: 100%;
            height: 100vh;
            box-sizing: border-box;
        }

        body {
            margin: 0;
            padding: 0;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <CR:CrystalReportViewer ID="CrystalReportViewerEmployeeResignedList" runat="server" AutoDataBind="True"
                Height="1039px" Width="901px" ReportSourceID="CrystalReportSourceEmployeeResignedList" ToolPanelView="None" />
            <CR:CrystalReportSource ID="CrystalReportSourceEmployeeResignedList" runat="server">
                <Report FileName="ResignedEmployeeReport.rpt">
                    <DataSources>
                        <CR:DataSourceRef TableName="SalaryAdvance" />
                        <CR:DataSourceRef TableName="Employee" />
                    </DataSources>
                </Report>
            </CR:CrystalReportSource>

        </div>
    </form>
</body>
</html>
