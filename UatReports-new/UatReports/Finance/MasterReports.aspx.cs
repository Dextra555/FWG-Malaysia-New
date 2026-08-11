using System;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using OBMS.Application;

public partial class Finance_MasterReports : OBMSBasePage
{
    private CommonValidation oValidate = new CommonValidation();
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.ClientScript.IsClientScriptBlockRegistered("IframeSizing"))
        {
            StringBuilder sbScript = new StringBuilder();
            sbScript.AppendLine("<script type=\"text/javascript\">");
            sbScript.AppendLine("function setSize() ");
            sbScript.AppendLine("{");
            sbScript.Append("var iframeElement = document.getElementById('");
            sbScript.Append(MasterReportsFrame.ClientID);
            sbScript.AppendLine("'); ");
            switch (Request.QueryString["Report"])
            {
                case "3":
                case "5":
                case "6":
                case "11":
                    sbScript.AppendLine("iframeElement.style.height = document.body.clientHeight-224;");
                    break;
                case "4":
                case "9":
                    sbScript.AppendLine("iframeElement.style.height = document.body.clientHeight-254;");
                    break;
                default:
                    sbScript.AppendLine("iframeElement.style.height = document.body.clientHeight-170;");
                    break;
            }
            sbScript.AppendLine("}");
            sbScript.AppendLine("</script>");
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "IframeSizing", sbScript.ToString());
        }
        if (!IsPostBack)
        {
            string ReportPageName = "";
            ViewState.Add("ReportID", Request.QueryString["Report"]);
            switch (Request.QueryString["Report"])
            {
                case "0":
                    divBranchList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "None");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    ReportPageName = "ClientInvoiceReport.aspx?ID=" + Request.QueryString["ID"];
                    break;
                case "1":
                    divBranchList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "None");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    //ReportPageName = "BranchPaymentReport.aspx?ID=" + Request.QueryString["ID"] ;
                    //ReportPageName = "BranchPaymentReport.aspx?ID=" + Request.QueryString["ID"] + "&Category=" + Request.QueryString["Category"];
                    ReportPageName = "BranchPaymentReport.aspx?ID=" + Request.QueryString["ID"] + "&Category=" + Request.QueryString["Category"] + "&ASN=" + Request.QueryString["ASN"];
                    break;
                case "2":
                    divBranchList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "None");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    ReportPageName = "BranchReceiptReport.aspx?ID=" + Request.QueryString["ID"] + "&ASN=" + Request.QueryString["ASN"];
                    break;
                case "3":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }

                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "33":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }

                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "4":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.SelectedIndexChanged += new EventHandler(cboBranch_SelectedIndexChanged);
                    if (oValidate.IsAdmin())
                    {
                        cboClient.DataSource = ClientList.GetListWithBlankRow((string)cboBranch.SelectedValue);
                    }
                    else
                    {
                        cboClient.DataSource = ClientList.GetList((string)cboBranch.SelectedValue);
                    }
                    cboClient.DataBind();
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "");
                    divTransPeriod.Style.Add("Display", "");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "44":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.SelectedIndexChanged += new EventHandler(cboBranch_SelectedIndexChanged);
                    if (oValidate.IsAdmin())
                    {
                        cboClient.DataSource = ClientList.GetListWithBlankRow((string)cboBranch.SelectedValue);
                    }
                    else
                    {
                        cboClient.DataSource = ClientList.GetList((string)cboBranch.SelectedValue);
                    }
                    cboClient.DataBind();
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "");
                    divTransPeriod.Style.Add("Display", "");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "5":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "6":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    this.btnShow2.Visible = true;
                    break;
                case "7":
                    divBranchList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "None");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    ReportPageName = "ClientInvoiceAmountReport.aspx?ID=" + Request.QueryString["ID"];
                    break;
                case "8":
                    divBranchList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "None");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    ReportPageName = "ClientInvoiceDetailReport.aspx?ID=" + Request.QueryString["ID"];
                    break;
                case "14":
                    divBranchList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "None");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    ReportPageName = "TaxInvoiceReport.aspx?ID=" + Request.QueryString["ID"];
                    break;
                case "9":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    cboSupplier.DataSource = SupplierList.GetList(string.Empty);
                    cboSupplier.DataBind();
                    cboCategory.DataSource = InventoryCategory.GetList("U");
                    cboCategory.DataBind();
                    //cboPayTo.DataSource = ReceipientList.GetList(cboCategory.SelectedValue);
                    cboPayTo.DataSource = SupplierList.GetList("U");
                    cboPayTo.DataBind();
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "");
                    divTransPeriod.Style.Add("Display", "");
                    divCategory.Style.Add("Display", "");
                    divPayToList.Style.Add("Display", "");
                    break;
                case "49":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    cboSupplier.DataSource = SupplierList.GetList(string.Empty);
                    cboSupplier.DataBind();
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "");
                    divTransPeriod.Style.Add("Display", "");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "10":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "11":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                case "12":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    this.btnShowTotal.Visible = true;
                    this.btnShowTotalCurrentYear.Visible = true;
                    break;
                case "13":
                    if (oValidate.IsAdmin())
                    {
                        this.cboBranch.DataSource = BranchList.GetListWithBlankRow(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    else
                    {
                        this.cboBranch.DataSource = BranchList.GetList(((FormsIdentity)Page.User.Identity).Name.Split(new char[] { '|' })[0].ToString());
                    }
                    this.cboBranch.DataBind();
                    this.cboBranch.AutoPostBack = false;
                    divBranchList.Style.Add("Display", "");
                    divClientList.Style.Add("Display", "None");
                    divSupplierList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;
                default:
                    divBranchList.Style.Add("Display", "None");
                    divTransPeriod.Style.Add("Display", "None");
                    divCategory.Style.Add("Display", "None");
                    divPayToList.Style.Add("Display", "None");
                    break;

            }
            DataBind();
            if (!string.IsNullOrEmpty(ReportPageName))
                this.MasterReportsFrame.Attributes["src"] = ReportPageName;
        }
    }
    void Page_Init(Object sender, EventArgs e)
    {
        switch (Request.QueryString["Report"])
        {
            case "7":
            case "8":
            case "14":
            case "0":
                this.MenuName = "Invoice";
                this.NavigationTitle = "Finance - Reports - Invoice";
                break;
            case "1":
                this.MenuName = "Payments";
                this.NavigationTitle = "Finance - Reports - Payments";
                break;
            case "2":
                this.MenuName = "Receipts";
                this.NavigationTitle = "Finance - Reports - Receipts";
                break;
            case "3":
                this.MenuName = "Branch Transactions Report";
                this.NavigationTitle = "Finance - Reports - Branch Transactions";
                break;
            case "33":
                this.MenuName = "Branch Transactions Report (Manager)";
                this.NavigationTitle = "Finance - Reports - Branch Transactions (Manager)";
                break;
            case "4":
                this.MenuName = "Client Statement Report";
                this.NavigationTitle = "Finance - Reports - Client Statement";
                break;
            case "44":
                this.MenuName = "Client Statement Report";
                this.NavigationTitle = "Finance - Reports - Client Statement with b/f";
                break;
            case "5":
                this.MenuName = "Branch Collections Report";
                this.NavigationTitle = "Finance - Reports - Branch Collections";
                break;
            case "6":
                this.MenuName = "Invoice Ageing Report";
                this.NavigationTitle = "Finance - Reports - Invoice Ageing";
                break;
            case "9":
                this.MenuName = "Supplier Statement Report";
                this.NavigationTitle = "Finance - Reports - Payments";
                break;
            case "49":
                this.MenuName = "Supplier Statement Report";
                this.NavigationTitle = "Finance - Reports - Payments with b/f";
                break;
            case "10":
                this.MenuName = "Credit Note Summary Report";
                this.NavigationTitle = "Finance - Reports - Credit Note Summary";
                break;
            case "11":
                this.MenuName = "Deleted Invoice Report";
                this.NavigationTitle = "Finance - Reports - Deleted Invoice Details";
                break;
            case "12":
                this.MenuName = "Invoice Collection Status Report";
                this.NavigationTitle = "Finance - Reports - Invoice Collection Status";
                break;
            case "13":
                this.MenuName = "Monthly Invoice Status Report";
                this.NavigationTitle = "Finance - Reports - Monthly Invoice Status";
                break;
        }

    }
    protected void ShowMessage(string Message)
    {
        ((Finance_MasterData)this.Page.Master).ShowMessage(Message);
    }
    protected void btnShow_Click(object sender, EventArgs e)
    {

        //if (cboBranch.SelectedIndex == 0)
        //{
        //    ShowMessage("Please select Branch!");
        //    cboBranch.Focus();
        //    return;
        //}
        if ((string)ViewState["ReportID"] != "9")
        {
            if ((string)ViewState["ReportID"] != "10")
            {
                if (oValidate.IsAdmin())
                {
                    if (cboBranch.SelectedIndex == 0)
                    {
                        ShowMessage("Please select branch!");
                        cboBranch.Focus();
                        return;
                    }
                }
                else
                {
                    if (cboBranch.SelectedIndex == -1)
                    {
                        ShowMessage("Please select branch!");
                        cboBranch.Focus();
                        return;
                    }
                }
            }
        }

        if ((string)ViewState["ReportID"] == "9")
        {
            if (oValidate.IsAdmin())
            {
                if (cboBranch.SelectedIndex == 0)
                {
                    ShowMessage("Please select branch!");
                    cboBranch.Focus();
                    return;
                }

                if (cboSupplier.SelectedIndex == 0)
                {
                    ShowMessage("Please select Supplier!");
                    cboSupplier.Focus();
                    return;
                }

                //if (cboCategory.SelectedIndex == 0)
                //{
                //    ShowMessage("Please select Category!");
                //    cboCategory.Focus();
                //    return;
                //}

                //if (cboPayTo.SelectedIndex == 0)
                //{
                //    ShowMessage("Please select PayTo!");
                //    cboPayTo.Focus();
                //    return;
                //}
            }
            else
            {
                if (cboBranch.SelectedIndex == -1)
                {
                    ShowMessage("Please select branch!");
                    cboBranch.Focus();
                    return;
                }

                if (cboSupplier.SelectedIndex == -1)
                {
                    ShowMessage("Please select Supplier!");
                    cboSupplier.Focus();
                    return;
                }

                //if (cboCategory.SelectedIndex == -1)
                //{
                //    ShowMessage("Please select Category!");
                //    cboCategory.Focus();
                //    return;
                //}

                //if (cboPayTo.SelectedIndex == -1)
                //{
                //    ShowMessage("Please select PayTo!");
                //    cboPayTo.Focus();
                //    return;
                //}
            }
        }

        if (txtStartDate.Text == "")
        {
            ShowMessage("Please select Start Date!");
            txtStartDate.Focus();
            return;
        }
        if (txtEndDate.Text == "")
        {
            ShowMessage("Please select End Date!");
            txtEndDate.Focus();
            return;
        }
        ShowMessage("");
        string connString = ConfigurationManager.ConnectionStrings["obms"].ConnectionString;
        SqlConnection myConn = new SqlConnection(connString);
        myConn.Open();
        string ssql = "";
        SqlCommand myCmd;
        switch ((string)ViewState["ReportID"])
        {
            case "3":
                this.MasterReportsFrame.Attributes["src"] = "BranchTransactionReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "33":
                this.MasterReportsFrame.Attributes["src"] = "BranchTransactionMgrReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "44":
                //need to execute the stored procedure with the said parameters
                ssql = "exec dbo.GetClientStatement '" + txtStartDate.Text + "','" + txtEndDate.Text + "','";
                ssql = ssql + this.cboBranch.SelectedValue + "','" + this.cboClient.SelectedValue + "'";
                myCmd = new SqlCommand(ssql);
                myCmd.Connection = myConn;
                myCmd.ExecuteNonQuery();
                this.MasterReportsFrame.Attributes["src"] = "ClientStatement2Report.aspx?Branch=" + this.cboBranch.SelectedValue + "&Client=" + this.cboClient.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "4":
                ssql = "exec dbo.GetClientStatement '" + txtStartDate.Text + "','" + txtEndDate.Text + "','";
                ssql = ssql + this.cboBranch.SelectedValue + "','" + this.cboClient.SelectedValue + "'";
                myCmd = new SqlCommand(ssql);
                myCmd.Connection = myConn;
                myCmd.ExecuteNonQuery();
                this.MasterReportsFrame.Attributes["src"] = "ClientStatementReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&Client=" + this.cboClient.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "5":
                this.MasterReportsFrame.Attributes["src"] = "BranchCollectionReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "6":
                this.MasterReportsFrame.Attributes["src"] = "InvoiceAgeingReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "9":
                if (cboPayTo.SelectedIndex == 0)//cboSupplier
                {
                    ssql = "exec dbo.BfSupplierStatement '" + txtStartDate.Text + "','" + txtEndDate.Text + "','";
                    ssql = ssql + this.cboBranch.SelectedValue + "','" + this.cboSupplier.SelectedValue + "','" + this.rdStatus.SelectedValue + "'";
                }
                else
                {
                    ssql = "exec dbo.BfSupplierStatement2 '" + txtStartDate.Text + "','" + txtEndDate.Text + "','";
                    ssql = ssql + this.cboBranch.SelectedValue + "','" + this.cboPayTo.SelectedValue + "','";
                    ssql = ssql + this.cboCategory.SelectedValue + "'"; //,'" + this.rdStatus.SelectedValue + "'"
                }
                //ssql = "exec dbo.BfSupplierStatement '" + txtStartDate.Text + "','" + txtEndDate.Text + "','";
                //ssql = ssql + this.cboBranch.SelectedValue + "','" + this.cboSupplier.SelectedValue + "','";
                //ssql = ssql + this.cboCategory.SelectedValue + "'";
                myCmd = new SqlCommand(ssql);
                myCmd.Connection = myConn;
                myCmd.ExecuteNonQuery();

                if (cboPayTo.SelectedIndex == 0)//cboSupplier
                {
                    this.MasterReportsFrame.Attributes["src"] = "SupplierStatementReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&Supplier=" + cboSupplier.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text + "&Category=" + cboCategory.SelectedValue;
                }
                else
                {
                    this.MasterReportsFrame.Attributes["src"] = "SupplierStatementReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&Supplier=" + cboPayTo.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text + "&Category=" + cboCategory.SelectedValue;
                }

                break;
            /*           case "49":
                            //need to execute the stored procedure with the said parameters
                            ssql = "exec dbo.BfSupplierStatement '" + txtStartDate.Text + "','" + txtEndDate.Text + "','";
                            ssql = ssql + this.cboBranch.SelectedValue + "','" + this.cboSupplier.SelectedValue + "'";
                            myCmd = new SqlCommand(ssql);
                            myCmd.Connection = myConn;
                            myCmd.ExecuteNonQuery();
                            this.MasterReportsFrame.Attributes["src"] = "SupplierStatement2Report.aspx?Branch=" + this.cboBranch.SelectedValue + "&Supplier=" + cboSupplier.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                            break;
             */
            case "10":
                this.MasterReportsFrame.Attributes["src"] = "CreditNoteReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "11":
                this.MasterReportsFrame.Attributes["src"] = "DeletedInvoiceReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "12":
                this.MasterReportsFrame.Attributes["src"] = "InvoiceCollectionReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            case "13":
                this.MasterReportsFrame.Attributes["src"] = "MonthlyInvoiceReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            default:
                break;
        }
        if (myConn != null)
            myConn.Close();
    }
    protected void cboBranch_SelectedIndexChanged(object sender, EventArgs e)
    {
        cboClient.DataSource = ClientList.GetListWithBlankRow((string)cboBranch.SelectedValue);
        cboClient.DataBind();
    }
    protected void btnShowTotal_Click(object sender, EventArgs e)
    {
        if (txtStartDate.Text == "")
        {
            ShowMessage("Please select Start Date!");
            return;
        }
        if (txtEndDate.Text == "")
        {
            ShowMessage("Please select End Date!");
            return;
        }
        ShowMessage("");
        this.MasterReportsFrame.Attributes["src"] = "InvoiceCollectionTotalReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
    }
    protected void cboCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        //cboPayTo.DataSource = ReceipientList.GetList(cboCategory.SelectedValue);
        //cboPayTo.DataBind();
    }
    protected void cboSupplier_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            cboCategory.DataSource = InventoryCategory.GetListBySupplier(cboSupplier.SelectedValue);
            cboCategory.DataBind();
            cboPayTo.DataSource = SupplierList.GetListBySupplier(cboSupplier.SelectedValue);
            cboPayTo.DataBind();
        }
        catch (ArgumentNullException ex)
        {
            ShowMessage("Data Cannot be null." + ex.Message);
        }
        catch (Exception ex)
        {
            ShowMessage("Error Found." + ex.Message);
        }
    }
    protected void btnShowTotalCurrentYear_Click(object sender, EventArgs e)
    {
        if (txtStartDate.Text == "")
        {
            ShowMessage("Please select Start Date!");
            return;
        }
        if (txtEndDate.Text == "")
        {
            ShowMessage("Please select End Date!");
            return;
        }
        ShowMessage("");
        this.MasterReportsFrame.Attributes["src"] = "InvoiceCollectionTotalCurrentPeriodReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
    }
    protected void btnShow2_Click(object sender, EventArgs e)
    {
        if (txtStartDate.Text == "")
        {
            ShowMessage("Please select Start Date!");
            txtStartDate.Focus();
            return;
        }
        if (txtEndDate.Text == "")
        {
            ShowMessage("Please select End Date!");
            txtEndDate.Focus();
            return;
        }
        ShowMessage("");
        
        switch ((string)ViewState["ReportID"])
        {
            case "6":
                this.MasterReportsFrame.Attributes["src"] = "InvoiceAgeingAllBranchesReport.aspx?Branch=" + this.cboBranch.SelectedValue + "&StartDate=" + txtStartDate.Text + "&EndDate=" + txtEndDate.Text;
                break;
            default:
                break;
        }
    }
}
