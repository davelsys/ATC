<%@ Page Title="Search Orders" MasterPageFile="~/Site.master" Language="VB" AutoEventWireup="false" CodeFile="SearchOrder.aspx.vb" Inherits="SearchOrder" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">

    <style type="text/css">
        
        .completionListElement {
            margin : 0px!important ;
            color : windowtext ;
            background-color : window ;
            border: 1px solid buttonshadow;
            overflow : auto ;
            height : 200px ;
            cursor: default;
            font-size : small ;
            text-align : left ;
            list-style-type : none ;
            padding: 0px;
        }
        
        .completionListItem {
            background-color : window ;
            color : windowtext ;
            padding : 1px ;
        }
        
        .highlightedListItem {
            background-color : whitesmoke ;
            color : black ;
            padding: 1px ;
        }
        
        #orderVendorDiv {
            height: 30px;
        }
        
        #orderVendorDiv table {
            float: right;
        }
        
    </style>

    
    <script type="text/javascript">

        window.onload = function () {

            // Set auto search when selecting auto complete item.
            // Without timeout, we can't pick up the object.
            setTimeout( function () {
                var compBhvr = $find( 'autoCompBhvr' );
                compBhvr.add_itemSelected( function () {
                    __doPostBack(); // Search orders
                } );
            }, 0 );

        }
        
    </script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div style="height: 20px;">
        <asp:LinkButton ID="add_order_btn" runat="server" Text="New Order" style="float: right; cursor: pointer;"/>
        <h2>Search Orders</h2>
    </div>

    <div id="orderVendorDiv">
        <asp:RadioButtonList ID="vendorSelectRadios" runat="server"
                                RepeatDirection="Horizontal">
            <asp:ListItem Selected="True">All</asp:ListItem>
            <asp:ListItem>PP</asp:ListItem>
            <asp:ListItem>Ver</asp:ListItem>
            <asp:ListItem>Concord</asp:ListItem>
	        <asp:ListItem>Sprint</asp:ListItem>
            <asp:ListItem>Telco</asp:ListItem>
            <asp:ListItem>VZPP</asp:ListItem>
            <%--<asp:ListItem Value="AT" Enabled="false">AT</asp:ListItem>--%>
           
        </asp:RadioButtonList>
    </div>

    <div>
        <label>Cell Number: </label>
        <asp:TextBox ID="cellNumberInput" runat="server"></asp:TextBox>

        <span>&nbsp;</span>

        <label>Last Name: </label>
        <asp:AutoCompleteExtender ID="lastNameAutoCompleter" runat="server" MinimumPrefixLength="1"
            EnableCaching="true" CompletionSetCount="20" TargetControlID="lnameInput"
            CompletionListCssClass="completionListElement"
            CompletionListItemCssClass="completionListItem"
            CompletionListHighlightedItemCssClass="highlightedListItem"
            BehaviorID="autoCompBhvr"
            CompletionInterval="150" ServiceMethod="LastNamesCompletion">
        </asp:AutoCompleteExtender>
        <asp:TextBox ID="lnameInput" runat="server"></asp:TextBox>

        <span>&nbsp;</span>

        <label>Serial Number: </label>
        <asp:TextBox ID="searchSerialNumFld" runat="server"></asp:TextBox>

        <asp:Button ID="searchBtn" style="cursor: pointer;" runat="server" Text="Go" />

    </div>

    &nbsp;

    <div id="order_search_data">
        
        <asp:GridView ID="GridView1" runat="server" AllowPaging="True" PageSize="25"
            AllowSorting="True" AutoGenerateColumns="False" DataKeyNames="order_id" 
            RowStyle-CssClass="highlightedGvRow"
            DataSourceID="SqlDataSource1" Font-Size="11px" >
       
            <Columns>
                <asp:BoundField DataField="cell_num" HeaderText="Cell Number" SortExpression="cell_num" />
                <asp:BoundField DataField="fname" HeaderText="First Name" SortExpression="fname" />
                <asp:BoundField DataField="lname" HeaderText="Last Name" SortExpression="lname" />
                <asp:BoundField DataField="serial_num" HeaderText="Serial Number" SortExpression="serial_num" />
                <asp:BoundField DataField="esn" HeaderText="ESN" SortExpression="esn" Visible="false" />
                <asp:BoundField DataField="Last Usage" HeaderText="Last Usage" SortExpression="Last Usage" Visible="true" />
                <asp:BoundField DataField="Expiration" HeaderText="Expiration" SortExpression="Expiration" Visible="true" />
                <asp:BoundField DataField="MinutesAvailable" HeaderText="Minutes Avail" SortExpression="MinutesAvailable" Visible="true" />
                <asp:BoundField DataField="CashBalance" HeaderText="Cash Balance" SortExpression="CashBalance" Visible="true" />
                <asp:BoundField DataField="Status" HeaderText="Status" SortExpression="Status" Visible="true" />
            </Columns>
        </asp:GridView>

        <asp:SqlDataSource ID="SqlDataSource1" runat="server" 
            ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>"  >
            <SelectParameters>
                <asp:ControlParameter ControlID="cellNumberInput" Name="CellNumber" 
                        DefaultValue="%" Type="String" />

                <asp:ControlParameter ControlID="lnameInput" Name="LastName" 
                        DefaultValue="%" Type="String" />
                        
                <asp:ControlParameter ControlID="searchSerialNumFld" Name="SerialNumber" 
                        DefaultValue="%" Type="String" />

                <asp:QueryStringParameter Name="InitialAgent" Type="String" />
            </SelectParameters> 
        </asp:SqlDataSource>
        
    </div>

</asp:Content>
