﻿<%@ Page Title="VZPP" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false"
    CodeFile="VZPP.aspx.vb" Inherits="VZPP" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="Server">


    <div id="searchPinsPnl" class="searchDivPins" style="margin-bottom: 0; padding: 0; width: 98%;">

        <div id="pollVZPPDiv" runat="server" style="width: 18%">
            <fieldset style="width: 98%; border-right: none">
                <legend>Polling</legend>
                <asp:RadioButtonList ID="pollvzppRadioList" CssClass="noPadding" runat="server"
                    RepeatDirection="Horizontal" AutoPostBack="true">
                    <asp:ListItem Value="all" Selected="True">All</asp:ListItem>
                    <asp:ListItem Value="pollon">Yes</asp:ListItem>
                    <asp:ListItem Value="polloff">No</asp:ListItem>
                </asp:RadioButtonList>
            </fieldset>
        </div>

        <div id="filterVZPPDiv" style="width: 28%;" runat="server">
            <fieldset style="width: 98%; border-right: none">
                <legend>Status</legend>
                <asp:RadioButtonList ID="filtervzppRadioList" CssClass="noPadding" runat="server" RepeatDirection="Horizontal" AutoPostBack="true">
                    <asp:ListItem Value="open" >Open</asp:ListItem>
                    <asp:ListItem Value="completed">Completed</asp:ListItem>
                    <asp:ListItem Value="error">Error</asp:ListItem>
                    <asp:ListItem Value="all" Selected="True">All</asp:ListItem>
                </asp:RadioButtonList>
            </fieldset>
        </div>

        <div id="searchVZPPDiv" style="width: 54%;">
            <fieldset style="width: 98%;">
                <legend>Search</legend>
                <label>
                    Cell #:</label>
                <span class="globalSearchFld">
                    <asp:TextBox ID="phoneNumberSearchFld" runat="server" ClientIDMode="Static"></asp:TextBox>
                    <img alt="" src="images/delete.png" title="Clear global search." />
                </span>
                <label>
                    ESN:</label>
                <asp:TextBox ID="esnSearchFld" runat="server"></asp:TextBox>
                <asp:Button ID="searchBtn" runat="server" Text="Go" />
                &nbsp;
             <%--<asp:Button ID="restartVer" runat="server" Text="Restart" Width="57px" Style="text-align: left" />--%>
            </fieldset>
        </div>


    </div>

    <div style="overflow: auto; width: 930px; height: 100%;">
        <%--Note: There is code that depends on the header text of column being 'Cell'--%>
        <asp:GridView ID="vzppRespGridview" runat="server" Width="98.3%" AutoGenerateColumns="false"
            Font-Size="11px" ViewStateMode="Enabled" DataKeyNames="vzppreqid" ClientIDMode="static"
            DataSourceID="vzppRespGridviewDataSource" AllowPaging="true" PageSize="20" AllowSorting="True">
            <Columns>
                <asp:BoundField DataField="vzppreqid" HeaderText="vzppreqid" SortExpression="vzppreqid" Visible="false" />
                <asp:BoundField DataField="Processed" HeaderText="Processed" SortExpression="Processed" />
                <asp:BoundField DataField="Reqtype" HeaderText="Request" SortExpression="Reqtype" />
                <asp:BoundField DataField="MDN" HeaderText="Cell" SortExpression="MDN" />
                <asp:BoundField DataField="ESN" HeaderText="ESN" SortExpression="ESN" />
                <asp:BoundField DataField="Planname" HeaderText="Plan" SortExpression="Planname" />
                <asp:BoundField DataField="VZAcct" HeaderText="VZAccount" SortExpression="VZAcct" />
                <asp:BoundField DataField="VZPasswd" HeaderText="VZ Passcode" SortExpression="VZPasswd" />
                <asp:BoundField DataField="VendTransId" HeaderText="VZ TransID" SortExpression="VendTransId" />
                <asp:BoundField DataField="RespAckMsg" HeaderText="RespAckMsg" SortExpression="RespAckMsg" />
                <asp:BoundField DataField="Poll" HeaderText="Polling" SortExpression="Polling" />
                <asp:BoundField DataField="PollCount" HeaderText="Count" SortExpression="PollCount" />
                <asp:BoundField DataField="LastPoll" HeaderText="Last Poll" SortExpression="LastPoll" />
                <asp:TemplateField>
                    <HeaderTemplate>
                        Action
                    </HeaderTemplate>
                    <ItemTemplate>
                        <asp:DropDownList ID="badTransDropdown" OnSelectedIndexChanged="badTransDropdown_SelectedIndexChanged"
                            EnableViewState="true" AutoPostBack="true" runat="server" Visible="false" CssClass="badTransactionDropdown">
                            <asp:ListItem></asp:ListItem>
                            <asp:ListItem>RePoll</asp:ListItem>
                            <asp:ListItem>ReSubmit</asp:ListItem>
                            <asp:ListItem>Clear</asp:ListItem>
                            <%--<asp:ListItem>Cancel</asp:ListItem>--%>
                            <%--<asp:ListItem>Clear</asp:ListItem>--%>
                        </asp:DropDownList>
                    </ItemTemplate>
                    <FooterTemplate>
                    </FooterTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
        <asp:SqlDataSource ID="vzppRespGridviewDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>">
            <SelectParameters>
                <asp:ControlParameter ControlID="phoneNumberSearchFld" Name="PhoneNumber" DefaultValue="%"
                    Type="String" />
                <asp:ControlParameter ControlID="esnSearchFld" Name="Pin" DefaultValue="%" Type="String" />
            </SelectParameters>
        </asp:SqlDataSource>
        <input type="hidden" id="selectedRowIndex" value="-1" />
    </div>
    <label id="lblPinsSummary" runat="server"></label>
    <%--<div id="downloadBtnDiv" class="downloadBtnDiv" runat="server" style="margin-right: 5px;">
        <input id="refreshCellNum" runat="server" type="button" onclick="refreshCellNumFromServer();"
            value="Refresh Cell" />
       <input id="downloadPinsBtn" type="button" runat="server" value="Download" />
   </div>--%>
    <script type="text/javascript" src="Scripts/GlobalCellSearch.js"></script>
    <script type="text/javascript">

        var gcsManager = new atcGlobalCellSearch('globalSearchFld', 'searchPinsBtn', '<%= phoneNumberSearchFld.ClientID %>', '<%= vzppRespGridview.ClientID%>', '<%= _gcsManager.IsGSOn %>');

        function refreshCellNumFromServer() {

            var pinsTblRows = document.getElementById("vzppRespGridview").getElementsByTagName("tr");
            var selectedIndex = document.getElementById("selectedRowIndex").value;
            // Take advantage of the global cell search function getCellIndex()
            var cellColumnIndex = gcsManager.getCellIndex();

            if (selectedIndex > -1) {
                var cellNum = pinsTblRows[selectedIndex].getElementsByTagName("td")[cellColumnIndex].innerText;
                var hasDigitPtrn = new RegExp(/[0-9]{1}$/);
                if (!hasDigitPtrn.test(cellNum)) {
                    alert('Cannot update invalid cell number.');
                    return false;
                }
                var args = "refreshCellNum:" + cellNum + "," + selectedIndex;
                UseCallBack(args, "refreshCellNum");
            } else {
                alert("Please select a row.");
            }

        }

        function setPinCellNum(newCell) {

            var pinsTblRows = document.getElementById("vzppRespGridview").getElementsByTagName("tr");
            var selectedIndex = document.getElementById("selectedRowIndex").value;
            // Take advantage of the global cell search function getCellIndex()
            var cellColumnIndex = gcsManager.getCellIndex();

            pinsTblRows[selectedIndex].getElementsByTagName("td")[cellColumnIndex].innerText = newCell;

        }

    </script>
</asp:Content>
