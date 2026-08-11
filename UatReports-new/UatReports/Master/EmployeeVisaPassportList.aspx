<%@ Page Language="C#" AutoEventWireup="true" CodeFile="EmployeeVisaPassportList.aspx.cs" Inherits="Master_EmployeeVisaPassportList" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Employee Visa &amp; Passport List</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
    <style>
        #CrystalReportViewerVisaPassportList {
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
            <CR:CrystalReportViewer ID="CrystalReportViewerVisaPassportList" runat="server" AutoDataBind="True"
                Height="1039px" Width="901px" ReportSourceID="CrystalReportSourceVisaPassportList" ToolPanelView="None" />
            <CR:CrystalReportSource ID="CrystalReportSourceVisaPassportList" runat="server">
                <Report FileName="EmployeeVisaPassportListReport.rpt">
                    <DataSources>
                        <CR:DataSourceRef TableName="VWEmployeeVisaPassportList" />
                    </DataSources>
                </Report>
            </CR:CrystalReportSource>
        </div>
    </form>
</body>
</html>
