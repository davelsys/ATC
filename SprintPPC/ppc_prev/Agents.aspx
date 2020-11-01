<%@ Page Title="Agents" ClientIDMode="Static" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Agents.aspx.vb" Inherits="Agents" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">

    <style type="text/css">
        
        #agentAmountsDiv span, #agentAmountsDiv label, #agentAmountsDiv select {
            margin-right: 5px;
            float: left;
        }
        
        .verticalHalf {
            width: 49%;
            margin-bottom: 5px;
            height: 500px;
        }
        
        .verticalHalf .chargeDiv {
            height: 50%;
        }
        
        .verticalHalf .detailsDiv {
            margin-top: 10px;
            height: 40%;
            overflow: auto;
        }
        
        #agentCreditTransGV {
            white-space: nowrap;
            font-size: 11px;
            width: 100%;
            padding: 1px;
        }
        
        #innerCCDiv .row {
            padding-left: 20px;
            height: 30px;
        }
        
        #innerCCDiv .row label {
           width: 90px;
           display: inline-block;
           font-weight: bold;
           font-size: 11px;
        }
        
        #innerCCDiv .row .cell {
            float: left;
            width: 250px;
        }
        
        #updateBillingInfoPnl .row, #adminCreditAgent .row {
            padding-left: 15px;
            height: 30px;
        }
        
        #updateBillingInfoPnl .row label, #adminCreditAgent .row label {
           width: 80px;
           display: inline-block;
           font-weight: bold;
           font-size: 11px;
        }
        
        #updateBillingInfoPnl .row .cell, #adminCreditAgent .row .cell {
            float: left;
            width: 49%;
        }
        
        #updateBillingInfoPnl .txtFldWidth {
            width: 100px;
        }
        
    </style>

    <script type="text/javascript">

        window.onload = function () {

            setTabPnlsMinHeight( 200 );

            setupCCExpDropdown();

            document.getElementById( 'agentPaymentBtn' ).onclick = validateCCVals;

            document.getElementById( 'triggerUpdateBtn' ).onclick = validateBillVals;

            attachAdminPayBtnHandler();

            setUpdateBillingInfoMsg();

        }

        var validateCCVals = function () {
            // Validate cc num and card code.
            var ccPtrn = /^[0-9]{15}(?:[0-9]{1})?$/,
                starredPtrn = /^[*]{12}[0-9]{3}(?:[0-9]{1})?$/,
                cCodePtrn = /^[0-9]{3}(?:[0-9]{1})?$/,
                num = document.getElementById( 'creditCardNumber' ).value,
                code = document.getElementById( 'creditCardCode' ).value,
                amnt = document.getElementById( 'agentPayAmount' ).value;
                
            var validNum = ccPtrn.test( num ) || starredPtrn.test( num );
            var validCode = ( code.length > 0 ) ? cCodePtrn.test( code ) : true;
            var validAmnt = validateAmount( amnt );

            if ( !validNum ) { document.getElementById( 'agntPayError' ).innerText = 'Invalid Credit Card Number.'; return false; }
            if ( !validCode ) { document.getElementById( 'agntPayError' ).innerText = 'Invalid Credit Card Code.'; return false; }
            if ( !validAmnt ) { document.getElementById( 'agntPayError' ).innerText = 'Invalid Amount.'; return false; }

            return true;
        }

        var validateBillVals = function () {
            var lname = num = document.getElementById( 'billLnameFld' ).value
                , phone = document.getElementById( 'billPhoneFld' ).value;

            var validLName = ( lname.length > 0 );
            var validPhone = ( phone.length >= 10 );

            if ( !validLName ) { document.getElementById( 'updateBillingErrorMsg' ).innerText = 'Last name required.'; return false; }
            if ( !validPhone ) { document.getElementById( 'updateBillingErrorMsg' ).innerText = 'Invalid phone number.'; return false; }

            return true;
        }

        var validateAmount = function ( val ) {
            var dollarPtrn = /^[0-9]+(\.[0-9][0-9])?$/;
            return dollarPtrn.test( val.replace( /\$/, '' ).replace( /,/g, '' ) );
        }

        var attachAdminPayBtnHandler = function () {
            var adminPayBtn = document.getElementById( 'adminAgentPayBtn' );
            if ( adminPayBtn == null ) { return; }
            adminPayBtn.onclick = function () {
                var valid = validateAmount( document.getElementById( 'adminAgentPayFld' ).value );
                if ( !valid ) {
                    document.getElementById( 'adminAgentPayError' ).innerText = 'Invalid amount.';
                }
                return valid;
            }
        }

        var setUpdateBillingInfoMsg = function () {
            // This variable set in the code behind
            if( typeof testBillingInfo != 'undefined' && testBillingInfo == false ) { return; }

            var hasBillingInfo = document.getElementById( 'hasBillingInfo' ).value;
            if ( hasBillingInfo == 'false' ) {
                document.getElementById( 'agentPaymentBtn' ).disabled = true;
                document.getElementById( 'agntPayError' ).innerText = 'Please update billing information.';
                setTimeout( function () {
                    $find( "creditAgentTabs" ).set_activeTabIndex( 0 );
                }, 0 );
            } else {
                document.getElementById( 'agentPaymentBtn' ).disabled = false;
                setTimeout( function () {
                    $find( "creditAgentTabs" ).set_activeTabIndex( 1 );
                }, 0 );
            }
        }

    </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div style="height: 40px;">

        <div style="padding: 2px; font-size: 13px;" id="agentAmountsDiv">
            <label>Agents: </label>
            <asp:DropDownList ID="salesRepDropdown" runat="server" AutoPostBack="true">
            </asp:DropDownList>

            <span>&nbsp;</span>

            <label style="font-weight: bold;">Credit Limit:</label>
            <asp:Label ID="creditLimitLbl" runat="server"></asp:Label>

            <span id="commissionAmountSpan" runat="server">
                <span>&nbsp;+</span>
        
                <label style="font-weight: bold;">Commissions: </label>
                <asp:Label ID="totalComLbl" runat="server"></asp:Label>
            </span>

            <span>&nbsp;-</span>
        
            <label style="font-weight: bold;">Used: </label>
            <asp:Label ID="creditUsedLbl" runat="server"></asp:Label>
        
            <span>&nbsp;=</span>
        
            <label style="font-weight: bold;">Available: </label>
            <asp:Label ID="creditAvailableLbl" runat="server"></asp:Label>

            <span style="float: right;" runat="server">
                <label style="font-weight: bold;">Balance: </label>
                <asp:Label ID="balanceLbl" runat="server"></asp:Label>
            </span>

        </div>

    </div>
    
    <div>

        <div class="verticalHalf" style="float: left;">

            <asp:TabContainer ID="agentLeftTabs" runat="server">
                <asp:TabPanel ID="agentTransTab" runat="server" HeaderText="Transactions">
                    <ContentTemplate>
                        <asp:GridView ID="agentAuthtransGridview" runat="server" AutoGenerateColumns="True" 
                                        AllowPaging="true" PageSize="20" 
                                        EmptyDataText="This agent hasn't charged any customers."
                                        Width="100%" Font-Size="11px" >
                        </asp:GridView>
            
                        <div style="height: 10px;"></div>

                        <div id="gvHasRowsDiv" runat="server" style="width: 100%; height: 25px;">
                            <div style="width: 15%; float: left;">
                                <input id="downloadAgentsBtn" runat="server" type="button" value="Download" />
                            </div>
                            <div style="float: right; text-align: right;">
                                <label>Total Charges:</label>
                                <asp:Label ID="totalChargesLbl" runat="server" Font-Size="11px"></asp:Label>
                            </div>
                        </div>

                    </ContentTemplate>
                </asp:TabPanel>

                <asp:TabPanel ID="commissionsGVTab" runat="server" HeaderText="Commissions">
                    <ContentTemplate>
                        <div style="overflow: auto; min-height: 0%;">
                            <asp:GridView ID="agentCommissionsGV" runat="server"
                                AllowPaging="true" PageSize="20"
                                Font-Size="11px" Width="100%"
                                HeaderStyle-Wrap="false" RowStyle-Wrap="false"
                                DataSourceID="agentCommissionsDataSource">
                            </asp:GridView>

                            <asp:SqlDataSource 
                                ID="agentCommissionsDataSource"
                                runat="server"
                                ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                                ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>">
                                <SelectParameters>
                                    <asp:ControlParameter ControlID="salesRepDropdown" Name="Agent" Type="String" />
                                </SelectParameters>
                            </asp:SqlDataSource>
                        </div>

                        <div style="height: 10px;"></div>

                        <div id="commissionsBtnDiv" runat="server" style="width: 100%; height: 25px;">
                            <div style="width: 15%; float: left;">
                                <input id="downloadCommissionsBtn" runat="server" type="button" value="Download" />
                            </div>
                            <div style="float: right; text-align: right;">
                                <label>Total Commissions:</label>
                                <asp:Label ID="totalComTabLbl" runat="server" Font-Size="11px"></asp:Label>
                            </div>
                        </div>

                    </ContentTemplate>
                </asp:TabPanel>
            </asp:TabContainer>

        </div>
        
        <div class="verticalHalf" id="agentMakePaymentDiv" style="float: right;">
            
            <div id="chargeDiv" class="chargeDiv">

                <asp:TabContainer ID="creditAgentTabs" runat="server">

                    <asp:TabPanel runat="server" ID="billingInfoTab" HeaderText="Billing Information">
                        <ContentTemplate>
                            <div id="updateBillingInfoPnl">
                                <div style="height: 15px;"></div>
                                <div class="row">
                                    <div class="cell">
                                        <label>First Name:</label>
                                        <asp:TextBox ID="billFnameFld" runat="server" CssClass="txtFldWidth"></asp:TextBox>
                                    </div>
                                    <div class="cell">
                                        <label>Last Name:</label>
                                        <asp:TextBox ID="billLnameFld" runat="server" CssClass="txtFldWidth"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="row">
                                    <div>
                                        <label>Email:</label>
                                        <asp:TextBox ID="billEmailFld" runat="server" Width="305px"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="row">
                                    <div>
                                        <label>Address:</label>
                                        <asp:TextBox ID="billAddressFld" runat="server" Width="305px"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="row">
                                    <div class="cell">
                                        <label>City:</label>
                                        <asp:TextBox ID="billCityFld" runat="server" CssClass="txtFldWidth"></asp:TextBox>
                                    </div>
                                    <div class="cell">
                                        <label>State:</label>
                                        <asp:TextBox ID="billStateFld" runat="server" CssClass="txtFldWidth"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="row">
                                    <div class="cell">
                                        <label>Zip:</label>
                                        <asp:TextBox ID="billZipFld" runat="server" CssClass="txtFldWidth"></asp:TextBox>
                                    </div>
                                    <div class="cell">
                                        <label>Phone:</label>
                                        <asp:TextBox ID="billPhoneFld" runat="server" CssClass="txtFldWidth"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="row">
                                    <span id="updateBillingErrorMsg" runat="server" style="color: brown;"></span>
                                    <asp:Button ID="triggerUpdateBtn" runat="server" Text="Update" 
                                        style="float: right; margin-right: 20px;" />
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:TabPanel>

                    <asp:TabPanel runat="server" ID="ccChargeTab" HeaderText="Credit Card">
                        <ContentTemplate>
                            <div id="innerCCDiv">
                                <asp:HiddenField ID="hasBillingInfo" runat="server" />
                                <div style="height: 15px;"></div>
                                <div class="row">
                                    <label>Card Number:</label>
                                    <asp:TextBox ID="creditCardNumber" runat="server" Width="175px"></asp:TextBox>
                                </div>
                                <div class="row">
                                    <label>Exp Date: </label>
                                    <asp:DropDownList ID="creditCardExpirationMonth" runat="server">
                                        <asp:ListItem Value="01">01</asp:ListItem>
                                        <asp:ListItem Value="02">02</asp:ListItem>
                                        <asp:ListItem Value="03">03</asp:ListItem>
                                        <asp:ListItem Value="04">04</asp:ListItem>
                                        <asp:ListItem Value="05">05</asp:ListItem>
                                        <asp:ListItem Value="06">06</asp:ListItem>
                                        <asp:ListItem Value="07">07</asp:ListItem>
                                        <asp:ListItem Value="08">08</asp:ListItem>
                                        <asp:ListItem Value="09">09</asp:ListItem>
                                        <asp:ListItem Value="10">10</asp:ListItem>
                                        <asp:ListItem Value="11">11</asp:ListItem>
                                        <asp:ListItem Value="12">12</asp:ListItem>
                                    </asp:DropDownList>

                                    <label style="width: 10px; font-weight: normal;">/</label>

                                    <asp:DropDownList ID="creditCardExpirationYear" runat="server"  onchange="setupCCExpDropdown();">
                                    </asp:DropDownList>
                                </div>
                                <div class="row">
                                    <label>Card Code:</label>
                                    <asp:TextBox ID="creditCardCode" Width="90px" runat="server"></asp:TextBox>
                                </div>
                                <div class="row">
                                    <label>Amount:</label>
                                    <asp:TextBox ID="agentPayAmount" Width="90px" runat="server"></asp:TextBox>
                                </div>
                                <div class="row">
                                    <div style="float: left; width: 70%; height: 100%;">
                                        <span id="agntPayError" runat="server" style="color: brown;"></span>
                                    </div>
                                    <div style="float: right; width: 25%; height: 100%; text-align: right;">
                                        <asp:Button ID="agentPaymentBtn" runat="server" Text="Pay" />
                                    </div>
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:TabPanel>

                    <asp:TabPanel runat="server" ID="adminCreditAgent" HeaderText="Admin Payments">
                        <ContentTemplate>
                            <div>
                                <div style="height: 15px;"></div>
                                <div class="row">
                                    <div class="cell">
                                        <label style="width: auto;">Amount:</label>
                                        <asp:TextBox ID="adminAgentPayFld" Width="100" runat="server"></asp:TextBox>
                                    </div>
                                    <div class="cell"">
                                        <asp:Button ID="adminAgentPayBtn" style="float: right;" runat="server" Text="Pay" />
                                    </div>
                                </div>
                                <div class="row">
                                    <span id="adminAgentPayError" runat="server" style="color: brown;"></span>
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:TabPanel>

                </asp:TabContainer>

            </div>

            <div id="detailsDiv" class="detailsDiv">
                <div id="innerDetailsDiv">
                    <asp:GridView ID="agentCreditTransGV" runat="server" AutoGenerateColumns="false">
                        <Columns>
                            <asp:BoundField DataField="agent" HeaderText="Agent" />
                            <asp:BoundField DataField="user" HeaderText="Credit By" />
                            <asp:BoundField DataField="paydate" HeaderText="Pay Date" />
                            <asp:BoundField DataField="total" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Amount" />
                            <asp:BoundField DataField="authtransid" HeaderText="Auth Id" />
                            <asp:BoundField DataField="authmessage" HeaderText="Auth Message" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

        </div>

    </div>

</asp:Content>