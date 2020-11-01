<%@ Page Title="Order Summary" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false"
    CodeFile="OrderSummary.aspx.vb" Inherits="OrderSummary" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="Server">
    <div id="mainDiv">
        <div class="searchDiv">
            <div id="searchCustomersDiv" style="width: 65%;">
                <fieldset>
                    <legend>Search</legend><span>
                        <label>
                            Carrier:</label>
                        <asp:DropDownList ID="ddlCarrier" autopostback="true" runat="server">                        
                            <%--<asp:ListItem>All Talk</asp:ListItem>--%>
                            <asp:ListItem>Sprint</asp:ListItem>
                            <asp:ListItem>Verizon</asp:ListItem>
                            <asp:ListItem>Concord</asp:ListItem>
                            <asp:ListItem>Telco</asp:ListItem>
                            <asp:ListItem>VZPP</asp:ListItem>
                            <asp:ListItem>Page Plus</asp:ListItem>
                        </asp:DropDownList></span><span>
                        <label>
                            Cell:</label>
                        <span class="globalSearchFld">
                            <asp:TextBox ID="searchCusByCell" runat="server" ClientIDMode="Static"></asp:TextBox>
                            <img alt="" src="images/delete.png" title="Clear global search." />
                        </span></span><span>
                            <label>
                                Name:</label>
                            <asp:TextBox ID="searchCusByName" runat="server"></asp:TextBox>
                        </span><span>
                            <asp:Button ID="searchCusBtn" runat="server" Text="Go" />
                        </span>
                </fieldset>
            </div>
            <div id="totalCustomersDiv" runat="server" style="width: 30%; float: right;">
                <span style="float: right;">
                    <label>
                    </label>
                    <label ID="totalCustomerLbl" runat="server" ClientIDMode="Static" style="align-content:right"></label>
                   <%-- <asp:Label ID="totalCustomerLbl" runat="server" ClientIDMode="Static" style="align-content:right"></asp:Label>--%>
                </span>
            </div>
        </div>
        <div id="ordersSummmaryGridviewDiv" style="height: 90%; width: 100%; overflow: auto;">
            <asp:GridView ID="ordersSummmaryGridview" runat="server" AutoGenerateColumns="true"
                AllowSorting="true" RowStyle-CssClass="highlightedGvRow" AllowPaging="true" PageSize="25"
                Width="100%" DataSourceID="ordersSummmaryGVDataSource" Font-Size="11px">
            </asp:GridView>
        </div>
        <asp:SqlDataSource ID="ordersSummmaryGVDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>">
            <SelectParameters>
                <asp:ControlParameter ControlID="searchCusByCell" Name="CellNum" DefaultValue="%"
                    Type="String" />
                <asp:ControlParameter ControlID="searchCusByName" Name="CusName" DefaultValue="%"
                    Type="String" />
            </SelectParameters>
        </asp:SqlDataSource>
        <div id="downloadBtnDiv" class="downloadBtnDiv" runat="server">
            <input id="downloadCustomersBtn" runat="server" type="button" value="Download" />
        </div>
    </div>
    <script type="text/javascript" src="Scripts/GlobalCellSearch.js"></script>
    <script type="text/javascript">

        var gcsManager = new atcGlobalCellSearch('globalSearchFld', 'searchCusBtn', '<%= searchCusByCell.ClientID %>', '<%= ordersSummmaryGridview.ClientID %>', '<%= _gcsManager.IsGSOn %>');

        window.onload = function () {

            if (document.getElementById('totalCustomerLbl')) {
                setInterval(function () {
                    UseCallBack("refreshCustomerTotal:", "refreshCustomerTotal");
                }, (60 * 1000));
            }

        }

    </script>
</asp:Content>
