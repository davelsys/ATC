<%@ Page Title="Renewals" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="ViewRenewals.aspx.vb" Inherits="ViewRenewals" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">
    
    <div>

        <div id="searchRenewalsDiv" class="searchDiv">

            <div style="width: 30%;">
                <fieldset>
                    <legend>Filter</legend>
                    <asp:RadioButtonList ID="filterRenewalsRadioList" runat="server" RepeatDirection="Horizontal" 
                            AutoPostBack="true" style="float: left;">
                        <asp:ListItem Selected="true" Text="All" Value="all" />
                        <asp:ListItem Text="Monthly" Value="monthly" />
                        <asp:ListItem Text="Cash" Value="cash" />
                        <asp:ListItem Text="Intl" Value="intl" />
                    </asp:RadioButtonList>
                </fieldset>
            </div>

            <div style="width: 30%;">
                <fieldset>
                    <legend>Search</legend>
                    <span>
                        <label>Cell</label>
                        <span class="globalSearchFld">
			                <asp:TextBox ID="searchRenewCell" runat="server" ClientIDMode="Static"></asp:TextBox>
			                <img alt="" src="images/delete.png" title="Clear global search." />
		                </span>
                    </span>

                    <span>
                        <asp:Button ID="searchRenewBtn" runat="server" Text="Go" />
                    </span>

                </fieldset>
            </div>

        </div>
        
        <div id="renewalsGVDiv">
            <asp:GridView ID="renewalsGV" runat="server" AutoGenerateColumns="True" Font-Size="11px" Width="100%"
                RowStyle-CssClass="highlightedGvRow"
                DataSourceID="renewalsGVDataSource" AllowSorting="true" AllowPaging="true" PageSize="25">
            </asp:GridView>
        </div>

        <asp:SqlDataSource ID="renewalsGVDataSource" runat="server"
            ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>" >
            <SelectParameters>

                <asp:ControlParameter ControlID="searchRenewCell" Name="CellNum" 
                        DefaultValue="%" Type="String" />

            </SelectParameters> 
            
        </asp:SqlDataSource>

        <div id="downloadBtnDiv" class="downloadBtnDiv" runat="server">
            <input id="downloadRenewalsBtn" type="button" value="Download" runat="server" />
        </div>

    </div>

    <script type="text/javascript" src="Scripts/GlobalCellSearch.js"></script>
    <script type="text/javascript">

        var gcsManager = new atcGlobalCellSearch( 'globalSearchFld', 'searchRenewBtn', '<%= searchRenewCell.ClientID %>', '<%= renewalsGV.ClientID %>', '<%= _gcsManager.IsGSOn %>' );

    </script>

</asp:Content>

