<%@ Page Title="Manage Accounts" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="ManageAccounts.aspx.vb" Inherits="ManageAccounts" ClientIDMode="Static" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div id="containerDiv">
        
        <div id="viewAccountsDiv" style="width: 40%; float: left;">
            <fieldset>
                <legend>Search Accounts</legend>

                <div id="searchAccountsDiv">
                    <span>
                        <label>Name:</label>
                        <asp:TextBox ID="searchNameFld" runat="server"></asp:TextBox>
                    </span>
                    <span>
                        <asp:Button ID="searchAccountsBtn" runat="server" Text="Go" />
                    </span>
                </div>

                <br />

                <div id="accountsGridviewDiv">
                     <asp:GridView ID="accountsGridView" runat="server" AllowPaging="True" PageSize="15"
                        AllowSorting="True" AutoGenerateColumns="False"
                        DataSourceID="searchAccountsDataSource" Width="100%">
                        <Columns>
                            <asp:BoundField DataField="AccountName" HeaderText="Account Name" SortExpression="AccountName" />
                            <asp:BoundField DataField="Password" HeaderText="Password" SortExpression="Password" />
                            <asp:BoundField DataField="Active" HeaderText="Active" SortExpression="Active" />
                        </Columns>
                    </asp:GridView>

                    <asp:SqlDataSource ID="searchAccountsDataSource" runat="server">
                        <SelectParameters>
                            <asp:ControlParameter ControlID="searchNameFld" Name="AccountName" 
                                    DefaultValue="%" Type="String" />
                        </SelectParameters> 
                    </asp:SqlDataSource>          
                </div>

            </fieldset>
        </div>

        <div id="editAccountsDiv" style="width: 55%; float: right; position: relative;">

            <asp:HiddenField ID="hiddenAccountId" runat="server" />
            <asp:HiddenField ID="hiddenAccountName" runat="server" />

            <fieldset style="height: 250px;">
                <legend>Account Details</legend>

                <div id="editAccountDiv">
                    
                    <span>
                        <label>Account Name:</label>
                        <asp:TextBox ID="editAccountNameFld" runat="server"></asp:TextBox>
                        <asp:Label ID="accountNameMsgLbl" runat="server" ForeColor="Red"></asp:Label>
                    </span>

                    <span>
                        <label>Password:</label>
                        <asp:TextBox ID="editPasswordFld" runat="server"></asp:TextBox>
                        <asp:Label ID="accountPasswordMsgLbl" runat="server" ForeColor="Red"></asp:Label>
                    </span>

                    <span style="display: block; margin-top: 3px;">
                        <label style="display: inline;">Active:</label>
                        <asp:CheckBox ID="accountActiveChk" runat="server" Checked="true" />
                    </span>

                    <div id="editAccoutsErrorMsg" runat="server" style="color: Red; margin: 3px;">
                    </div>

                    <div style="position: absolute; bottom: 0; width: 95%; margin-bottom: 20px;">
                        <div id="manageAccountsBtnDiv" style="float: right;">
                            <div id="createAccountBtnDiv" style="display: inline;">
                                <asp:Button ID="createAccountBtn" runat="server" Text="Create Account" OnClientClick="return CreateAccount();" />
                            </div>
                            <div id="updateAccountBtnDiv" style="display: none;">
                                <asp:Button ID="updateAccountBtn" runat="server" Text="Update" OnClientClick="return UpdateAccount();" />
                                <input type="button" id="clearAccountFieldsBtn"  value="Clear" onclick="ClearAccountFields();" />
                            </div>
                        </div>
                    </div>

                </div>

            </fieldset>
        </div>

    </div>

</asp:Content>

