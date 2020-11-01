<%@ Page Title="Admin" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false"
    CodeFile="Admin.aspx.vb" Inherits="Admin" ClientIDMode="Static" EnableEventValidation="false" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="Server">
    <style type="text/css">
        .userDivs
        {
            margin: 3px;
        }
        
        .userDivs label
        {
            display: block;
            margin: 2px;
            width: 150px;
        }
        
        .userDivs input[type=text], .userDivs input[type=password], .userDivs select
        {
            width: 170px;
        }
        
        .uploadPinsRow
        {
            height: 30px;
        }
        
        .verticalDiv
        {
            height: 100%;
        }
        
        .verticalDiv fieldset
        {
            height: 95%;
        }
        
        .setComPlansOverflow
        {
            height: 80%;
            overflow: auto;
        }
        
        .comPlanHeader
        {
            font-weight: bold;
        }
        
        .comPlanRow
        {
            height: 25px;
            margin: 1px 0px;
        }
        
        .comPlanRow .comPlanCell
        {
            height: 100%;
            display: inline-block;
            padding-left: 3px;
        }
        
        #manageComPlansBtnDiv
        {
            border-top-width: 1pt;
            border-top-style: solid;
            border-top-color: Black;
            padding-top: 3px;
        }
        
        .comNameCol
        {
            float: left;
            width: 55%;
        }
        
        .comAmntCol
        {
            float: left;
            width: 40%;
        }
        
        .invalidInputStar
        {
            color: Red;
        }
        
        #manageComPlansBtnDiv input[type=submit], #manageComPlansBtnDiv input[type=button]
        {
            float: right;
            margin-left: 3px;
        }
        
        .verMdnGV
        {
            margin-bottom: 10px;
        }
    </style>
    <script type="text/javascript">

        window.onload = function () {

            toggleUpdateAccountBtnDiv();
            toggleUpdateUserBtnDiv();
            toggleUpdateEsnBtnDiv();
            setTabPnlsMinHeight(300);

            comPlans.init();

        }

        var comPlans = {

            planName: '',

            init: function () {
                this.setState();
                this.attachComHandlers();
            },

            attachComHandlers: function () {

                // Remember for inner scopes
                var self = this;

                // Select commission plan click
                var comGv = document.getElementById('selectComPlanGV')
                    , rows = comGv.getElementsByTagName('tr');
                // Skip the header
                for (var i = 1; i < rows.length; i++) {
                    rows[i].onclick = function () {
                        self.getComPlan(this.cells[0].innerText);
                    }
                }

                // Create commission plan button
                document.getElementById('createComPlanBtn').onclick = function () {
                    return self.createComPlan(self);
                }

                // Update commission plan button
                document.getElementById('updateComPlanBtn').onclick = function () {
                    return self.validateComAmnts();
                }

                // Clear commission button
                document.getElementById('clearComPlanBtn').onclick = function () {
                    self.clearComPlan();
                }

            },

            setState: function () {
                var updateDiv = document.getElementById('updateComPlanDiv')
                    , createDiv = document.getElementById('createComPlanDiv')
                    , planHdn = document.getElementById('updateComPlanHdn')
                // The plan label has a length greater
                // than zero only when updating a commission plan.
                    , isCreate = planHdn.value.length == 0;

                createDiv.style.display = isCreate ? 'block' : 'none';
                updateDiv.style.display = isCreate ? 'none' : 'block';

                (!isCreate) ? this.setUpdatingPlan(planHdn.value) : null;

            },

            setUpdatingPlan: function (name) {
                var planLbl = document.getElementById('updateComPlanLbl')
                    , planHdn = document.getElementById('updateComPlanHdn');

                planLbl.innerText = planHdn.value = name || '';
            },

            getUpdatingPlan: function () {
                return document.getElementById('updateComPlanHdn').value;
            },

            createComPlan: function (self) {
                var newComPlanName = document.getElementById('createComPlanNameFld').value;

                // Check if there is a plan name.
                if (newComPlanName.length <= 0) {
                    alert('Invalid plan name.'); return false;
                }

                // Check that the new plan name is unique
                var comGv = document.getElementById('selectComPlanGV')
                    , rows = comGv.getElementsByTagName('tr')
                    , nameArr = [];
                // Skip the header
                for (var i = 1; i < rows.length; i++) {
                    nameArr.push(rows[i].cells[0].innerText.toLowerCase());
                }
                if (nameArr.inArray(newComPlanName.toLowerCase())) {
                    alert('Plan name already exists.'); return false;
                }

                if (self.validateComAmnts()) {
                    // Set commission plan name
                    self.setUpdatingPlan(newComPlanName);
                } else {
                    return false;
                }

            },

            validateComAmnts: function () {
                var amntFlds = getElemsByClass('comPlanAmntFld')
                    , isFldValid = false
                    , invalidCounter = 0
                    , dlrPtrn = /^([0-9]+(\.[0-9][0-9])?)?$/;

                for (var i = 0; i < amntFlds.length; i++) {
                    isFldValid = (dlrPtrn.test(amntFlds[i].value.replace(/\$/, '').replace(/,/g, '')));
                    amntFlds[i].parentNode.getElementsByTagName('span')[0].style.display =
                          isFldValid ? 'none' : 'inline';
                    if (!isFldValid) { invalidCounter++; }
                }

                if (invalidCounter > 0) {
                    alert('There are invalid dollar amounts for the commission.');
                }

                return invalidCounter === 0;

            },

            getComPlan: function (name) {

                // Set the name for use by the response handler
                this.planName = name;

                var args = "GetComPlan:" + name;
                UseCallBack(args, "GetComPlan");
            },

            populateComPlan: function (str) {

                var jsonArr = str2Json(str)
                    , updateDiv = document.getElementById('updateComPlanDiv')
                    , createDiv = document.getElementById('createComPlanDiv')
                    , itemName
                    , amntFlds = getElemsByClass('comPlanAmntFld')
                    , getItemName = function (amntFld) {
                        var inputs = amntFld.parentNode.getElementsByTagName('input');
                        for (var i = 0; i < inputs.length; i++) {
                            if (inputs[i].type == 'hidden') {
                                return inputs[i].value;
                            }
                        }
                        return null;
                    }
                    , getItemAmnt = function (itemName) {
                        for (var i = 0; i < jsonArr.length; i++) {
                            if (jsonArr[i].item === itemName) {
                                return jsonArr[i].amount;
                            }
                        }
                        return '';
                    };


                for (var i = 0; i < amntFlds.length; i++) {
                    amntFlds[i].value = getItemAmnt(getItemName(amntFlds[i]));
                }

                // Set commission plan name
                this.setUpdatingPlan(this.planName);

                createDiv.style.display = 'none';
                updateDiv.style.display = 'block';

            },

            clearComPlan: function () {
                var amntFlds = getElemsByClass('comPlanAmntFld')
                    , updateDiv = document.getElementById('updateComPlanDiv')
                    , createDiv = document.getElementById('createComPlanDiv')
                    , newPlanNameFld = document.getElementById('createComPlanNameFld')
                    , invalidAmntsStars = getElemsByClass('invalidInputStar');

                for (var i = 0; i < amntFlds.length; i++) {
                    amntFlds[i].value = '';
                    // Indexes for the two arrays match, so use the same loop.
                    invalidAmntsStars[i].style.display = 'none';
                }

                newPlanNameFld.value = '';

                this.setUpdatingPlan('');

                updateDiv.style.display = 'none';
                createDiv.style.display = 'block';
            }

        }
        
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="Server">
    <div>
        <asp:TabContainer ID="adminTabContainer" runat="server" >
            <asp:TabPanel runat="server" ID="accountsTab" HeaderText="Accounts">
                <ContentTemplate>
                    <div id="containerDiv" style="overflow: auto;">
                        <div id="viewAccountsDiv" style="width: 40%; float: left;">
                            <fieldset>
                                <legend>Search Accounts</legend>
                                <div id="searchAccountsDiv">
                                    <span>
                                        <label>
                                            Name:</label>
                                        <asp:TextBox ID="searchNameFld" runat="server"></asp:TextBox>
                                    </span><span>
                                        <asp:Button ID="searchAccountsBtn" runat="server" Text="Go" />
                                    </span>
                                </div>
                                <br />
                                <div id="accountsGridviewDiv">
                                    <asp:GridView ID="accountsGridView" runat="server" AllowPaging="True" PageSize="20"
                                        AllowSorting="True" AutoGenerateColumns="False" Font-Size="11px" RowStyle-CssClass="highlightedGvRow"
                                        DataSourceID="searchAccountsDataSource" Width="100%">
                                        <Columns>
                                            <asp:BoundField DataField="AccountName" HeaderText="Account Name" SortExpression="AccountName" />
                                            <asp:BoundField DataField="Password" HeaderText="Password" SortExpression="Password" />
                                            <asp:BoundField DataField="Active" HeaderText="Active" SortExpression="Active" />
                                        </Columns>
                                    </asp:GridView>
                                    <asp:SqlDataSource ID="searchAccountsDataSource" runat="server">
                                        <SelectParameters>
                                            <asp:ControlParameter ControlID="searchNameFld" Name="AccountName" DefaultValue="%"
                                                Type="String" />
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
                                        <label style="display: block; width: 150px;">
                                            Account Name:</label>
                                        <asp:TextBox ID="editAccountNameFld" runat="server"></asp:TextBox>
                                        <asp:Label ID="accountNameMsgLbl" runat="server" ForeColor="Red"></asp:Label>
                                    </span><span>
                                        <label>
                                            Password:</label>
                                        <asp:TextBox ID="editPasswordFld" runat="server"></asp:TextBox>
                                        <asp:Label ID="accountPasswordMsgLbl" runat="server" ForeColor="Red"></asp:Label>
                                    </span><span style="display: block; margin-top: 3px;">
                                        <label style="display: inline;">
                                            Active:</label>
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
                                                <input type="button" id="clearAccountFieldsBtn" value="Clear" onclick="ClearAccountFields();" />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </fieldset>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>
            <asp:TabPanel ID="esnTab" runat="server" HeaderText="ESN">
                <ContentTemplate>
                    <div id="adminESNContainer" style="overflow: auto;">
                        <div id="displayEsnDiv" style="float: left; width: 52%;">
                            <fieldset>
                                <legend>Esn Numbers</legend>
                                <asp:GridView ID="esnGridView" runat="server" AllowPaging="True" PageSize="20" RowStyle-CssClass="highlightedGvRow"
                                    AllowSorting="True" AutoGenerateColumns="False" Font-Size="11px" DataSourceID="esnDataSource"
                                    Width="450px">
                                    <Columns>
                                        <asp:BoundField DataField="Serial#" HeaderText="Serial #" SortExpression="Serial#" />
                                        <asp:BoundField DataField="ESN" HeaderText="ESN" SortExpression="ESN" />
                                        <asp:BoundField DataField="International" HeaderText="International" SortExpression="International" />
                                        <asp:BoundField DataField="CustomerPin" HeaderText="Customer Pin" SortExpression="CustomerPin" />
                                        <asp:BoundField DataField="InsertedDate" DataFormatString="{0:MM/dd/yyyy}" HtmlEncode="false"
                                            HeaderText="Date Created" SortExpression="InsertedDate" />
                                    </Columns>
                                </asp:GridView>
                                <asp:SqlDataSource ID="esnDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                                    ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>">
                                    <SelectParameters>
                                        <asp:ControlParameter ControlID="serialNumSearchFld" Name="SerialNum" DefaultValue="%"
                                            Type="String" />
                                        <asp:ControlParameter ControlID="esnSearchFld" Name="ESN" DefaultValue="%" Type="String" />
                                        <asp:ControlParameter ControlID="intlSearchFld" Name="Intl" DefaultValue="%" Type="String" />
                                        <asp:ControlParameter ControlID="cusPinSearchFld" Name="CustPin" DefaultValue="%"
                                            Type="String" />
                                        <asp:QueryStringParameter Name="DateCreated" Type="String" />
                                    </SelectParameters>
                                </asp:SqlDataSource>
                            </fieldset>
                            <div id="downloadBtnDiv" class="downloadBtnDiv" visible="true" runat="server">
                            <input id="downloadReportBtn" type="button" runat="server"
                                value="Download" /><%--onclick="preventMultiClick();" --%>
                        </div>
                        </div>
                        
                        <div id="esnRightPnl" style="float: right; width: 45%;">
                            <div id="uploadEsnDiv">
                                <fieldset style="min-height: 80px;">
                                    <legend>Upload Files</legend>
                                    <div style="margin-top: 5px; margin-bottom: 5px;">
                                        <div style="float: left; display: inline-block; width: 70%;">
                                            <asp:FileUpload ID="esnUploadControl" runat="server" />
                                        </div>
                                        <div style="float: right; display: inline-block; width: 25%;">
                                            <input type="button" runat="server" value="Upload" id="uploadEsnFile" style="float: right;"
                                                onserverclick="uploadEsnFile_Click" onclick="if(!validateUpload('esnUploadControl', 'uploadStatusLbl', [ 'xls', 'xlsx' ])) return false;" />
                                        </div>
                                    </div>
                                    <div class="clear" />
                                    <div style="margin-top: 5px; margin-bottom: 5px;">
                                        <asp:Label ID="uploadStatusLbl" runat="server" ForeColor="Red">
                                        </asp:Label>
                                    </div>
                                </fieldset>
                            </div>
                            <div id="manageEsnDiv">
                                <fieldset class="esnFieldset" style="position: relative;">
                                    <legend>Manage ESN</legend>
                                    <div>
                                        <label>
                                            Serial #:</label>
                                        <asp:TextBox ID="manageSerialFld" runat="server"></asp:TextBox>
                                        <asp:HiddenField ID="originalSerial" runat="server" />
                                    </div>
                                    <div>
                                        <label>
                                            ESN:</label>
                                        <asp:TextBox ID="manageEsnFld" runat="server"></asp:TextBox>
                                    </div>
                                    <div>
                                        <label>
                                            Intl:</label>
                                        <asp:TextBox ID="manageIntlFld" runat="server"></asp:TextBox>
                                    </div>
                                    <div>
                                        <label>
                                            Customer Pin:</label>
                                        <asp:TextBox ID="manageCusPinFld" runat="server"></asp:TextBox>
                                    </div>
                                    <div>
                                        <span id="manageEsnErrorMsg" runat="server" style="color: Red;"></span>
                                    </div>
                                    <div style="position: absolute; bottom: 0; width: 95%; margin-bottom: 10px;">
                                        <div id="manageEsnBtnDiv" style="float: right;">
                                            <div id="createEsnDiv" style="display: inline;" runat="server">
                                                <asp:Button ID="createEsnBtn" runat="server" Text="Create ESN" OnClientClick="return requireAllEsnFields();" />
                                            </div>
                                            <div id="udEsnDiv" style="display: none;" runat="server">
                                                <asp:Button ID="updateEsnBtn" runat="server" Text="Update ESN" OnClientClick="return requireAllEsnFields();" />
                                                <asp:Button ID="deleteEsnBtn" runat="server" Text="Delete ESN" OnClientClick="return deleteEsn();" />
                                                <input type="button" id="clearEsnBtn" value="Clear" onclick="clearManageEsnFields();" />
                                            </div>
                                        </div>
                                    </div>
                                </fieldset>
                            </div>
                            <div id="searchEsnDiv">
                                <fieldset class="esnFieldset">
                                    <legend>Search ESN</legend>
                                    <div>
                                        <label>
                                            Serial #:</label>
                                        <asp:TextBox ID="serialNumSearchFld" runat="server"></asp:TextBox>
                                    </div>
                                    <div>
                                        <label>
                                            ESN:</label>
                                        <asp:TextBox ID="esnSearchFld" runat="server"></asp:TextBox>
                                    </div>
                                    <div>
                                        <label>
                                            Intl:</label>
                                        <asp:TextBox ID="intlSearchFld" runat="server"></asp:TextBox>
                                    </div>
                                    <div>
                                        <label>
                                            Customer Pin:</label>
                                        <asp:TextBox ID="cusPinSearchFld" runat="server"></asp:TextBox>
                                    </div>
                                    <div>
                                        <span>
                                            <label>
                                                Date Created:</label>
                                            <asp:TextBox ID="dateCreatedSearchFld" runat="server"></asp:TextBox>
                                        </span><span>
                                            <asp:Button ID="searchEsnBtn" Style="float: right;" runat="server" Text="Go" />
                                        </span>
                                    </div>
                                </fieldset>
                            </div>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>
            <asp:TabPanel ID="pinsTab" runat="server" HeaderText="Upload Pins">
                <ContentTemplate>
                    <div id="mainPinsDiv" style="overflow: auto; min-height: 170px;">
                        <div>
                           
                           <%-- <asp:RadioButtonList ID="filterPinCarrier" runat="server" RepeatDirection="Horizontal"
                                AutoPostBack="true">
                                <asp:ListItem Value="Page Plus" Selected="True">Page Plus</asp:ListItem>
                                <asp:ListItem Value="All Talk">All Talk</asp:ListItem>
                            </asp:RadioButtonList>--%>
                        </div>
                        <div id="displayImportedPins" style="float: left; width: 48%;">
                            <fieldset>
                                <legend>Imported Pins</legend>
                                <asp:GridView ID="seePinsGridview" runat="server" AllowPaging="True" PageSize="20"
                                    AutoGenerateColumns="True" Font-Size="11px">
                                    <%--Columns  <asp:BoundField DataField="pins" HeaderText="Pins" SortExpression="pins" ItemStyle-Width="195px" />  </Columns--%>
                                </asp:GridView>
                                <div id="uploadPinsBtnDiv" runat="server" visible="false" style="margin: 5px 0px 0px 0px;">
                                    <asp:Button ID="cancelPinsUpload" runat="server" Text="Cancel" Style="float: right;
                                        margin: 2px;" />
                                    <asp:Button ID="contPinsUpload" runat="server" Text="Continue" Style="float: right;
                                        margin: 2px;" />
                                </div>
                            </fieldset>
                        </div>
                        <div id="uploadPinsDiv" style="width: 50%; float: right; height: 100px;">
                            <fieldset style="height: 100%;">
                                <legend>Upload Pins</legend>
                                <div class="uploadPinsRow">
                                    <div style="float: left; display: inline-block; width: 45%;">
                                        <asp:FileUpload ID="uploadPinsControl" runat="server" />
                                    </div>
                                    <div style="float: right; display: inline-block; width: 25%; text-align: right;">
                                        <asp:Button ID="importPinsBtn" runat="server" Text="Upload" OnClientClick="return validateUpload('uploadPinsControl', 'uploadPinsErrorMsg', [ 'csv' ]);" />
                                    </div>
                                </div>
                                <div class="uploadPinsRow">
                                    <label>
                                        Default to</label>
                                    <asp:DropDownList ID="pinTypeDropdown" runat="server">
                                        <asp:ListItem>             </asp:ListItem>
                                        <asp:ListItem>$10.00 - PIN</asp:ListItem>
                                        <asp:ListItem>$12.00 - PIN</asp:ListItem>
                                        <asp:ListItem>$25.00 - PIN</asp:ListItem>
                                        <asp:ListItem>$50.00 - PIN</asp:ListItem>
                                        <asp:ListItem>$80.00 - PIN</asp:ListItem>
                                        <asp:ListItem>TalkNText 1200 $29.95</asp:ListItem>
                                        <asp:ListItem>Unlimited TalkNText $39.95</asp:ListItem>
                                        <%--***AT pin type names here and in atc_pins table MUST match the planname in the plans table!!!!!!!!!***--%>
                                        <%--<asp:ListItem Enabled="false">AT Unlimited</asp:ListItem>
                                        <asp:ListItem Enabled="false">AT Talk 250</asp:ListItem>
                                        <asp:ListItem Enabled="false">AT Talk 1200</asp:ListItem>
                                        <asp:ListItem Enabled="false">AT Talk 2000</asp:ListItem>--%>
                                    </asp:DropDownList>
                                </div>
                                <div class="uploadPinsRow">
                                    <div style="float: left; display: inline-block; width: 70%;">
                                        <asp:Label ID="uploadPinsErrorMsg" runat="server" ForeColor="Red">
                                        </asp:Label>
                                    </div>
                                </div>
                            </fieldset>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>
            <asp:TabPanel ID="usersTab" runat="server" HeaderText="Users">
                <ContentTemplate>
                    <div id="userTabContainer" style="overflow: auto;">
                        <div id="mainUserDiv">
                            <div id="searchUsers" style="width: 40%; height: 100%; float: left;">
                                <fieldset id="searchUserFieldset" style="min-height: 410px;">
                                    <legend>Search Users</legend>
                                    <div id="searchUsersBar">
                                        <label>
                                            User Id:</label>
                                        <asp:TextBox CssClass="userSearchFld" ID="userIdSearchFld" runat="server"></asp:TextBox>
                                        &nbsp;
                                        <label>
                                            Name:</label>
                                        <asp:TextBox CssClass="userSearchFld" ID="userNameSearchFld" runat="server"></asp:TextBox>
                                        <asp:Button ID="searchUsersBtn" runat="server" Text="Go" />
                                    </div>
                                    <br />
                                    <div id="displayUsersGV">
                                        <asp:GridView ID="usersGridView" runat="server" AllowPaging="True" PageSize="20"
                                            RowStyle-CssClass="highlightedGvRow" AllowSorting="True" AutoGenerateColumns="False"
                                            Font-Size="11px" DataSourceID="searchUsersDataSource" Width="175px">
                                            <Columns>
                                                <asp:BoundField DataField="UserName" HeaderText="User ID" SortExpression="UserName" />
                                                <asp:BoundField DataField="FullName" HeaderText="User Name" SortExpression="FullName" />
                                            </Columns>
                                        </asp:GridView>
                                        <asp:SqlDataSource ID="searchUsersDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                                            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>">
                                            <SelectParameters>
                                                <asp:ControlParameter ControlID="userNameSearchFld" Name="UserName" DefaultValue="%"
                                                    Type="String" />
                                                <asp:ControlParameter ControlID="userIdSearchFld" Name="UserID" DefaultValue="%"
                                                    Type="String" />
                                            </SelectParameters>
                                        </asp:SqlDataSource>
                                    </div>
                                </fieldset>
                            </div>
                            <div id="userDetails" style="width: 58%; height: 100%; float: right;">
                                <span id="pwLenSpan" style="display: none;">
                                    <%= Membership.MinRequiredPasswordLength %></span>
                                <fieldset id="userDetailsFieldset" style="min-height: 410px; position: relative;">
                                    <legend>User Details</legend>
                                    <div class="userDivs">
                                        <span>
                                            <label>
                                                User ID:</label>
                                            <asp:TextBox ID="userIdEditFld" runat="server"></asp:TextBox>
                                            <asp:HiddenField ID="hiddenUpdateUserId" runat="server" />
                                            <asp:Label ID="userIdErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                                        </span>
                                    </div>
                                    <div class="userDivs">
                                        <span>
                                            <label>
                                                User Name:</label>
                                            <asp:TextBox ID="userNameEditFld" runat="server"></asp:TextBox>
                                        </span>
                                    </div>
                                    <div class="userDivs">
                                        <span>
                                            <label>
                                                User Level:</label>
                                            <asp:DropDownList ID="userLevelDropdown" runat="server">
                                            </asp:DropDownList>
                                        </span>
                                    </div>
                                    <div class="userDivs">
                                        <span>
                                            <label>
                                                Commission Plan:</label>
                                            <asp:DropDownList ID="commissionPlansDrop" runat="server">
                                            </asp:DropDownList>
                                        </span>
                                    </div>
                                    <div id="passwordFieldsContainer" runat="server">
                                        <div class="userDivs">
                                            <span>
                                                <label>
                                                    Password:</label>
                                                <asp:TextBox ID="passwordEditFld" runat="server" TextMode="Password"></asp:TextBox>
                                                <asp:Label ID="passwordErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                                            </span>
                                        </div>
                                        <div class="userDivs">
                                            <span>
                                                <label>
                                                    Confirm Password:</label>
                                                <asp:TextBox ID="confirmPassword" runat="server" TextMode="Password"></asp:TextBox>
                                                <asp:Label ID="confirmPasswordErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                                            </span>
                                        </div>
                                    </div>
                                    <div class="userDivs">
                                        <span>
                                            <label>
                                                Email:</label>
                                            <asp:TextBox ID="emailEditFld" runat="server"></asp:TextBox>
                                            <asp:Label ID="emailErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                                        </span>
                                    </div>
                                    <div class="userDivs">
                                        <span>
                                            <label>
                                                Credit Limit:</label>
                                            <asp:TextBox ID="creditLimitField" runat="server"></asp:TextBox>
                                            <asp:Label ID="creditLimitErrorMsg" runat="server" ForeColor="Red"></asp:Label>
                                        </span>
                                    </div>
                                    <div class="userDivs" style="margin-left: 0px; margin-top: 5px;">
                                        <span>
                                            <label style="display: inline;">
                                                Monitor Account:</label>
                                            <asp:CheckBox ID="monitorAgent" Checked="true" runat="server" Style="vertical-align: middle;" />
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
                                                <asp:Button ID="updateUserBtn" runat="server" Text="Update" OnClientClick="return updateUser();" />
                                                <asp:Button ID="resetPwDialogBtn" runat="server" Text="Reset PW" />
                                                <asp:Button ID="unlockUserBtn" runat="server" Text="Unlock" />
                                                <asp:Button ID="deleteUserBtn" runat="server" Text="Remove" OnClientClick="return RemoveUser();" />
                                                <asp:Button ID="retroCommissionsBtn" runat="server" Text="Apply Commission" />
                                                <input type="button" id="clearUserFieldsBtn" onclick="clearUserFields();" value="Clear" />
                                            </div>
                                        </div>
                                    </div>
                                </fieldset>
                            </div>
                        </div>
                        <asp:ModalPopupExtender ID="resetPassowrdModalPopup" runat="server" TargetControlID="resetPwDialogBtn"
                            PopupControlID="resetPwModalPnl" CancelControlID="cancelPwResetBtn" OnCancelScript="ClearResetPanel();"
                            BackgroundCssClass="modalBackground">
                        </asp:ModalPopupExtender>
                        <asp:Panel ID="resetPwModalPnl" runat="server" CssClass="modalPanel">
                            <div style="font-size: medium; font-weight: bold;">
                                Reset Password</div>
                            <div id="resetPwErrorMsg" runat="server" style="height: 30px; color: Red;">
                            </div>
                            <div>
                                <div>
                                    <label>
                                        New Password</label>
                                    <asp:TextBox ID="resetPwField" runat="server" TextMode="Password"></asp:TextBox>
                                </div>
                                <div>
                                    <label>
                                        Confirm Password</label>
                                    <asp:TextBox ID="resetConfirmPassword" runat="server" TextMode="Password"></asp:TextBox>
                                </div>
                            </div>
                            <div style="position: absolute; bottom: 0; width: 95%;">
                                <div style="float: right;">
                                    <asp:Button ID="triggerPwResetBtn" runat="server" Text="Reset" OnClientClick="return ResetPassword();" />
                                    <asp:Button ID="cancelPwResetBtn" runat="server" Text="Cancel" />
                                </div>
                            </div>
                        </asp:Panel>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>
            <asp:TabPanel ID="commissionPlans" runat="server" HeaderText="Commission Plans">
                <ContentTemplate>
                    <div style="height: 425px;">
                        <div class="verticalDiv" style="float: left; width: 30%;" id="selectComPlanDiv">
                            <fieldset>
                                <legend>Plans</legend>
                                <asp:GridView ID="selectComPlanGV" runat="server" DataSourceID="selectComPlanSqlSource"
                                    AutoGenerateColumns="false" RowStyle-CssClass="highlightedGvRow" Font-Size="11px"
                                    Width="65%">
                                    <Columns>
                                        <asp:BoundField DataField="CommissionPlan" HeaderText="Commission Plans" />
                                    </Columns>
                                </asp:GridView>
                                <asp:SqlDataSource ID="selectComPlanSqlSource" runat="server" ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                                    SelectCommand="SELECT DISTINCT CommissionPlan FROM CommissionPlans"></asp:SqlDataSource>
                            </fieldset>
                        </div>
                        <div class="verticalDiv" style="float: right; width: 55%;" id="setComPlanDiv">
                            <fieldset>
                                <legend>Set Commissions</legend>
                                <div class="comPlanRow">
                                    <div class="comPlanCell comNameCol comPlanHeader">
                                        <label>
                                            Item Name</label>
                                    </div>
                                    <div class="comPlanCell comAmntCol comPlanHeader">
                                        <label>
                                            Item Commission</label>
                                    </div>
                                </div>
                                <div class="setComPlansOverflow">
                                    <asp:Repeater ID="comPlanItemRepeater" runat="server">
                                        <ItemTemplate>
                                            <div class="comPlanRow">
                                                <div class="comPlanCell comNameCol">
                                                    <label>
                                                        <%# Eval("Item") %>:</label>
                                                </div>
                                                <div class="comPlanCell comAmntCol">
                                                    <asp:TextBox ClientIDMode="AutoID" ID="comAmntFld" CssClass="comPlanAmntFld" runat="server"></asp:TextBox>
                                                    <span style="display: none;" class="invalidInputStar">*</span>
                                                    <asp:HiddenField ClientIDMode="Inherit" runat="server" />
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                                <div style="height: 10px">
                                </div>
                                <div id="manageComPlansBtnDiv" class="comPlanRow">
                                    <div id="createComPlanDiv">
                                        <div class="comPlanCell" style="width: 65%; float: left;">
                                            <label>
                                                Commission Name:</label>
                                            <asp:TextBox ID="createComPlanNameFld" runat="server"></asp:TextBox>
                                        </div>
                                        <div class="comPlanCell" style="width: 30%; float: right;">
                                            <asp:Button ID="createComPlanBtn" runat="server" Text="Create Plan" />
                                        </div>
                                    </div>
                                    <div id="updateComPlanDiv" style="display: none;">
                                        <div class="comPlanCell" style="width: 45%; float: left;">
                                            <label id="updateComPlanLbl">
                                            </label>
                                            <asp:HiddenField ID="updateComPlanHdn" runat="server" />
                                        </div>
                                        <div class="comPlanCell" style="width: 50%; float: right;">
                                            <input id="clearComPlanBtn" type="button" value="Clear" />
                                            <asp:Button ID="updateComPlanBtn" runat="server" Text="Update Plan" />
                                        </div>
                                    </div>
                                </div>
                            </fieldset>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>
            <asp:TabPanel ID="vzppBalanceTab" runat="server" HeaderText="VZPP Balance">
                <ContentTemplate>
                    <div style="height: 425px;">
                        <%--<div class="verticalDiv" style="float: left; width: 30%;" id="vzppBalanceDiv">--%>
                            <fieldset>
                                <legend>VZPP Balance</legend>
                                <table width="100%">
                                    <tr>
                                        <td align="right" class="comPlanHeader" >
                                            Last Updated:
                                        </td>
                                        <td align="left"  ><%--style="padding-right:20px"--%>
                                            <%--<asp:TextBox ClientIDMode="AutoID" ID="tbVZPPLastUpdated" CssClass="comPlanAmntFld" runat="server" Enabled="false" ></asp:TextBox>--%>
                                            <label ID="lblVZPPLastUpdated" runat="server"></label>
                                        </td>

                                        <td align="right" class="comPlanHeader" ><%--style="padding-left:40px"--%>
                                            Balance:
                                        </td>
                                        <td align="left" >
                                            <%--<asp:TextBox ClientIDMode="AutoID" ID="tbVZPPBalace" CssClass="comPlanAmntFld" runat="server" Enabled="false" ></asp:TextBox>--%>
                                            <label ID="lblVZPPBalace" runat="server" ></label>
                                        </td>
                                        
                                        
                                        
                                    </tr>
                                </table>
                                
                                
                            </fieldset>
                       <%-- </div>--%>
                    </div>
                </ContentTemplate>
                    
            </asp:TabPanel>
           <%-- <asp:TabPanel ID="verMdnTab" runat="server" HeaderText="Ver MDN">
                <ContentTemplate>
                    <div>
                        <div style="width: 100%; overflow: auto; min-height: 0%; margin-bottom: 5px;">
                            <asp:GridView ID="verMdnGV" DataSourceID="verMdnSqlDS" ShowHeaderWhenEmpty="true"
                                HeaderStyle-Wrap="false" RowStyle-Wrap="false" runat="server" Font-Size="11px"
                                CssClass="verMdnGV" Width="100%" AllowPaging="true" PageSize="20" AllowSorting="True" >
                            </asp:GridView>
                            <asp:SqlDataSource ID="verMdnSqlDS" ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                                runat="server"></asp:SqlDataSource>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>--%>
            <%--<asp:TabPanel ID="esnOrders" runat="server" HeaderText="ESN Status">
                <ContentTemplate>
                    <div id="viewESNOrdersDiv" style="width: 100%; overflow: auto; min-height: 0%; margin-bottom: 5px;">--%>
            <%--commented out<div id="divSearchEsnOrders">
                                    <span>
                                        <label>
                                            Name:</label>
                                        <asp:TextBox ID="txtName" runat="server"></asp:TextBox>
                                    </span><span>
                                        <asp:Button ID="btnGo" runat="server" Text="Go" />
                                    </span>
                                </div>--%>
            <%--<div id="divSearchEsnOrdersGV">
                            <asp:GridView ID="esnOrdersGV" runat="server" AllowPaging="True" PageSize="20" ShowHeaderWhenEmpty="true"
                                HeaderStyle-Wrap="false" RowStyle-Wrap="false" AllowSorting="True" AutoGenerateColumns="False"
                                Font-Size="11px" RowStyle-CssClass="highlightedGvRow" DataSourceID="esnOrdersDataSource"
                                CssClass="verMdnGV" Width="100%">
                                <Columns>
                                    <asp:BoundField DataField="ESN" HeaderText="ESN" SortExpression="ESN" />
                                    <asp:BoundField DataField="isValidESN" HeaderText="Valid ESN" SortExpression="isValidESN" />
                                    <asp:BoundField DataField="CellNum" HeaderText="Cell" SortExpression="CellNum" />
                                    <asp:BoundField DataField="First" HeaderText="First" SortExpression="First" />
                                    <asp:BoundField DataField="Last" HeaderText="Last" SortExpression="Last" />
                                    <asp:BoundField DataField="Status" HeaderText="Status" SortExpression="Status" />
                                    <asp:BoundField DataField="Balance" HeaderText="Balance" SortExpression="Balance" />
                                    <asp:BoundField DataField="ExpDate" HeaderText="Plan Exp" SortExpression="ExpDate" />
                                    <asp:BoundField DataField="PlanExpDate" HeaderText="Plan Exp" SortExpression="PlanExpDate" />
                                    <asp:BoundField DataField="Stacked" HeaderText="Stacked" SortExpression="Stacked" />
                                    <asp:BoundField DataField="Vendor Plan" HeaderText="Vendor Plan" SortExpression="Vendor Plan" />
                                    <asp:BoundField DataField="ESN Checked" HeaderText="ESN Checked" SortExpression="ESN Checked" />
                                </Columns>
                            </asp:GridView>
                            <asp:SqlDataSource ID="esnOrdersDataSource" runat="server">--%>
            <%--commented out <SelectParameters>
                                            <asp:ControlParameter ControlID="txtName" Name="AccountName" DefaultValue="%"
                                                Type="String" />
                                        </SelectParameters>--%>
            <%--</asp:SqlDataSource>
                        </div>
                    </div>
                    <br />
                    <div style="margin-bottom: 20px;">
                        <div style="float: right;">
                            <div style="display: inline;">
                                <asp:Button ID="btnDownload" runat="server" Text="Download" /></div>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>--%>
        </asp:TabContainer>
    </div>
</asp:Content>
