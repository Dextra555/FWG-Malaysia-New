<%@ page language="C#" autoeventwireup="true" inherits="PayRoll_NetPayReport, App_Web_402qaw3v" title="e-Security Management System" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>
    <style>
        #crptNetpayList {
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
            <cr:crystalreportviewer id="crptNetpayList" runat="server" autodatabind="True"
                height="1039px" width="901px" reportsourceid="crptNetpay" toolpanelview="None" />
            <cr:crystalreportsource id="crptNetpay" runat="server">
                <report filename="SalaryStatement.rpt">
                    <datasources>
                        <cr:datasourceref tablename="BranchMaster" />
                        <cr:datasourceref tablename="Employee" />
                        <cr:datasourceref tablename="EmployeeSalaryDetails" />
                        <cr:datasourceref tablename="EmploymentDetails" />
                        <cr:datasourceref tablename="Payslip" />
                    </datasources>
                </report>
            </cr:crystalreportsource>
        </div>
    </form>
</body>
</html>


