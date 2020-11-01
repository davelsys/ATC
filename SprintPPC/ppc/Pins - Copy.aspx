<%@ Page Title="Pins" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Pins.aspx.vb" Inherits="Pins" EnableEventValidation="false"  %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">
    
    <div id="searchPinsPnl" class="searchDiv">

        <div id="filterPinsDiv" style="width: 20%;">
            <fieldset>
                <legend>Filter</legend>
                    
                <asp:RadioButtonList ID="filterPinsRadioList" runat="server" RepeatDirection="Horizontal"
                                    AutoPostBack="true" >
                    <asp:ListItem Value="all" Selected="True">All</asp:ListItem>
                    <asp:ListItem Value="failed">Failed</asp:ListItem>
                </asp:RadioButtonList>

            </fieldset>
        </div>

        <div id="searchPinsDiv" style="width: 50%;">
            <fieldset>
                <legend>Search</legend>

                <span>
                    <label>Cell #:</label>
                    <span class="globalSearchFld">
			            <asp:TextBox ID="phoneNumberSearchFld" runat="server" ClientIDMode="Static"></asp:TextBox>
			            <img alt="" src="images/delete.png" title="Clear global search." />
		            </span>
                </span>

                <span>
                    <label>Pin:</label>
                    <asp:TextBox ID="pinSearchFld" runat="server"></asp:TextBox>
                </span>

                <span>
                    <asp:Button ID="searchPinsBtn" runat="server" Text="Go" />
                </span>
            </fieldset>
                
        </div>

    </div>

    <div style="overflow:auto; width: 930px; height: 100%;">
        <%--Note: There is code that depends on the header text of column being 'Cell'--%>
        <asp:GridView ID="pinsGridview" runat="server" Width="98.3%" AutoGenerateColumns="false"
                        Font-Size="11px" ViewStateMode="Enabled" datakeynames="id" ClientIDMode="static"
                        DataSourceID="pinsGridviewDataSource"
                        AllowPaging="true" PageSize="20" AllowSorting="True" >
            <Columns>
                <asp:BoundField DataField="DatePurchased" HeaderText="Purchase Date" SortExpression="DatePurchased" />
                <asp:BoundField DataField="PIN" HeaderText="Pin" SortExpression="PIN" />
                <asp:BoundField DataField="Cellnum" HeaderText="Cell" SortExpression="Cellnum" />
                <asp:BoundField DataField="Assigned" HeaderText="Assigned" SortExpression="Assigned" />
                <asp:BoundField DataField="PinType" HeaderText="Pin Type" SortExpression="PinType" />
                <asp:BoundField DataField="control" HeaderText="Control" SortExpression="control" />
                <asp:BoundField DataField="postbalamt" HeaderText="Cash" SortExpression="postbalamt" />
                <asp:BoundField DataField="postbalpin" HeaderText="Stacked" SortExpression="postbalpin" />
                <asp:BoundField DataField="status" HeaderText="Status" SortExpression="status" />
                <asp:BoundField DataField="retrycount" HeaderText="Retries" SortExpression="retrycount" />
                <asp:templatefield>
                    <HeaderTemplate>Action</HeaderTemplate>
                    <ItemTemplate>
                        <asp:DropDownList ID="badTransDropdown" OnSelectedIndexChanged="badTransDropdown_SelectedIndexChanged" 
                                    EnableViewState="true" AutoPostBack="true"
                                    runat="server" Visible="false" CssClass="badTransactionDropdown">
                            <asp:ListItem></asp:ListItem>
                            <asp:ListItem>Retry</asp:ListItem>
                            <asp:ListItem>Manual</asp:ListItem>
                            <asp:ListItem>Cancel</asp:ListItem>
                            <asp:ListItem>Clear</asp:ListItem>
                        </asp:DropDownList>
                    </ItemTemplate>
                    <FooterTemplate></FooterTemplate>
                </asp:templatefield>
            </Columns>
        </asp:GridView>

        <asp:SqlDataSource ID="pinsGridviewDataSource" runat="server"
            ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>" >
            <SelectParameters>

                <asp:ControlParameter ControlID="phoneNumberSearchFld" Name="PhoneNumber" 
                        DefaultValue="%" Type="String" />

                <asp:ControlParameter ControlID="pinSearchFld" Name="Pin" 
                        DefaultValue="%" Type="String" />

            </SelectParameters> 
        </asp:SqlDataSource>

        <input type="hidden" id="selectedRowIndex" value="-1" />

    </div>
    
    <div id="downloadBtnDiv" class="downloadBtnDiv" runat="server" style="margin-right: 5px;">
        <input id="refreshCellNum" runat="server" type="button" onclick="refreshCellNumFromServer();" value="Refresh Cell"/>
        <input id="downloadPinsBtn" type="button" runat="server" value="Download" />
    </div>

    <script type="text/javascript" src="Scripts/GlobalCellSearch.js"></script>
    <script type="text/javascript">

        var gcsManager = new atcGlobalCellSearch( 'globalSearchFld', 'searchPinsBtn', '<%= phoneNumberSearchFld.ClientID %>', '<%= pinsGridview.ClientID %>', '<%= _gcsManager.IsGSOn %>' );

        function refreshCellNumFromServer() {

            var pinsTblRows = document.getElementById( "pinsGridview" ).getElementsByTagName( "tr" );
            var selectedIndex = document.getElementById( "selectedRowIndex" ).value;
            // Take advantage of the global cell search function getCellIndex()
            var cellColumnIndex = gcsManager.getCellIndex();

            if ( selectedIndex > -1 ) {
                var cellNum = pinsTblRows[selectedIndex].getElementsByTagName( "td" )[cellColumnIndex].innerText;
                var hasDigitPtrn = new RegExp( /[0-9]{1}$/ );
                if ( !hasDigitPtrn.test( cellNum ) ) {
                    alert( 'Cannot update invalid cell number.' );
                    return false;
                }
                var args = "refreshCellNum:" + cellNum + "," + selectedIndex;
                UseCallBack( args, "refreshCellNum" );
            } else {
                alert( "Please select a row." );
            }

        }

        function setPinCellNum( newCell ) {

            var pinsTblRows = document.getElementById( "pinsGridview" ).getElementsByTagName( "tr" );
            var selectedIndex = document.getElementById( "selectedRowIndex" ).value;
            // Take advantage of the global cell search function getCellIndex()
            var cellColumnIndex = gcsManager.getCellIndex();

            pinsTblRows[selectedIndex].getElementsByTagName( "td" )[cellColumnIndex].innerText = newCell;

        }

    </script>

</asp:Content>

