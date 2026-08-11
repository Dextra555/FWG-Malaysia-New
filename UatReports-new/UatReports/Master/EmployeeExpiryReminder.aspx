<%@ Page Language="C#" AutoEventWireup="true" CodeFile="EmployeeExpiryReminder.aspx.cs" Inherits="Master_EmployeeExpiryReminder" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Employee Visa & Passport Expiry Reminder</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
    <style>
        #CrystalReportViewerExpiryReminder {
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
            <CR:CrystalReportViewer ID="CrystalReportViewerExpiryReminder" runat="server" AutoDataBind="True"
                Height="1039px" Width="901px" ReportSourceID="CrystalReportSourceExpiryReminder" ToolPanelView="None" />
            <CR:CrystalReportSource ID="CrystalReportSourceExpiryReminder" runat="server">
                <Report FileName="EmployeeExpiryReminderReport.rpt">
                    <DataSources>
                        <CR:DataSourceRef TableName="VWEmployeeExpiryReminder" />
                    </DataSources>
                </Report>
            </CR:CrystalReportSource>
        </div>
    </form>
</body>
</html>
