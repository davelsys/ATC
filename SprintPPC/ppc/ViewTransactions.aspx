<%@ Page Title="Transactions" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="ViewTransactions.aspx.vb" Inherits="ViewTransactions" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server"></asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div id="mainViewTransGrid">

        <div id="searchTransDiv" class="searchDiv">

            <div id="searchByTransTypeDiv" style="width: 25%;">
                <fieldset>
                    <legend>Filter</legend>

                    <asp:RadioButtonList ID="selectTransactionRadioList" runat="server" RepeatDirection="Horizontal" AutoPostBack="true">
                        <asp:ListItem Selected="true"  Text="All" Value="all" />
                        <asp:ListItem Text="Credit Card" Value="cc" />
                        <asp:ListItem Text="Agent" Value="agent" />
                    </asp:RadioButtonList>

                </fieldset>
            </div>

            <div id="searchByOtherDiv" style="width: 60%;">
                
                <fieldset>

                    <legend>Search</legend>
                    <span>
                        <label>Cell</label>
                        <span class="globalSearchFld">
			                <asp:TextBox ID="searchCellFld" runat="server" ClientIDMode="Static"></asp:TextBox>
			                <img alt="" src="images/delete.png" title="Clear global search." />
		                </span>
                    </span>

                    <span>
                        <label>Date</label>
                        <asp:TextBox ID="searchDateFld" runat="server" Width="125"></asp:TextBox>
                    </span>

                    <span id="searchAgentSpan" runat="server">
                        <label>Agent</label>
                        <asp:TextBox ID="searchAgentFld" runat="server" Width="100"></asp:TextBox>
                    </span>

                    <span>
                        <asp:Button ID="searchTransBtn" runat="server" Text="Go" />
                    </span>

                </fieldset>

            </div>

        </div>

        <div id="transactionGridviewDiv" style="width: 100%; overflow: auto;">
            <%--Note: There is code that depends on the header text of column being 'Cell'--%>
            <asp:GridView ID="transGridView" runat="server" AllowSorting="true"
                    Font-Size="11px" Width="100%" DataSourceID="transGridViewDataSource"
                    AutoGenerateColumns="False" AllowPaging="true" PageSize="25" >
                    <Columns>
                        <asp:BoundField DataField="Cell" HeaderText="Cell" SortExpression="Cell" ItemStyle-Width="75px"/>
                        <asp:BoundField DataField="Type" HeaderText="Type" SortExpression="Type"/>
                        <asp:BoundField DataField="user" HeaderText="User" SortExpression="user" />
                        <asp:BoundField DataField="agent" HeaderText="Agent" SortExpression="agent" />
                        <asp:BoundField DataField="paydate" HeaderText="Pay Date" SortExpression="paydate" />
                        <asp:BoundField DataField="monthly_amt" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Monthly" SortExpression="monthly_amt" />
                        <asp:BoundField DataField="cash_amt" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Cash" SortExpression="cash_amt" />
                        <asp:BoundField DataField="intl_amt" DataFormatString="{0:c}" HtmlEncode="false" HeaderText="Intl" SortExpression="intl_amt" />
                        <asp:BoundField DataField="item_amt" DataFormatString="{0:c}" HtmlEncode="false" HeaderText="Items" SortExpression="item_amt" />
                        <asp:BoundField DataField="authmessage" HeaderText="Authorization Message" SortExpression="authmessage" />
                        <asp:BoundField DataField="authtransid" HeaderText="Auth ID" SortExpression="authtransid" />
                        <asp:BoundField DataField="total" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Total" SortExpression="total" />
                    </Columns>
            </asp:GridView>
                    
            <asp:SqlDataSource ID="transGridViewDataSource" runat="server"
                ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>" >
                <SelectParameters>

                    <asp:ControlParameter ControlID="searchCellFld" Name="CellNumber" 
                            DefaultValue="%" Type="String" />

                    <asp:ControlParameter ControlID="searchAgentFld" Name="Agent" 
                            DefaultValue="%" Type="String" />

                </SelectParameters> 
            
            </asp:SqlDataSource>

            <input type="hidden" id="selectedRowIndex" value="-1" />

        </div>

        <div id="downloadBtnDiv" class="downloadBtnDiv" runat="server">
            <input id="downloadTransactionBtn" type="button" value="Download" runat="server" />
        </div>

    </div>
    
    <script type="text/javascript" src="Scripts/GlobalCellSearch.js"></script>
    <script type="text/javascript">

        var gcsManager = new atcGlobalCellSearch( 'globalSearchFld', 'searchTransBtn', '<%= searchCellFld.ClientID %>', '<%= transGridView.ClientID %>', '<%= _gcsManager.IsGSOn %>' );

    </script>

    
</asp:Content>

