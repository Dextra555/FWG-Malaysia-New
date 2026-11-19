<%@ page language="C#" autoeventwireup="true" CodeFile="Payslip1Report.aspx.cs" Inherits="PayRoll_Payslip1Report" title="e-Security Management System" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Payslip 1 Report</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <CR:CrystalReportViewer ID="crptPaySlip1List" runat="server" AutoDataBind="True"
                Height="1039px" Width="901px" ReportSourceID="crptPaySlip1" ToolPanelView="None" />
            <CR:CrystalReportSource ID="crptPaySlip1" runat="server">
                <Report FileName="..\Payroll\PaySlip1.rpt">
                    <DataSources>
                        <CR:DataSourceRef TableName="BranchMaster" />
                        <CR:DataSourceRef TableName="Employee" />
                        <CR:DataSourceRef TableName="EmployeeSalaryDetails" />
                        <CR:DataSourceRef TableName="EmploymentDetails" />
                        <CR:DataSourceRef TableName="Payslip" />
                    </DataSources>
                </Report>
            </CR:CrystalReportSource>
        </div>
    </form>
</body>
</html>
