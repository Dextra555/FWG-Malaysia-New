<%@ page language="C#" autoeventwireup="true" inherits="PayRoll_MonthlyAdvanceReport, App_Web_402qaw3v" title="e-Security Management System" %>

<%@ Register TagPrefix="CR" Namespace="CrystalDecisions.Web" Assembly="CrystalDecisions.Web, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>   
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <cr:crystalreportviewer id="crptBankAdvanceList" runat="server" autodatabind="True"
                height="100px" width="901px" reportsourceid="crptBankAdvance" toolpanelview="None" />
            <cr:crystalreportsource id="crptBankAdvance" runat="server">
                <report filename="..\Payroll\MonthlyAdvanceReport.rpt">
                    <datasources>
                        <cr:datasourceref tablename="SalaryAdvance" />
                        <cr:datasourceref tablename="Employee" />
                    </datasources>
                </report>
            </cr:crystalreportsource>
        </div>
    </form>
</body>
</html>

