<%@ Page Title="Users" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Users.aspx.vb" Inherits="Users" ClientIDMode="Static" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">
    
    <div id="mainUserDiv">

        <div id="searchUsers" style="width: 40%; height: 100%; float: left;">
            <fieldset id="searchUserFieldset" style="min-height: 410px;">
                <legend>Search Users</legend>
                <div id="searchUsersBar">

                    <label>User Id:</label>
                    <asp:TextBox CssClass="userSearchFld"  ID="userIdSearchFld" runat="server"></asp:TextBox>
                    &nbsp;
                    <label>Name:</label>
                    <asp:TextBox CssClass="userSearchFld" ID="userNameSearchFld" runat="server"></asp:TextBox>

                    <asp:Button ID="searchUsersBtn" runat="server" Text="Go" />

                </div>

                <br />

                <div id="displayUsersGV">

                    <asp:GridView ID="usersGridView" runat="server" AllowPaging="True" PageSize="15"
                        AllowSorting="True" AutoGenerateColumns="False"
                        DataSourceID="searchUsersDataSource" Width="175px">
                        <Columns>
                            <asp:BoundField DataField="UserName" HeaderText="User ID" SortExpression="UserName" />
                            <asp:BoundField DataField="FullName" HeaderText="User Name" SortExpression="FullName" />
                        </Columns>
                    </asp:GridView>

                    <asp:SqlDataSource ID="searchUsersDataSource" runat="server" 
                        ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                        ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>"  >
                        <SelectParameters>
                            <asp:ControlParameter ControlID="userNameSearchFld" Name="UserName" 
                                    DefaultValue="%" Type="String" />

                            <asp:ControlParameter ControlID="userIdSearchFld" Name="UserID" 
                                    DefaultValue="%" Type="String" />
                        </SelectParameters> 
                    </asp:SqlDataSource>

                </div>
            </fieldset>
        </div>

        <div id="userDetails" style="width: 58%; height: 100%; float: right;">

            <span id="pwLenSpan" style="display: none;"><%= Membership.MinRequiredPasswordLength %></span>

            <fieldset id="userDetailsFieldset" style="min-height: 410px; position: relative;">
                <legend>User Details</legend>

                <div class="userDivs">

                    <span>
                        <label>User ID:</label>
                        <asp:TextBox ID="userIdEditFld" runat="server" onkeydown="return TrapBackspace(event);"></asp:TextBox>
                        <asp:Label ID="userIdErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                    </span>

                </div>

                <div class="userDivs">

                    <span>
                        <label>User Name:</label>
                        <asp:TextBox ID="userNameEditFld" runat="server"></asp:TextBox>
                    </span>

                </div>

                <div class="userDivs">

                    <span>
                        <label>User Level:</label>
                        <asp:DropDownList ID="userLevelDropdown" runat="server">
                        </asp:DropDownList>
                    </span>

                </div>

                <div class="userDivs">

                    <span>
                        <label>Password:</label>
                        <asp:TextBox ID="passwordEditFld" runat="server" TextMode="Password"></asp:TextBox>
                        <asp:Label ID="passwordErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                    </span>

                </div>

                <div class="userDivs">

                    <span>
                        <label>Confirm Password:</label>
                        <asp:TextBox ID="confirmPassword" runat="server" TextMode="Password"></asp:TextBox>
                        <asp:Label ID="confirmPasswordErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                    </span>

                </div>

                <div class="userDivs">

                    <span>
                        <label>Email:</label>
                        <asp:TextBox ID="emailEditFld" runat="server"></asp:TextBox>
                        <asp:Label ID="emailErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                    </span>

                </div>

                <div class="userDivs">

                    <span>
                        <label>Credit Limit:</label>
                        <asp:TextBox ID="creditLimitField" runat="server"></asp:TextBox>
                        <asp:Label ID="creditLimitErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                    </span>

                </div>

                <div class="userDivs" style="margin-left: 0px; margin-top: 5px;">

                    <span>
                        <label style="display: inline;">Monitor Account:</label>
                        <asp:CheckBox ID="monitorAgent" Checked="true" runat="server" style="vertical-align: middle;" />
                    </span>

                </div>

                <div id="manageUsersErrorMsgDiv" runat="server" style="color: Red;">
                </div>

                <div class="userDivs" style="position: absolute; bottom: 0; width: 95%; margin-bottom: 5px;">
                    <div id="manageUserBtnDiv" style="float: right;">
                        <div id="createUserBtnDiv" style="display: inline;">
                            <asp:Button ID="createUserBtn" runat="server" Text="Create User" OnClientClick="return createUser();" />
                        </div>
                        <div id="updateUserBtnDiv" style="display: none;">
                            <asp:Button ID="updateUserBtn" runat="server" Text="Update" OnClientClick="return updateUser();"/>
                            <asp:Button ID="deleteUserBtn" runat="server" Text="Remove" OnClientClick="return RemoveUser();"/>
                            <input type="button" id="clearUserFieldsBtn" onclick="clearUserFields();" value="Clear" />
                        </div>
                    </div>
                    
                </div>

            </fieldset>
        </div>
    </div>

</asp:Content>

