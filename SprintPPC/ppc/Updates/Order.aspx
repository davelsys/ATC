<%@ Page Title="Orders" ClientIDMode="Static" MasterPageFile="~/Site.master"
    Language="VB" AutoEventWireup="false" CodeFile="Order.aspx.vb" Inherits="Order" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">

    <style type="text/css">
        
        /*  newNumberDiv   */
        .newNumberDiv {
            padding: 10px 2px 1px 2px;
            width: 100%;
        }
        
        .newNumberRow {
            height: 25px;
            width: 95%;
            margin: 5px 0px 5px 0px;
        }
        
        .lblDiv {
            float: left;
            width: 33%;
            display: inline-block;
            vertical-align: middle;
        }
        
        .fldDiv {
            float: left;
            display: inline-block;
            width: 40%;
        }
        
        .fldDiv input[type=text] {
            width: 85%;
        }
        
        .createPhoneNumberTbl {
            float: left;
        }
        
        .createPhoneNumberTbl td {
            width: 50px;    
        }
        
        .createPhoneNumberTbl label {
            text-align: center;
            display: block;
            width: 100%;
        }
        
        .createPhoneNumberTbl input[type=text] {
            width: 85%;
        }
        
        .phoneFieldsHeight {
            height: 35px;
        }
        
        .leftPhoneFields {
            width: 35%;
            float: left;
        }
        
        .rightPhoneFields {
            width: 64%;
            float: right;
        }
        
        .customerTabBtns input[type=submit] {
            margin: 2px;
            float: right;
        }
        
        .verReqBtns input[type=submit] {
            margin: 2px;
            float: left;
        }
       .btnCalendar
{
    BACKGROUND-POSITION: center 50%;
    background-color: Silver;
    BACKGROUND-ATTACHMENT: fixed;
    /*BACKGROUND-IMAGE: url(Images/smallcalendar.gif);*/
    BACKGROUND: transparent url(Images/smallcalendar.GIF) center center;
    BACKGROUND-REPEAT: no-repeat;
    WIDTH: 18px;
    HEIGHT: 19px;
    padding: 1px 1px 1px 1px;
}
        
    </style>
     <meta http-equiv="Page-Exit" content="progid:DXImageTransform.Microsoft.GradientWipe(Duration=0,Transition=5)" />
<style type="text/css">@import url(App_Themes/Aqua/theme.css);</style>
<script type="text/javascript" src="calendar.js"></script>
<script type="text/javascript" src="calendar-en.js"></script>
<script type="text/javascript" src="calendar-setup.js"></script>
<script language="javascript" type="text/javascript" src="formatScripts.js"></script>



    
    <script type="text/javascript">

        window.onload = function () {

            newNumberForm.init();
            planDropdowns.toggle();

            togglePaymentMethodForm();
            setRenewTypeRadioState();
            setupCCExpDropdown();
            setTabPnlsMinHeight( 300 );

            controlFreezeHeader();

            refreshVerStatusMsg.start();

            adminTasks.attachHandlers();

            // Prevent double clicking pay button
            document.getElementById( 'payButton' ).onclick = function () {
                var btn = this;
                setTimeout( function () {
                    btn.disabled = true;
                }, 0 );
            }

        }

        
        function toggleCloseBtn() {
            // Note: the css is also set in the server code on postback
            var activeTabsIndex = $find( "TabContainer1" ).get_activeTabIndex();
            var closeBtn = document.getElementById( "closeOrderButton" );
            if ( closeBtn ) {
                closeBtn.style.display = ( activeTabsIndex == 0 ) ? "inline" : "none";
            }
        }

        function togglePaymentMethodForm() {
            var radioButtonList = document.getElementById( "selectPaymentMethod" );
            var radioButtons = radioButtonList.getElementsByTagName( "input" );

            if ( radioButtons[0].checked ) {
                document.getElementById( "creditLimitDiv" ).style.display = "none";
                document.getElementById( "crediCardDiv" ).style.display = "block";
            } else if ( radioButtons[1].checked ) {
                document.getElementById( "crediCardDiv" ).style.display = "none";
                document.getElementById( "creditLimitDiv" ).style.display = "block";
            }

        }

        function SetVendorPin() {
            var cellNumber = document.getElementById( "cell_number" ).value;
            if ( cellNumber.length == 10 ) {
                var startIndex = cellNumber.length - 4;
                document.getElementById( "vendorPin" ).innerText = cellNumber.substring( startIndex );
            }
        }

        var agentCredit = {
            getLimit: function () {
                var agent = document.getElementById( "salesRepDropdown" ).value;

                var args = "GetAgentCreditLimit:" + agent;
                UseCallBack( args, "GetAgentCreditLimit" );
            },
            setLimitLabels: function ( xml ) {

                var creditLimit = parseFloat( xml.substring( xml.indexOf( "<CreditLimit>" ) + 13, xml.indexOf( "</CreditLimit>" ) ) );

                var creditUsed = parseFloat( xml.substring( xml.indexOf( "<creditUsed>" ) + 12, xml.indexOf( "</creditUsed>" ) ) );

                if ( isNaN( creditLimit ) ) {
                    creditLimit = 0;
                }

                if ( isNaN( creditUsed ) ) {
                    creditUsed = 0;
                }

                var available = creditLimit - creditUsed;

                document.getElementById( "creditLimitLbl" ).innerText = FormatMoney( creditLimit );
                document.getElementById( "creditUsedLbl" ).innerText = FormatMoney( creditUsed );
                document.getElementById( "creditAvailableLbl" ).innerText = FormatMoney( available );

            }
        }

        function SetIntlLbl( dropdown ) {

            var cost = dropdown.value;

            if ( cost.length === 0 ) {
                document.getElementById( "intlBalanceDropdownCostLbl" ).innerText = cost;
            } else {
                document.getElementById( "intlBalanceDropdownCostLbl" ).innerText = FormatMoney( cost );
            }

            calculateTotal();
        }

        var planCostLbls = {
            getCost: function (dropdown) {
              
                var selectedText = dropdown.options[dropdown.selectedIndex].text;
                
                var planId = dropdown.value;
                if (planId == 0) {
                    nextElementNode(dropdown.parentNode).innerText = "";
                    calculateTotal();
                    return false;
                }

                var args = "SetCostLblFromServer:" + planId + ":" + selectedText;
                UseCallBack(args, "SetCostLblFromServer");
            },
            setCostLbl: function (xml) {

                var planRef = xml.substring(xml.indexOf("<planref>") + 9, xml.indexOf("</planref>"));

                if (planRef == "Monthly") {
                    
                    var monthlyCost = xml.substring(xml.indexOf("<monthly_cost>") + 14, xml.indexOf("</monthly_cost>"));
                    document.getElementById("monthlyPlanDropdownCostLbl").innerText = FormatMoney(monthlyCost);
                }

                if (planRef == "Pay As You Go") {
                    var planCost = xml.substring(xml.indexOf("<plan_cost>") + 11, xml.indexOf("</plan_cost>"));
                    document.getElementById("cashBalanceDropdownCostLbl").innerText = FormatMoney(planCost);
                }

                calculateTotal();
            }
        }

        function clearIncompleteCCNum() {

            var ccNum = document.getElementById( "creditCardNumber" ).value;

            if ( ccNum.indexOf( "*" ) > -1 ) {
                document.getElementById( "creditCardNumber" ).value = "";
            }

        }

        //for all talk, when monthly plan is selected, disable cash plan dropdown and vice verca
        //***** IMPORTANT: both cannot be enabled at same time, otherwise processes may get messed up such as the UpdateAuthTrans Sub in ATCPin.vb file, and AssignPinBG sub in Module1.vb in ATCBackground project because in both cases, it assumes that month-plan_id and cash_plan_id will never both have a value greater than 0 in authtrans table 
        //SG 07/13/16 removing all talk 
        //function ToggleEnabled() {
        //    if (document.getElementById('carrierName').value == 'All Talk') {
        //                    if (document.getElementById("monthlyPlanDropdown").value > 0) {
        //            document.getElementById("cashBalanceDropdown").setAttribute("disabled", "disabled");
        //            document.getElementById("monthlyPlanDropdown").removeAttribute("disabled");
        //        }
        //        else if (document.getElementById("cashBalanceDropdown").value > 0) {
        //            document.getElementById("monthlyPlanDropdown").setAttribute("disabled", "disabled");
        //            document.getElementById("cashBalanceDropdown").removeAttribute("disabled");
        //        }
        //        else {
        //            document.getElementById("monthlyPlanDropdown").removeAttribute("disabled");
        //            document.getElementById("cashBalanceDropdown").removeAttribute("disabled");
        //        }
        //    }
        //}
        function TogglePayButton() {

            var payButton = document.getElementById( "payButton" );

            var radioButtonList = document.getElementById( "selectPaymentMethod" );
            var radioButtons = radioButtonList.getElementsByTagName( "input" );

            if ( radioButtons[0].checked ) {      // Credit card selected

                var ccNumber = document.getElementById( "creditCardNumber" ).value;

                var starredPattern = new RegExp( "[*]{10}[0-9]{4}$" );

                var regularPattern = new RegExp( "[0-9]{12}$" );

                if (!starredPattern.test(ccNumber) && !regularPattern.test(ccNumber) && document.getElementById("carrierName").value !== "Concord"
                    && document.getElementById("carrierName").value !== "Telco") {
                    
                    payButton.disabled = true;
                    return;
                } 
            }

            var monthlyValue = document.getElementById( "monthlyPlanDropdown" ).value;
            var cashValue = document.getElementById( "cashBalanceDropdown" ).value;
            var intlValue = parseFloat( StripMoney( document.getElementById( "intlBalanceDropdown" ).value ) );
            var itemsValue = GetItemCost();

            var miscCost = document.getElementById( "miscCostFld" ).value;
            if ( !validateMiscValues() ) {
                payButton.disabled = true;
                return false;
            } else if ( miscCost.length <= 0 ) {
                miscCost = 0;
            } else {
                miscCost = parseFloat( StripMoney( miscCost ) );
            }

            if ( monthlyValue > 0 ) {
                payButton.disabled = false;
            } else if ( cashValue > 0 ) {
                payButton.disabled = false;
            } else if ( intlValue > 0 ) {
                payButton.disabled = false;
            } else if ( itemsValue > 0 ) {
                payButton.disabled = false;
            } else if ( miscCost > 0 ) {
                payButton.disabled = false;
            } else {
                payButton.disabled = true;
            }

        }

        function calculateTotal() {

            var monthlyCost = getCost( document.getElementById( "monthlyPlanDropdownCostLbl" ).innerText );
            var cashCost = getCost( document.getElementById( "cashBalanceDropdownCostLbl" ).innerText );
            var intlCost = getCost( document.getElementById( "intlBalanceDropdownCostLbl" ).innerText );
            var miscCost = getCost( document.getElementById( "miscCostFld" ).value );

            var itemCost = GetItemCost();

            var total = 0 + monthlyCost + cashCost + intlCost + miscCost + itemCost;
            document.getElementById( "amntDue" ).innerText = FormatMoney( total );

        }

        function getCost(cost) {
            //alert(cost)
            if ( cost.length <= 0 || !isValidMoneyAmnt( cost ) ) {
                return 0;
            } else {
                return parseFloat( StripMoney( cost ) );
            }
        }

        function GetItemCost() {
            var cost = 0;
            var itemList = document.getElementById( "itemCheckList" ).getElementsByTagName( "input" );

            for ( var i = 0; i < itemList.length; i++ ) {
                var chkBox = itemList[i];

                if ( chkBox.getAttribute( 'type' ) === 'checkbox' && chkBox.checked ) {
                    // The cost is in a hidden input element which is contained in the itemList array
                    // in the next array element.
                    cost += parseFloat( itemList[i + 1].value );
                }

            }

            return cost;
        }

        function setItemCostLbl( checkBox, itemCost ) {
            if ( checkBox.checked ) {
                nextElementNode( checkBox.parentNode ).innerText = FormatMoney( itemCost );
            } else {
                nextElementNode( checkBox.parentNode ).innerText = "";
            }
        }

        function showSerialConfirm() {
            document.getElementById("confirmSerialSpan").style.display = "block";
            document.getElementById("confirmSerialNumber").focus(); //DL
        }

        function showCellNumberConfirm() {
            document.getElementById( "confirmCellNumberSpan" ).style.display = "block";
        }

        function continueSaveOrder() {
            
            // Validate customer info.
            if (!validateCustomerInfo()) {
                return false;
            }

            // Require serial number.
            if (!requireSerialNumber()) {
                return false;
            }

            // Validate serial number.
            //sg 11/08/16 removed to test new concord lines
            if (!ValidateSerialNumber()) {
                return false;
            }

            // carrierDropdown is defined in Order.aspx
            if ( newNumberForm.getCarrier() == 'page plus' ) {
                // Require cell number
                if ( !requireCellNumber() ) {
                    return false;
                }

                // Validate the cell number.
                if ( !validateCellNumber() ) {
                    return false;
                }

            }


        }

        function validateCustomerInfo() {

            var lname = document.getElementById( "lname" ).value;
            var fname = document.getElementById("fname").value;
            var phone = document.getElementById( "phone" ).value;


            if ( lname.length <= 0 ) {
                setOrderErrorMsg( "Last name is a required field." );
                return false;
            }
            if (fname.length <= 0) {
                setOrderErrorMsg("First name is a required field.");
                return false;
            }
            
            if ( phone.length <= 0 ) {
                setOrderErrorMsg( "Customer phone number is a required field." );
                return false;
            }

            return true;

        }

        function getPlanInfoFromSerial() {

            var serialNumber = document.getElementById( "serialNumber" ).value;
            var confirmSerial = document.getElementById( "confirmSerialNumber" ).value;

            if ( document.getElementById( "confirmSerialSpan" ).style.display === 'none' ) {
                return false;
            } else if (serialNumber.length <= 0 || confirmSerial.length <= 0) {
                
                document.getElementById( "esn" ).innerText = "";
                document.getElementById( "hiddenESN" ).value = "";
                return false;
            } else if ( serialNumber != confirmSerial ) {
                setOrderErrorMsg( "The serial numbers don't match." );
                return false;
            } else {
               
                setOrderErrorMsg(''); //setOrderErrorMsg('');
                var args = "getPlanInfoFromSerial:" + serialNumber;
                UseCallBack( args, "getPlanInfoFromSerial" );
            }

        }

        function populatePlanEsnAndPins( xml ) {

            if ( xml == "invalid" ) {
                document.getElementById( "serialNumber" ).value = "";
                setOrderErrorMsg( "Invalid serial number." );
                return false;
            }

            if ( xml == "nonunique" ) {
                document.getElementById( "serialNumber" ).value = "";
                setOrderErrorMsg( "Serial number already assigned." );
                return false;
            }

            var userLevel = parseInt( xml.substring( xml.indexOf( "<UserLevel>" ) + 11, xml.indexOf( "</UserLevel>" ) ) );

            var esn = xml.substring( xml.indexOf( "<ESN>" ) + 5, xml.indexOf( "</ESN>" ) );
            if ( userLevel == 1 ) {
                document.getElementById( "esn" ).innerText = esn;
            } else {
                document.getElementById( "esn" ).innerText = esn.substring( 0, 1 ) + "************";
            }
            document.getElementById( "hiddenESN" ).value = esn;

            var hiddenIntlPin = document.getElementById( "hiddenIntlPin" );
            if ( hiddenIntlPin.value.length <= 0 ) {
                var intlPin = xml.substring( xml.indexOf( "<International>" ) + 15, xml.indexOf( "</International>" ) );
                if ( userLevel == 1 ) {
                    document.getElementById( "intlPin" ).innerText = intlPin;
                } else {
                    document.getElementById( "intlPin" ).innerText = intlPin.substring( 0, 1 ) + "**********";
                }
                hiddenIntlPin.value = intlPin;
            }

            document.getElementById( "customerPin" ).value = xml.substring( xml.indexOf( "<CustomerPin>" ) + 13, xml.indexOf( "</CustomerPin>" ) );

            // Defined in Order.aspx
            if (newNumberForm.getCarrier() == 'verizon' || newNumberForm.getCarrier() == 'concord' || newNumberForm.getCarrier() == 'telco') {
                var originalSerial = document.getElementById( 'serailNumLbl' ).innerText;
                var newSerial = document.getElementById( 'serialNumber' ).value;
                // ESNs are mapped to serial numbers. See if ESN has changed from serial numbers.
                // alert("Original = " + originalSerial + ":" + newSerial);
                if (!newNumberForm.isNewOrder() && originalSerial != newSerial) {
                    
                    newNumberForm.initUploadESNReq();
                }

            }

        }

        function ValidateSerialNumber() {

            // First validate the serial number
            if ( document.getElementById( "confirmSerialSpan" ).style.display != 'none' ) {
                var serialNumber = document.getElementById( "serialNumber" ).value;
                var confirmSerial = document.getElementById( "confirmSerialNumber" ).value;

                if ( serialNumber.length <= 0 && confirmSerial.length > 0 ) {
                    setOrderErrorMsg("Please fill in the serial number.");
                    return false;
                }

                if ( serialNumber.length > 0 && confirmSerial.length <= 0 ) {
                    setOrderErrorMsg("Please confirm the serial number.");
                    return false;
                }

                if ( serialNumber != confirmSerial ) {
                    setOrderErrorMsg("The serial numbers don't match.");
                    return false;
                }

                if ( serialNumber.length <= 0 && confirmSerial.length <= 0 ) {

                    document.getElementById( "esn" ).innerText = "";
                    document.getElementById("hiddenESN").value = "";

                    

                }

                if ( serialNumber.length > 0 && confirmSerial.length > 0 ) {
                    if ( document.getElementById( "hiddenIntlPin" ).value.length <= 0 ||
                   document.getElementById("hiddenESN").value.length <= 0) {
                        //alert(document.getElementById("hiddenIntlPin").value + document.getElementById("hiddenESN").value)
                        // Didn't display message here because in this case the ajax call
                        // return, will set the error message. 
                        return false;
                    }
                }

            }

            return true;

        }

        function requireSerialNumber() {
            var serial = document.getElementById( "serialNumber" ).value;
            if ( serial.length <= 0 ) {
                setOrderErrorMsg( "Serial number required." );
                return false;
            } else {
                return true;
            }
        }

        function validateCellNumber() {

            var cellNumber = document.getElementById( "cell_number" ).value;
            var confirmCellNumber = document.getElementById( "confirmCellNumber" ).value;

            // First test for proper length
            if ( cellNumber.length > 0 ) {
                if ( cellNumber.length < 10 || cellNumber.length > 10 ) {
                    setOrderErrorMsg( "Cell number must be exactly 10 characters long." );
                    return false;
                }
            }

            if ( document.getElementById( "confirmCellNumberSpan" ).style.display != 'none' ) {

                if ( cellNumber.length <= 0 && confirmCellNumber.length > 0 ) {
                    setOrderErrorMsg( "Please fill in the cell number." );
                    return false;
                }

                if ( cellNumber.length > 0 && confirmCellNumber.length <= 0 ) {
                    setOrderErrorMsg( "Please confirm the cell number." );
                    return false;
                }

                if ( cellNumber != confirmCellNumber ) {
                    setOrderErrorMsg( "The cell numbers don't match." );
                    return false;
                }

            }

            return true;

        }

        function isCellNumberUnique() {

            setOrderErrorMsg( '' );

            var cellNumber = document.getElementById( "cell_number" ).value;

            if ( cellNumber.length != 10 ) {
                return false;
            }

            var args = "isCellNumberUnique:" + cellNumber;
            UseCallBack( args, "isCellNumberUnique" );
        }

        function setCellNumNonuniqueMsg( str ) {

            if ( str == 'nonunique' ) {
                document.getElementById( "cell_number" ).value = "";
                setOrderErrorMsg( "Cell number already assigned." );
            }
        }

        function requireCellNumber() {
            var cellNum = document.getElementById( "cell_number" ).value;

            if ( cellNum.length <= 0 ) {
                setOrderErrorMsg( "Cell phone is a required field." );
                return false;
            }

            return true;

        }

        function setOrderErrorMsg( msg ) {
            var span = document.getElementById( "orderErrorMessage" );
            span.style.color = 'red';
            span.innerText = msg;
        }

        function setRenewChkBxState() {

            var mIndex = document.getElementById("renewalMonthlyDropdown").selectedIndex;
            var cIndex = document.getElementById("renewalCashDropdown").selectedIndex;
            var iIndex = document.getElementById("renewalIntlDropdown").selectedIndex;

            if (mIndex == '0') {
                document.getElementById("monthlyRenewalChk").disabled = "disable";
                document.getElementById("monthlyRenewalChk").checked = "";
            }
            else {
                document.getElementById("monthlyRenewalChk").disabled = "";
            }

            if (cIndex == '0') {
                document.getElementById("cashRenewalChk").disabled = "disable";
                document.getElementById("cashRenewalChk").checked = "";
            }
            else {
                document.getElementById("cashRenewalChk").disabled = "";
            }

            if (iIndex == '0') {
                document.getElementById("intlRenewalChk").disabled = "disable";
                document.getElementById("intlRenewalChk").checked = "";
            }
            else {
                document.getElementById("intlRenewalChk").disabled = "";
            }

        }

        function setRenewTypeRadioState() {

            setRenewChkBxState()

            var mIndex = document.getElementById( "renewalMonthlyDropdown" ).selectedIndex;
            var cIndex = document.getElementById( "renewalCashDropdown" ).selectedIndex;
            var iIndex = document.getElementById( "renewalIntlDropdown" ).selectedIndex;

            var mChk = document.getElementById( "monthlyRenewalChk" ).checked;
            var cChk = document.getElementById( "cashRenewalChk" ).checked;
            var iChk = document.getElementById( "intlRenewalChk" ).checked;

            var totalSelections = mIndex + cIndex + iIndex + mChk + cChk + iChk;

            if ( totalSelections > 0 ) {
                document.getElementById( "setRenewalChargeTypeDiv" ).style.display = "block";
            } else {
                document.getElementById( "setRenewalChargeTypeDiv" ).style.display = "none";
            }

        }

        function SaveRenewals() {

            var monthlyId = document.getElementById( "renewalMonthlyDropdown" ).value;
            var cashId = document.getElementById( "renewalCashDropdown" ).value;
            var intlAmnt = document.getElementById( "renewalIntlDropdown" ).value;

            var monthlyChk = document.getElementById( "monthlyRenewalChk" ).checked;
            var cashChk = document.getElementById( "cashRenewalChk" ).checked;
            var intlChk = document.getElementById( "intlRenewalChk" ).checked;

            var typeCollection = document.getElementById( "renewChargeTypeRadio" ).getElementsByTagName( "input" );
            var renewalChargeType;
            for ( var i = 0; i < typeCollection.length; i++ ) {
                if ( typeCollection[i].checked ) {
                    renewalChargeType = typeCollection[i].value;
                }
            }

            var temp = monthlyId + "," + monthlyChk;
            temp += "~" + cashId + "," + cashChk;
            temp += "~" + intlAmnt + "," + intlChk;
            temp += "~" + renewalChargeType;
            //alert(temp)
            var args = "SaveRenewals:" + temp;
            UseCallBack( args, "SaveRenewals" );

        }

        function updateRenwalStatusBarLabels( xml ) {

            var obj = new ActiveXObject( "MsXml2.DOMDocument" );
            obj.loadXML(xml);
            

            var plans = obj.getElementsByTagName("plans");
            
            var planRef;

            for ( var i = 0; i < plans.length; i++ ) {

                planRef = plans[i].getElementsByTagName( "planref" );

                if ( planRef[0].text == 'Pay As You Go' ) {
                    var planCost = parseFloat( plans[i].getElementsByTagName( "plan_cost" )[0].text );
                    document.getElementById( "renewalCashLbl" ).innerText = FormatMoney( planCost );
                }

                if (planRef[0].text == 'Monthly') {
                    var monthlyCost = parseFloat( plans[i].getElementsByTagName( "monthly_cost" )[0].text );
                    document.getElementById( "renewalMonthLbl" ).innerText = FormatMoney( monthlyCost );
                }
            }

            // Set Intl renewal label
            document.getElementById( "renewIntlLbl" ).innerText = FormatMoney( document.getElementById( "renewalIntlDropdown" ).value );

            // Set monthly and cash labels when there is no plan selected
            if ( document.getElementById( "renewalMonthlyDropdown" ).value == 0 ) {
                document.getElementById( "renewalMonthLbl" ).innerText = FormatMoney( 0 );
            }

            if ( document.getElementById( "renewalCashDropdown" ).value == 0 ) {
                document.getElementById( "renewalCashLbl" ).innerText = FormatMoney( 0 );
            }

        }

        function controlFreezeHeader() {
            // This is to freeze the headers for the gridview that
            // is shown in the payments tab.
            // We need to timeout in order to get id.
            setTimeout( function () {
                var tabId = $find( "TabContainer1" ).get_activeTab().get_id();
                if ( tabId == 'TabPanel5' ) {
                    fh( {
                        gvId: 'transactionHistoryGridView',
                        scrollY: '100px',
                        scrollX: '100%',
                        scrollXInner: '110%'
                    } );
                } else if ( tabId == 'activityTab' ) {
                    fh( {
                        gvId: 'activityGv',
                        scrollY: '300px',
                        scrollX: '100%',
                        scrollXInner: '150%'
                    } );
                    // Data is ordered ASC, but we show the most recent
                    // first by scrolling down.
                    scrollActivtyGV();
                }
            }, 0 );
        }

        function fh( options ) {
            // This function doesn't seem to work when the header is wrapping while the body isn't wrapping.

            if ( options.constructor !== Object ) { return; }

            var gv = document.getElementById( options.gvId || '' );
            
            if ( !gv || typeof gv == 'undefined' ) { return; }

            // Initialize values
            var rows = gv.tBodies[0].getElementsByTagName( 'tr' )
                , header = rows[0]
                , bodyRows = []
                , container = gv.parentNode
                , containerHeight = container.clientHeight
                , gvWidth = gv.clientWidth
                , widths = [];

            // Remember column widths
            for ( var i = 0, ref = widths.length = header.cells.length; i < ref; i++ ) {
                widths[i] = header.cells[i].clientWidth;
            }
            // Remember rows
            for ( var i = 0, ref = bodyRows.length = rows.length; i < ref; i++ ) {
                bodyRows[i] = rows[i];
            }
            bodyRows.shift();   // Remove header
            
            // Get rid of the old table
            gv = container.removeChild( gv );
            
            // Header
            var scrollHead = document.createElement( 'div' );
            scrollHead.style.border = '0px none';
            scrollHead.style.width = options.scrollX;
            scrollHead.style.overflow = 'hidden';
            scrollHead.style.position = 'relative';

            var scrollHeadInner = document.createElement( 'div' );

            var headTbl = document.createElement( 'table' );
            // These properties are for matching the GridView properties
            headTbl.border = '1';
            headTbl.cellSpacing = '0';
            headTbl.cellPadding = '0';
            headTbl.rules = 'all';
            
            headTbl.style.marginLeft = '0px';
            // Avoid dark border between header table and body table
            if ( bodyRows.length > 0 ) { headTbl.style.borderBottom = '0px'; }

            var headThead = document.createElement( 'thead' );

            for ( var i = 0; i < header.cells.length; i++ ) {
                header.cells[i].style.width = widths[i] + 'px';
            }
            headThead.appendChild( header );
            
            headTbl.appendChild( headThead );
            scrollHeadInner.appendChild( headTbl );
            scrollHead.appendChild( scrollHeadInner );
            container.appendChild( scrollHead );


            // Body
            var scrollBody = document.createElement( 'div' );
            scrollBody.style.width = options.scrollX;
            scrollBody.style.overflow = 'auto';
            scrollBody.style.height = options.scrollY;

            var bodyTbl = document.createElement( 'table' );
            // These properties are for matching the GridView properties
            bodyTbl.border = '1';
            bodyTbl.cellSpacing = '0';
            bodyTbl.cellPadding = '0';
            bodyTbl.rules = 'all';

            // Avoid dark border between header table and body table
            bodyTbl.style.borderTop = 'none';
            bodyTbl.style.width = options.scrollXInner;
            bodyTbl.style.marginLeft = '0px';

            var bodyThead = document.createElement( 'thead' );
            var bodyTheadTr = document.createElement( 'tr' );
            bodyTheadTr.style.height = '0px';

            var th;
            for ( var i = 0; i < header.cells.length; i++ ) {
                th = document.createElement( 'th' );
                th.colSpan = '1';
                th.rowSpan = '1';
                th.style.width = widths[i] + 'px';
                th.style.height = '0px';
                th.style.padding = '0px';
                th.style.borderTopWidth = '0px';
                th.style.borderBottomWidth = '0px';
                bodyTheadTr.appendChild( th );
            }
            bodyThead.appendChild( bodyTheadTr );
            bodyTbl.appendChild( bodyThead );

            var bodyTbody = document.createElement( 'tbody' );
            for ( var count = 0; count < bodyRows.length; count++ ) {
                bodyTbody.appendChild( bodyRows[count] );
            }
            bodyTbl.appendChild( bodyTbody );

            scrollBody.appendChild( bodyTbl );
            container.appendChild( scrollBody );

            // Match header to body
            scrollHeadInner.style.width = bodyTbl.scrollWidth + 'px';
            headTbl.style.width = bodyTbl.scrollWidth + 'px';
            
            // Have the header align with the body when the body has a vertical scroll bar.
            scrollHeadInner.style.paddingRight = ( ( scrollBody.offsetWidth - scrollBody.clientWidth ) + 1 ) + 'px';

            // Make header scroll horizontally
            scrollBody.onscroll = function () {
                scrollHead.scrollLeft = this.scrollLeft;
            }

        }

        function scrollActivtyGV() {

            var activityTab = document.getElementById( 'activityTab' )
                , activityTbls = activityTab.getElementsByTagName( 'table' )
                // The second table is the body of the activityGv.
                // The element activityGv was removed by the fh function.
                , bodyScroll;
            try {
                bodyScroll = activityTbls[1].parentNode;
                bodyScroll.scrollTop = bodyScroll.scrollHeight;
            } catch( e ) {}

        }

        var newNumberForm = {

            verizonComponents: [],
            ppComponents: [],
            //atComponents: [],

            isNewOrder: function () {
                return !(/[?&]oid=/).test(location.href);
            },

            init: function () {
                var vReqType;

                this.verizonComponents = getElemsByClass('verizonComponent');
                this.ppComponents = getElemsByClass('ppComponent');
                //this.atComponents = getElemsByClass('atComponent');

                this.toggleNewNumberDivs(this);
                this.toggleZipOrNpa(this);

                if (this.isNewOrder()) {
                   
                    this.showPagePlus();
                } else {
                  
                    if (this.getCarrier() == 'page plus') {
                        this.showPagePlus();
                        return;
                    } else if (this.getCarrier() == 'verizon' || this.getCarrier() == 'concord' || this.getCarrier() == 'telco') {
                        
                        vReqType = verStatus.reqType();
                        //alert(vReqType)
                        if (vReqType == "NewService") {
                            if (verStatus.acknowledged() || verStatus.complete()) {
                                this.showVerizon();
                            } else if (verStatus.error()) {
                                this.initVerReq();
                            }
                        } else if (vReqType == "ChangeESN") {
                            if (verStatus.acknowledged() || verStatus.complete()) {
                                this.showVerizon();

                            } else if (verStatus.error()) {
                                this.initUploadESNReq();
                            }
                        } else if (vReqType == "Convert") {
                            if (verStatus.acknowledged() || verStatus.complete()) {
                                this.showVerizon();
                            } else if (verStatus.error()) {
                                this.showVerizon();
                            }
                            /*  } else if (vReqType == "Restore") {
                            if (verStatus.acknowledged() || verStatus.complete()) {
                            this.showVerizon();
                            } else if (verStatus.error()) {
                            this.showVerizon();
                            }
                            } else if (vReqType == "Suspend") {
                            if (verStatus.acknowledged() || verStatus.complete()) {
                            this.showVerizon();
                            } else if (verStatus.error()) {
                            this.showVerizon();
                            }
                            */
                        } else {
                            if (verStatus.missingMDN()) {
                                this.initVerReq();
                            } else {
                                this.showVerizon();
                            }
                        }
                    } 
                }

                this.attachEventHandlers();

            },
            initVerReq: function () {
                this.verizonComponents = this.verizonComponents.concat(getElemsByClass('verizonMdnComponent'));
                this.verizonComponents.push(document.getElementById('submitVerReqBtn'));

                this.showVerizon();

            },
            initUploadESNReq: function () {
                this.showVerizon();
                document.getElementById('saveOrderInfoBtn').style.visibility = 'hidden';  //DL
                document.getElementById("orderErrorMessage").style.color = 'black';  //DL
                document.getElementById("orderErrorMessage").innerText = "Change ESN"; //DL
                document.getElementById('submitVerReqBtn').style.display = 'inline';
            },

            attachEventHandlers: function () {
                // Remember this for use in inner scopes.
                var self = this
                    , serTypeRads = this.getNewNumberRadios()
                    , newSerTypeRads = this.getZipOrNpaRadios();

                // Hide or show the new verizon number form based on carrier. 
                document.getElementById('carrierName').onchange = function () {
                    
                        self.toggleCarrier(self);
                  
                }

                // Toggle form for "New" or "Port"
                for (var i = 0; i < serTypeRads.length; i++) {
                    serTypeRads[i].onclick = function () {
                        self.toggleNewNumberDivs(self);
                    }
                }

                // Toggle form for Zip or NPA-NXX
                for (var i = 0; i < newSerTypeRads.length; i++) {
                    newSerTypeRads[i].onclick = function () {
                        self.toggleZipOrNpa(self);
                    }
                }

                document.getElementById('submitVerReqBtn').onclick = function () {
                    return self.validateVerReqVals();
                }

            },

            // Validation
            validateVerReqVals: function () {
                if (!newNumberForm.isNewOrder()) {
                    // Change ESN - don't do validation
                    if (verStatus.reqType() == "ChangeESN") {
                        return;
                    }

                    if (newNumberForm.getNewNumberType() == 'new') {
                        var nService = new VerNewService();
                        if (nService.getIsZip()) {
                            if (!nService.validateZip()) {
                                setOrderErrorMsg("Invalid Zip."); return false;
                            }
                        } else {
                            if (!nService.validateNpaNxx()) {
                                setOrderErrorMsg("Invalid NPA - NXX."); return false;
                            }
                        }
                    } else if (newNumberForm.getNewNumberType() == 'port') {
                        if (!VerNewServicePort.isValidPhoneNum()) {
                            setOrderErrorMsg("Invalid phone number."); return false;
                        } else if (!VerNewServicePort.isValidPW()) {
                            setOrderErrorMsg("Invalid password."); return false;
                        } else if (!VerNewServicePort.isValidCarrierCode()) {
                            setOrderErrorMsg("Invalid carrier code."); return false;
                        }
                    }

                }
                return true;
            },

            // Carrier functions
            getCarrier: function () {
                return document.getElementById('carrierName').value.toLowerCase();
            },
            toggleCarrier: function (self) {
                var carrier = self.getCarrier();
                var msg = '';
                if (carrier == 'verizon' || carrier == 'concord' || carrier == 'telco') {
                    setOrderErrorMsg(msg)
                    self.showVerizon();
                } else if ((carrier == 'page plus') ){ //|| (carrier == 'all talk')) { sg 07/13/16 removing all talk
                    
                    setOrderErrorMsg(msg)
                    
                    self.showPagePlus();
                }
                //} else if (carrier == 'concord') {
                //    msg = document.getElementById('orderErrorMessage').innerText;
                    
                    
                //    setOrderErrorMsg("Port will complete once a plan is purchased."); return false;
                //}
            },
            showVerizon: function () {
                for (var i = 0; i < this.ppComponents.length; i++) {
                    this.ppComponents[i].style.display = 'none';
                }
                for (var i = 0; i < this.verizonComponents.length; i++) {
                    this.verizonComponents[i].style.display = 'block';
                }
            },
            showPagePlus: function () {
                for (var i = 0; i < this.verizonComponents.length; i++) {
                    this.verizonComponents[i].style.display = 'none';
                }
                for (var i = 0; i < this.ppComponents.length; i++) {
                    this.ppComponents[i].style.display = 'block';
                }
            },


            // New or Port functions
            getNewNumberRadios: function () {
                return document.getElementById('newNumberRadioType').getElementsByTagName('input');
            },
            getNewNumberType: function () {
                var radios = this.getNewNumberRadios(); ;
                for (var x = 0; x < radios.length; x++) {
                    if (radios[x].checked) {
                        return radios[x].value.toLowerCase();
                    }
                }
                return null;
            },
            toggleNewNumberDivs: function (self) {
                // In the current scope, this refers to the radio selected
                var type = self.getNewNumberType()
                    , portNumberDiv = document.getElementById('portNumberDiv')
                    , newNumberDiv = document.getElementById('createNumberDiv');

                if (type == 'new') {
                    portNumberDiv.style.display = 'none';
                    newNumberDiv.style.display = 'block';
                } else if (type == 'port') {
                    newNumberDiv.style.display = 'none';
                    portNumberDiv.style.display = 'block';
                }
            },

            // Zip or NPA - NXX functions
            getZipOrNpaRadios: function () {
                var radios = []
                    , inputs = document.getElementById('createZipNPARadioList')
                                     .getElementsByTagName('input');

                for (var i = 0; i < inputs.length; i++) {
                    if (inputs[i].type == 'radio') {
                        radios.push(inputs[i]);
                    }
                }
                return radios;
            },
            getZipOrNpaType: function () {
                var radios = this.getZipOrNpaRadios();
                for (var i = 0; i < radios.length; i++) {
                    if (radios[i].checked) {
                        return radios[i].value.toLowerCase();
                    }
                }
                return null;
            },
            toggleZipOrNpa: function (self) {
                var zipDiv = document.getElementById('createPhoneZipDiv')
                    , npaDiv = document.getElementById('createPhoneNpaDiv')
                    , type = self.getZipOrNpaType();

                if (type == 'createphonezip') {
                    npaDiv.style.display = 'none';
                    zipDiv.style.display = 'block';
                } else if (type == 'createnpanxx') {
                    zipDiv.style.display = 'none';
                    npaDiv.style.display = 'block';
                }
            }

        }

        var planDropdowns = {
            toggle: function () {
                var state = document.getElementById('cell_number').value.length != 10 && document.getElementById("carrierName").value != 'Concord' && document.getElementById("carrierName").value != 'Telco';
                
                var dropdowns = getElemsByClass( 'plansDropdown' );
                for ( var i = 0; i < dropdowns.length; i++ ) {
                    dropdowns[i].disabled = state;
                }
            }
        }

        var verStatus = {
            "msg": function() {
                return document.getElementById('orderErrorMessage').innerText;
            },
            "missingMDN": function() {
                return document.getElementById('cell_number').value.length != 10;
            },
            "reqType": function() {
                var msg = this.msg();
                //alert("Validate " + msg)
                if (/New(\s)?Service/i.test(msg)) {  // This includes NewServicePort
                    return "NewService";
                } else if (/Change(\s)?ESN/i.test(msg)) {
                    return "ChangeESN";
                } else if (/Convert/i.test(msg)) {
                    return "Convert";
                /*} else if (/Restore/i.test(msg)) {
                    return "Restore";
                } else if (/Suspend/i.test(msg)) {
                    return "Suspend";
                */
                } else {
                    return '';
                }
            },
            "acknowledged": function() {
                return /acknowledged|submitted/i.test(this.msg());
            },
            "complete": function() {
                return /complete/i.test(this.msg());
            },
            "error": function() {
                return /error/i.test(this.msg());
            }
        }

        function VerNewService() {

            this.zipFld = document.getElementById( 'createPhoneZip' );
            this.npaFld = document.getElementById( 'createPhoneNPA' );
            this.nxxFld = document.getElementById( 'createPhoneNXX' );

        }

        VerNewService.prototype.getIsZip = function () {
            var zipNpaRadios = document.getElementById( 'createZipNPARadioList' )
                                           .getElementsByTagName( 'input' );
            for ( var i = 0; i < zipNpaRadios.length; i++ ) {
                if ( zipNpaRadios[i].checked ) {
                    return zipNpaRadios[i].value == 'createPhoneZip' ?
                      true : false;
                }
            }
            return null;
        }

        VerNewService.prototype.validateZip = function () {
            return /(^\d{5}$)/.test( this.zipFld.value );
        }

        VerNewService.prototype.validateNpaNxx = function () {
            var ptrn = /(^\d{3}$)/;
            return ( ptrn.test( this.npaFld.value ) ) && ( ptrn.test( this.nxxFld.value ) );
        }

        var VerNewServicePort = {
            isValidPhoneNum: function () {
                return /^[0-9]{10}$/.test( document.getElementById( 'portPhone' ).value );
            },
            isValidPW: function () {
                return document.getElementById( 'portPwFld' ).value.trim().length > 0;
            },
            isValidCarrierCode: function () {
                return document.getElementById( 'carrierCodeFld' ).value.trim().length > 0;
            }
        }

        var refreshVerStatusMsg = {
            interval: null,
            start: function () {
                if ( verStatus.acknowledged() ) {
                    this.interval = setInterval( function () {
                        UseCallBack( 'refreshVerStatusMsg:', 'refreshVerStatusMsg' );
                    }, 5000 );
                }
            },
            refresh: function (response) {
                //(response)
                var json = str2Json( response )
                    , msgSpan = document.getElementById( 'orderErrorMessage' )
                    , cellNumSpan = document.getElementById( 'verCellLbl' )
                    , mainCellFld = document.getElementById( 'cell_number' )
                    , barCell = document.getElementById( 'lblCellNumber' )    // Top bar cell number
                    , barStatus = document.getElementById( 'statusLbl' );    // Top bar status
                
                msgSpan.style.color = 'black'; msgSpan.innerText = json.msg;
                barCell.innerText = cellNumSpan.innerText = mainCellFld.value = json.cell;
                barStatus.innerText = json.mdnStatus;

                // Stop polling if message comes back with either complete or error
                if ( /complete|error/i.test( json.msg ) ) {
                    if ( verStatus.reqType() == "NewService" && verStatus.error() ) {
                        newNumberForm.initVerReq();
                    } else if (verStatus.reqType() == "ChangeESN" && verStatus.error()) {
                        
                        newNumberForm.initUploadESNReq();
                    } else if ( verStatus.reqType() == '' && !verStatus.missingMDN() ) {
                        document.getElementById( 'submitVerReqBtn' ).style.display = 'none';
                    }
                    this.end();
                    planDropdowns.toggle();
                } else if ( json.convertStatus == 'ChangedToVerizon' ) {
                    UseCallBack( 'getEncodedOid:', 'getEncodedOid' );
                }
            },
            end: function () {
                clearInterval( this.interval );
            }
        }

        var adminTasks = {
            attachHandlers: function () {
                var exeAdminTaskBtn = document.getElementById( 'executeAdminTasks' );
                if ( exeAdminTaskBtn == null ) { return; }
                exeAdminTaskBtn.onclick = adminTasks.validateAdminTasks;
            },
            validateAdminTasks: function () {
                var inputs = document.getElementById( 'adminTasksRadiolist' ).getElementsByTagName( 'input' );
                for ( var i = 0; i < inputs.length; i++ ) {
                    if ( inputs[i].type == 'radio' && inputs[i].checked ) {
                        return true;
                    }
                }
                alert( 'No task has been selected' );
                return false;
            }
        }

        var refreshIntlCalls = function () {
            document.getElementById( 'refreshIntlCallGV' ).click();
        }



        function dateChanged(calendar) {


           
                //alert(calendar.id);
                // Beware that this function is called even if the end-user only
                // changed the month/year.  In order to determine if a date was
                // clicked you can use the dateClicked property of the calendar:
                //alert('hi');
                if (calendar.dateClicked) {
                    var y = calendar.date.getFullYear();
                    var m = calendar.date.getMonth();
                    var d = calendar.date.getDate();
                    
                    if (calendar.eventName == "from") {
                        document.getElementById("txtFrom").value = m + 1 + "/" + d + "/" + y;
                   
                }
               if (calendar.eventName == "to") {
                   document.getElementById("txtTo").value = m + 1 + "/" + d + "/" + y;
                }


               document.getElementById("useCalendar").Value = "true";
               
                    __doPostBack('', '');
                    
                    calendar.callCloseHandler()
                    //calendar.hide;
                }   
        }

        function showCharge(amount, id, type) {
            //alert(amount);
            //alert(id)
            document.getElementById('<%=rfAmount.ClientID%>').value = amount
            document.getElementById('<%=transIdHF.ClientID()%>').value = id
            document.getElementById('<%=errorLbl.ClientID()%>').value = ""
            
        }

        //function checkCarrier() {

        //    if (document.getElementById("carrierName").value == "Concord") {
                
        //        document.getElementById("orderErrorMessage").value = "Port will complete once a plan is purchased."
        //    }
        //}
    </script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div id="order_info_bar" style="font-size: 9pt;">
            <div>
                <span class="statusBlock">
                    <label>Serial #:</label>
                    <asp:Label ID="serailNumLbl" runat="server" Text="New Order" Font-Bold="true" Font-Names="Arial"
                        Font-Size="9pt" ForeColor="black"></asp:Label>&nbsp;
                </span>
        
                <span class="statusBlock">
                    <label>Cell #: </label>   
                    <asp:Label ID="lblCellNumber" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                        ForeColor="black"></asp:Label>&nbsp;
                </span>

                <span class="statusBlock">
                    <label>Name:</label>
                    <asp:Label ID="lblName" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                        ForeColor="black"></asp:Label>&nbsp;
                </span>

                <span class="statusBlock" style="width: 150px;">
                    <label>ATC Plan:</label>
                    <asp:Label ID="planNameBarLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                        ForeColor="black"></asp:Label>&nbsp;
                </span>

                <span class="statusBlock" style="width: 135px;">
                    <label>Signup Date:</label>
                    <asp:Label ID="signupDateBarLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                        ForeColor="black"></asp:Label>&nbsp;
                </span>

                <span class="statusBlock" style="width: 100px; text-align: right;">
                    <label>Agent:</label>
                    <asp:Label ID="initialAgentLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                        ForeColor="black"></asp:Label>
                </span>

        </div>

        <br />

        <div>

            <span class="statusBlock">
                <label>As of: </label>
                <asp:Label ID="lastUpdatedLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>
            </span>
          
            <span class="statusBlock" >
                <label>Status:</label>
                <asp:Label ID="statusLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;
<%--                <asp:Button ID="statusBtn" runat="server" Height="12px" 
                Width="28px" BackColor="#C8DCFB" BorderStyle="Outset" ForeColor="White" />--%>
                <asp:LinkButton ID="statusLkBn" runat="server" Font-Size="X-Small" style="text-decoration: underline">Refresh</asp:LinkButton>
            </span>
                
            <span class="statusBlock">
                <label>Balance:</label>
                <asp:Label ID="balanceLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>
                &nbsp;&nbsp;
                <label>Intl:</label>
                <asp:Label ID="inltBallanceLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>
            </span>


                
            <span class="statusBlock" style="width: 150px;">
                <label>Expiration date:</label>
                <asp:Label ID="expirationDateLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;
            </span>

            <span class="statusBlock" style="width: 150px;">
                <span id="cashStackedDiv" runat="server" visible="false"><label>Cash Stacked:</label>
                <asp:Label ID="cashStackedLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;</span>
                <span id="planexpdiv" runat="server" visible="false"><label>Plan Expiration:</label>
                <asp:Label ID="planExpirationDateLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;</span>
            </span>
                
            <span class="statusBlock" style="width: 85px; text-align: right;">
                <label>Stacked:</label>
                <asp:Label ID="pinStackedStatusLbl" runat="server" Font-Bold="true"
                        Text="0" Font-Names="Arial" Font-Size="9pt" ForeColor="black"></asp:Label>
            </span> 

        </div>

        <br />

        <div>
            <span class="statusBlock">
                <label id="lblVendorName" runat="server">Vendor Plan:</label>
                <asp:Label ID="vendorNameLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;
            </span>

            <span class="statusBlock">
                <span runat="server" visible="false" id="divMinRemaining">
                <label>Minutes Remaining:</label>
                <asp:Label ID="totalMinutesLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;
            </span>

            <span runat="server" visible="false" id="divMinUsed">
                <label>Minutes Used:</label>
                <asp:Label ID="minutesUsedLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;
                </span>
            </span>

            <span class="statusBlock" style="width: 350px">
                 <asp:Label ID="esnValidLbl" runat="server" visible="false">ESN:</asp:Label>
                <asp:Label ID="esnLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt" visible="False"
                    ForeColor="black"></asp:Label>&nbsp;

                    
                 <asp:LinkButton ID="esnLkBn" runat="server" Font-Size="X-Small" 
                style="text-decoration: underline" visible="false">Refresh</asp:LinkButton>&nbsp;

            <%--</span>--%>




<%--           <span class="statusBlock">
                <label></label>
                <asp:Label ID="incomingMinutesLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;
            </span>--%>                
<%--          <span class="statusBlock">
                <label></label>
                <asp:Label ID="outgoingMinutesLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>&nbsp;
            </span>--%>
             <%--<span class="statusBlock" style="width: 200px" >--%>
                 <asp:Label ID="LastValidateLbl" runat="server" visible="false">- As of:</asp:Label>
                <asp:Label ID="LastValidateDateLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt" visible="False"
                    ForeColor="black"></asp:Label>&nbsp;


            </span>
                


            <span class="statusBlock" style="width: 210px; text-align: right;">
                <label>Renew M:</label>
                <asp:Label ID="renewalMonthLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>
                <label> C: </label>
                <asp:Label ID="renewalCashLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>
                <label>I:</label>
                <asp:Label ID="renewIntlLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                    ForeColor="black"></asp:Label>
            </span> 

        </div>
        

    </div>

    <div>
         
        <asp:TabContainer ID="TabContainer1" runat="server" OnClientActiveTabChanged="toggleCloseBtn">

            <asp:TabPanel runat="server" ID="TabPanel1" HeaderText="Customer Information">

                <ContentTemplate>

                    <div id="customerTabDiv">

                        <div id="tab1FieldsDiv" style="height: 280px;">

                            <div id="basicCustomerIfnoDiv" class="orderFields"
                                style="float: left; width: 38%; border-right: 1px solid black;">
                            
                                <div id="initialAgentDropdownDiv" class="CustomRow orderFields" runat="server" visible="false">
                                    <label class="custLabel">Agent</label>
                                    <asp:DropDownList ID="initialAgentDropdown" runat="server">
                                    </asp:DropDownList>
                                </div>

                                <div class="CustomRow">
                                    <label class="custLabel">Title:</label>
                                    <asp:TextBox ID="prefix" Width="50px" runat="server"></asp:TextBox>

                                    <label style="width: 75px;">First Name:</label>
                                    <asp:TextBox ID="fname" Width="105px" runat="server"></asp:TextBox>

                                </div>

                                <div class="CustomRow">
                                    <label class="custLabel">Last Name: </label>
                                    <asp:TextBox ID="lname" runat="server" Width="250px"></asp:TextBox>
                                </div>

                                <div class="CustomRow">
                                    <label class="custLabel">Phone: </label>
                                    <asp:TextBox ID="phone" runat="server" Width="250px"></asp:TextBox>
                                </div>

                                <div class="CustomRow">
                                    <label class="custLabel">Address: </label>
                                    <asp:TextBox ID="address" runat="server" Width="250px"></asp:TextBox>
                                </div>

                                <div class="CustomRow">
                                    <label class="custLabel">City: </label>
                                    <asp:TextBox ID="city" Width="120px" runat="server"></asp:TextBox>

                                    <label class="custLabel">State: </label>
                                    <asp:TextBox ID="state" Width="60px" runat="server"></asp:TextBox>
                                </div>

                                <div class="CustomRow">
                                    <label class="custLabel">Zip</label>
                                    <asp:TextBox ID="zip" runat="server" Width="50px"></asp:TextBox>

                                    <label style="width: 85px;">Customer Pin: </label>
                                    <asp:TextBox ID="customerPin" runat="server" Width="95px"></asp:TextBox>

                                </div>
                             
                                <div class="CustomRow">
                                    <label class="custLabel">EMail:</label>
                                    <asp:TextBox ID="email" runat="server" Width="250px"></asp:TextBox>
                                </div>

                            </div>

                            <div id="phoneInfoDiv" class="orderFields" 
                                style="float: left; width: 60%; padding-left: 15px;">
                               
                                <div class="phoneFieldsHeight">

                                    <div class="leftPhoneFields">
                                        <label class="orderLabel">Carrier Name: </label>
                                        <asp:DropDownList ID="carrierName" runat="server" AutoPostBack="false"
                                                    Width="105px" >
                                            <asp:ListItem>Page Plus</asp:ListItem>
                                            <asp:ListItem>Verizon</asp:ListItem>
                                            <asp:ListItem>Concord</asp:ListItem>
                                            <asp:ListItem>Telco</asp:ListItem>
                                            <%--<asp:ListItem>All Talk</asp:ListItem>--%>
                                        </asp:DropDownList>
                                    </div>

                                    <div id="readOnlyCellDiv" style="display: none;" runat="server" class="rightPhoneFields verizonComponent">
                                        <label style="width: 75px;">Cell Phone: </label>
                                        <asp:Label ID="verCellLbl" runat="server"></asp:Label>
                                    </div>

                                </div>
                         
                                <div id="pagePlusCellNumDiv" class="phoneFieldsHeight ppComponent" runat="server">

                                    <div class="leftPhoneFields">
                                        <label class="orderLabel">Cell Number: </label>
                                        <asp:TextBox ID="cell_number" runat="server" Width="105px" MaxLength="10"></asp:TextBox>
                                    </div>
                                
                                    <div id="confirmCellNumberSpan" runat="server" class="rightPhoneFields" style="display: none;">
                                        <label class="orderLabel">Confirm: </label>
                                        <asp:TextBox ID="confirmCellNumber" runat="server" Width="105px"></asp:TextBox>
                                    </div>

                                </div>

                                <div class="phoneFieldsHeight">
                                    <div class="leftPhoneFields">
                                        <label class="orderLabel">Serial: </label>
                                        <asp:TextBox ID="serialNumber" runat="server" Width="105px" onchange="showSerialConfirm();getPlanInfoFromSerial();"></asp:TextBox>
                                    </div>

                                    <div id="confirmSerialSpan" runat="server" class="rightPhoneFields" style="display: none;">
                                        <label class="orderLabel">Confirm: </label>
                                        <asp:TextBox ID="confirmSerialNumber" runat="server" Width="105px" onchange="getPlanInfoFromSerial()"></asp:TextBox>
                                    </div>
                                </div>

                                <div style="height: 190px; Width: 100%;">

                                    <div class="leftPhoneFields" style="height: 100%;">
                                        <div class="phoneFieldsHeight">
                                            <label class="orderLabel">ESN: </label>
                                            <asp:Label ID="esn" ForeColor="Black" runat="server"></asp:Label>
                                            <asp:HiddenField ID="hiddenESN" runat="server" />
                                        </div>

                                        <div class="phoneFieldsHeight">
                                            <label class="orderLabel">Intl Pin: </label>
                                            <asp:Label ID="intlPin" ForeColor="Black" runat="server"></asp:Label>
                                            <asp:HiddenField ID="hiddenIntlPin" runat="server" />
                                        </div>

                                        <div class="phoneFieldsHeight">
                                            <label class="orderLabel">Vendor Pin: </label>
                                            <asp:TextBox ID="vendorPin" runat="server" Width="50px"></asp:TextBox>
                                        </div>

                                        <div class="phoneFieldsHeight ppComponent" style="display: none;">
                                            <div id="moniterChckBox" runat="server">
                                                <label>Monitor: </label>
                                                <asp:CheckBox ID="monitor" Enabled="false" runat="server"/>
                                            </div>
                                        </div>

                                        <div class="phoneFieldsHeight verReqBtns">
                                            <asp:Button ID="submitVerReqBtn" runat="server" Text="Submit" style="display: none;" />
                                        </div>

                                    </div>

                                    <%--Currently this shows for Verizon orders only ( see javascript )--%>
                                    <div runat="server" id="newNumberFldsDiv" style="height: 100%; display: none;" 
                                            class="rightPhoneFields verizonMdnComponent">
                                        
                                        <div id="newNumberHolderDiv" style="height: 100%; width: 85%; border: 1px solid gray; border-radius: 3px;">

                                            <asp:RadioButtonList ID="newNumberRadioType" runat="server" Font-Bold="true"
                                                    RepeatDirection="Horizontal" TextAlign="Left" Width="130px">
                                                <asp:ListItem Selected="True">New</asp:ListItem>
                                                <asp:ListItem>Port</asp:ListItem>
                                            </asp:RadioButtonList>

                                            <div id="portNumberDiv" class="newNumberDiv" style="display: none;">
                                            
                                                <div class="newNumberRow" id="phoneNumberDiv">
                                                    <div class="lblDiv">
                                                        <label>Phone Number</label>
                                                    </div>
                                                    <div class="fldDiv">
                                                        <asp:TextBox ID="portPhone" runat="server"></asp:TextBox>
                                                    </div>
                                                </div>

                                                <div class="newNumberRow" id="pinNumberDiv">
                                                    <div class="lblDiv">
                                                        <label>Password</label>
                                                    </div>
                                                    <div class="fldDiv">
                                                        <asp:TextBox ID="portPwFld" runat="server"></asp:TextBox>
                                                    </div>
                                                </div>

                                                <div class="newNumberRow" id="carrierCodeDiv">
                                                    <div class="lblDiv">
                                                        <label>Account Code</label>
                                                    </div>
                                                    <div class="fldDiv">
                                                        <asp:TextBox ID="carrierCodeFld" runat="server"></asp:TextBox>
                                                    </div>
                                                </div>

                                                <div class="newNumberRow" id="phoneOriginTypeDiv">
                                                    <div>
                                                        <asp:RadioButtonList ID="phoneOriginTypeRadio" runat="server"
                                                                             RepeatDirection="Horizontal" Font-Bold="true"
                                                                             TextAlign="Left" Width="180px">
                                                            <asp:ListItem Selected="True"
                                                                           Value="wireless"
                                                                           Text="Wireless">
                                                            </asp:ListItem>
                                                            <asp:ListItem Value="landLine" Text="Land Line">
                                                            </asp:ListItem>
                                                        </asp:RadioButtonList>
                                                    </div>
                                                </div>

                                            </div>

                                            <div id="createNumberDiv" class="newNumberDiv" style="display: none;">
                                                <asp:HiddenField ID="hiddenZip" runat="server" value=""/>
                                                <div style="height: 50px;">

                                                    <div class="leftPhoneFields">
                                                        <asp:RadioButtonList ID="createZipNPARadioList" runat="server" TextAlign="Left">
                                                            <asp:ListItem Selected="True" Value="createPhoneZip">Zip Code</asp:ListItem>
                                                            <asp:ListItem Value="createNpaNxx">NPA - NXX</asp:ListItem>
                                                        </asp:RadioButtonList>
                                                    </div>

                                                    <div id="displayZipNpaDivs" class="rightPhoneFields">

                                                        <div id="createPhoneZipDiv" style="display: none;">
                                                            <div class="lblDiv" style="width: 55px;">
                                                                <label>Zip Code</label>
                                                            </div>
                                                            <div class="fldDiv">
                                                                <asp:TextBox ID="createPhoneZip" Width="60px" runat="server"></asp:TextBox>
                                                            </div>
                                                        </div>

                                                        <div id="createPhoneNpaDiv" style="display: none;">
                                                            <table class="createPhoneNumberTbl">
                                                                <tbody>
                                                                    <tr>
                                                                        <td>
                                                                            <label>NPA</label>
                                                                        </td>
                                                                        <td style="width: 10px;"></td>
                                                                        <td>
                                                                            <label>NXX</label>
                                                                        </td>
                                                                    </tr>
                                                                    <tr>
                                                                        <td>
                                                                            <asp:TextBox ID="createPhoneNPA" runat="server"></asp:TextBox>
                                                                        </td>
                                                                        <td style="width: 10px; text-align: center;"> - </td>
                                                                        <td>
                                                                            <asp:TextBox ID="createPhoneNXX" runat="server"></asp:TextBox>
                                                                        </td>
                                                                    </tr>
                                                                </tbody>
                                                            </table>
                                                        </div>

                                                    </div>

                                                </div>

                                            </div>

                                        </div>
                                            
                                    </div>

                                </div>

                            </div>

                        </div>

                        <br />

                        <div class="CustomRow customerTabBtns">

                            <asp:Button runat="server" Text="Cancel" OnCommand="CancelOrder"/>

                            <asp:Button ID="saveOrderInfoBtn" runat="server" OnCommand="SaveOrderBtnClick" 
                                OnClientClick="return continueSaveOrder();"/>
                                
                             <asp:Button ID="clearMsgBtn" runat="server" Text="Clear" OnCommand="ClearStatusMsg"/>
                             
                            <span id="orderErrorMessage" runat="server" style="color: Red; float: right; padding-right: 15px;"></span>
                        </div>

                    </div>

                </ContentTemplate>
            </asp:TabPanel>

            <asp:TabPanel runat="server" ID="TabPanel5" HeaderText="Payments" OnClientClick="controlFreezeHeader">

                <ContentTemplate>
                    <div>
                        <div style="height: 300px;">

                            <div id="billingPanel" style="width: 69%; height: 275px; float: left; margin-top: -15px;">
                                <fieldset id="billingInfoFieldset" style="height: 100%;">
                                    <legend>Billing Information</legend>                                                                                                                                                                                                                                                                                                                                                         <div id="innerBillingContainer">

                                    <div id="billingInfo" style="width: 50%; float: left; border-right: 1px solid gray;">

                                        <div class="CustomRow orderFields">
                                            <label class="custLabel">First Name: </label>
                                            <asp:TextBox ID="billingFname" runat="server" Width="200px" onchange="fieldChanged();"></asp:TextBox>
                                        </div>

                                        <div class="CustomRow orderFields">
                            
                                            <label class="custLabel">Last Name: </label>
                                            <asp:TextBox ID="billingLname" runat="server" Width="200px" onchange="fieldChanged();"></asp:TextBox>
                            
                                        </div>

                                        <div class="CustomRow orderFields">
                                            <label class="custLabel">Phone: </label>
                                            <asp:TextBox ID="billingPhone" runat="server" Width="200px" onchange="fieldChanged();"></asp:TextBox>
                                        </div>

                                        <div class="CustomRow orderFields">
                                            <label class="custLabel">Address: </label>
                                            <asp:TextBox ID="billingAddress" runat="server" Width="200px" onchange="fieldChanged();"></asp:TextBox>
                                        </div>

                                        <div class="CustomRow orderFields">
                                            <label class="custLabel">City: </label>
                                            <asp:TextBox ID="billingCity" Width="200px" runat="server" onchange="fieldChanged();"></asp:TextBox>
                                        </div>

                                        <div class="CustomRow orderFields">
                                            
                                            <label class="custLabel">State: </label>
                                            <asp:TextBox ID="billingState" runat="server" Width="80px" onchange="fieldChanged();"></asp:TextBox>

                                            <label style="width: 20px;">Zip: </label>
                                            <asp:TextBox ID="billingZip" runat="server" Width="80px" onchange="fieldChanged();"></asp:TextBox>

                                        </div>

                                        <div class="CustomRow orderFields">
                                            
                                            <label class="custLabel">EMail: </label>
                                            <asp:TextBox ID="billingEmail" runat="server" Width="200px"></asp:TextBox>

                                        </div>

                                    </div>

                                    <div id="selectChargeMethodDiv" style="width: 47%; height: 230px; float: right;">

                                        <div class="CustomRow" id="salesRepDiv" runat="server">
                                            <label style="font-weight: bold; font-size: smaller;">Sales Rep: </label>
                                            <asp:DropDownList ID="salesRepDropdown" runat="server" onchange="agentCredit.getLimit()">
                                            </asp:DropDownList>
                                        </div>

                                        <div class="CustomRow">
                                            <asp:RadioButtonList ID="selectPaymentMethod" runat="server" RepeatDirection="Horizontal"
                                                        onclick="togglePaymentMethodForm();" Font-Size="Smaller" Font-Bold="true" Width="220px">
                                                <asp:ListItem selected="true" onclick="TogglePayButton();">Credit Card</asp:ListItem>
                                                <asp:ListItem onclick="TogglePayButton();">Agent Account</asp:ListItem>
                                                <%--<asp:ListItem>Bank Account (USA Only)</asp:ListItem>--%>
                                            </asp:RadioButtonList>
                                        </div>

                                        <div id="crediCardDiv" runat="server" class="orderFields transTypeDiv">
                                    
                                            <label class="invoiceLabel">Card Number: </label>
                                            <asp:TextBox ID="creditCardNumber" onchange="TogglePayButton();" runat="server" Width="175px"></asp:TextBox>
                                            <br /><br />
                                    
                                            <label class="invoiceLabel">Exp Date: </label>

                                            <asp:DropDownList ID="creditCardExpirationMonth" runat="server" onchange="clearIncompleteCCNum();">
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

                                            <label style="font-size: 13px; font-weight: normal;">/</label>

                                            <asp:DropDownList ID="creditCardExpirationYear" runat="server" onchange="setupCCExpDropdown();clearIncompleteCCNum();">
                                            </asp:DropDownList>

                                            <br /><br />

                                            <label class="invoiceLabel">Card Code: </label>
                                            <asp:TextBox ID="creditCardCode" Width="98px" runat="server" onchange="clearIncompleteCCNum(); fieldChanged();"></asp:TextBox>

                                        </div>

                                        <div id="creditLimitDiv" runat="server" class="orderFields transTypeDiv" style="display: none;">
                                            <div class="creditRow">
                                                <label style="width: 75px;">Credit Limit: </label>
                                                <asp:Label ID="creditLimitLbl" runat="server" Width="125px"></asp:Label>
                                            </div>

                                            <div class="creditRow">
                                                <label style="width: 75px;">Used: </label>
                                                <asp:Label ID="creditUsedLbl" runat="server" Width="125px"></asp:Label>
                                            </div>

                                            <div class="creditRow">
                                                <label style="width: 75px;">Available: </label>
                                                <asp:Label ID="creditAvailableLbl" runat="server" Width="125px"></asp:Label>
                                            </div>
                                        </div>

                                        <%--<div id="bankAccoutnDiv" runat="server" style="display: none;" class="orderFields">
                                            <label>Bank Name:</label>
                                            <asp:TextBox ID="bankName" runat="server" Width="150px"></asp:TextBox>
                                            <br /><br />

                                            <label>Bank Account Number:</label>
                                            <asp:TextBox ID="accountNumber" runat="server" Width="150px"></asp:TextBox>
                                            <br /><br /><br />

                                            <label>ABA Routing Number:</label>
                                            <asp:TextBox ID="abaRoutingNumber" runat="server" Width="150px"></asp:TextBox>
                                            <br /><br /><br />

                                            <label>Name On Account:</label>
                                            <asp:TextBox ID="nameOnAccount" runat="server" Width="150px"></asp:TextBox>
                                            <br /><br />

                                            <label>Bank Account Type:</label>
                                            <asp:DropDownList ID="bankAccountTypeDropdown" runat="server">
                                                <asp:ListItem>Personal Checking</asp:ListItem>
                                                <asp:ListItem>Personal Savings</asp:ListItem>
                                                <asp:ListItem>Business Checking</asp:ListItem>
                                            </asp:DropDownList>
                                    
                                        </div>--%>
                                        
                                        <div style="clear: both; height: 65px; position: relative;">
                                            <asp:Button ID="saveCustomerBillingInfoBtn" Style="position: absolute; bottom: 0px;" runat="server" Text="Save" />
                                        </div>

                                    </div>
                                    
                                    <div style="display: inline-block; width: 100%; height: 25px;">
                                        <asp:Label ID="authNoteLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                                            ForeColor="black"></asp:Label>
                                    </div>

                                </fieldset>
                            </div>

                            <div id="invoicePanel" style="width: 30%; height: 275px; float: right; margin-top: -15px;">
                                <fieldset id="fixedChargesPnl" style="background-color: InfoBackground; height: 100%;">
                                    <legend>Invoice</legend>

                                    <div class="InvoiceRow">
                                        <span style="float: left;">
                                            <label class="invoiceLabel">Monthly: </label>
                                            <asp:DropDownList Width="110px" ID="monthlyPlanDropdown" runat="server" 
                                                              onchange="planCostLbls.getCost(this); TogglePayButton(); "
                                                              class="plansDropdown"><%--ToggleEnabled();--%>
                                            </asp:DropDownList>
                                        </span>
                                        <asp:Label ID="monthlyPlanDropdownCostLbl" runat="server" style="float: right;"></asp:Label>
                                    </div>

                                    <div class="InvoiceRow">
                                        <span style="float: left;">
                                            <label class="invoiceLabel">Cash: </label>
                                            <asp:DropDownList Width="110px" ID="cashBalanceDropdown" runat="server" 
                                                              onchange="planCostLbls.getCost(this); TogglePayButton(); "
                                                              class="plansDropdown"><%--ToggleEnabled();--%>
                                            </asp:DropDownList>
                                        </span>
                                        <asp:Label ID="cashBalanceDropdownCostLbl" runat="server" Style="float: right"></asp:Label>
                                    </div>

                                    <div class="InvoiceRow">
                                        <span style="float: left;">
                                            <label class="invoiceLabel">Intl: </label>
                                            <asp:DropDownList Width="110px" ID="intlBalanceDropdown" runat="server" 
                                                              onchange="SetIntlLbl(this); TogglePayButton()"
                                                              class="plansDropdown">
                                                 <asp:ListItem Value="">None</asp:ListItem>
                                                 <asp:ListItem Value="7.00">$7.00</asp:ListItem>
                                                 <asp:ListItem Value="14.00">$14.00</asp:ListItem>
                                                 <asp:ListItem Value="21.00">$21.00</asp:ListItem> 
                                            </asp:DropDownList>
                                        </span>
                                        <asp:Label ID="intlBalanceDropdownCostLbl" runat="server" Style="float: right"></asp:Label>
                                    </div>

                                    <div class="InvoiceRow" style="margin: 0px;">
                                        <span style="float: left;">
                                            <label class="invoiceLabel">Misc.</label>
                                            <asp:TextBox ID="miscNameFld" runat="server" Width="90" onchange="TogglePayButton();"></asp:TextBox>
                                        </span>
                                        <span style="float: right;">
                                            <label>Cost</label>
                                            <asp:TextBox ID="miscCostFld" runat="server" style="text-align: right;" Width="30" onchange="calculateTotal(); TogglePayButton();"></asp:TextBox>
                                        </span>

                                    </div>

                                    <div id="itemCheckList" style="max-height: 110px; overflow: auto; overflow-x: hidden;">
                                        <asp:Repeater ID="itemRepeater" runat="server">
                                            <itemtemplate>
                                                <div style="height: 20px;">
                                                    <span style="float: left;">
                                                        <input type="checkbox" runat="server" id="itemsCheck" clientidmode="AutoID" />
                                                        <label><%# Eval("item_name") %></label>
                                                        <asp:HiddenField ID="hiddenItemCost" ClientIDMode="AutoID" 
                                                               Value='<%# Eval("item_cost") %>' runat="server" />
                                                    </span>
                                                    <span id="itemCostLbl" clientidmode="AutoID" style="float: right;" runat="server"></span>
                                                </div>
                                            </itemtemplate>
                                        </asp:Repeater>
                                    </div>

                                    <hr />

                                    <div class="InvoiceRow" style="margin: 0px;">
                                        <asp:Button ID="payButton" runat="server" disabled="disabled" Text="Pay" style="float: left;"/>

                                        <span style="float: right;">
                                            <label>Amount Due: </label>
                                            <asp:Label ID="amntDue" runat="server" Text="$0.00"></asp:Label>
                                        </span>
                                    </div>

                                </fieldset>
                            </div>

                        </div>

                        <br />
                        
                        <div style="height: 115px; font-size: 11px;">
                            <asp:GridView ID="transactionHistoryGridView" runat="server" AutoGenerateColumns="false"
                                            ShowHeader="true" Width="100%" DataSourceID="SqlDataSource3" Font-Size="11px">
                                <Columns>
                                    <asp:BoundField DataField="trans_type" HeaderText="Type" />
                                    <asp:BoundField DataField="user" HeaderText="User" SortExpression="user" />
                                    <asp:BoundField DataField="agent" HeaderText="Agent" SortExpression="agent" />
                                    <asp:BoundField DataField="paydate" HeaderText="Pay Date" SortExpression="paydate" />
                                    <asp:BoundField DataField="monthly_amt" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Monthly" SortExpression="monthly_amt" />
                                    <asp:BoundField DataField="cash_amt" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Cash" SortExpression="cash_amt" />
                                    <asp:BoundField DataField="intl_amt" DataFormatString="{0:c}" HtmlEncode="false" HeaderText="Intl" SortExpression="intl_amt" />
                                    <asp:BoundField DataField="item_amt" DataFormatString="{0:c}" HtmlEncode="false" HeaderText="Items" SortExpression="item_amt" />
                                    <asp:BoundField DataField="authmessage" HeaderText="Authorization Message" SortExpression="authmessage" />
                                    <asp:BoundField DataField="total" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Total" SortExpression="total" />
                                </Columns>
                                    
                            </asp:GridView>

                            <asp:SqlDataSource ID="SqlDataSource3" runat="server" CancelSelectOnNullParameter="false">
                            </asp:SqlDataSource>
                        </div> 

                    </div>

                </ContentTemplate>
            </asp:TabPanel>

            <asp:TabPanel runat="server" ID="equipmentTab" HeaderText="Equipment">

                <ContentTemplate>
                    <div>
                        <asp:GridView ID="equipmentGridview" runat="server" AutoGenerateColumns="false"  
                                        Width="60%" Font-Size="11px" AllowPaging="true" PageSize="20"
                                        ShowHeaderWhenEmpty="true">
                            <Columns>
                                <asp:BoundField ItemStyle-Width="220px" DataField="item_name" HeaderText="Name" />
                                <asp:BoundField ItemStyle-Width="220px" DataField="paydate" HeaderText="Date" />
                                <asp:BoundField ItemStyle-Width="95px" DataField="item_cost" 
                                            DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Cost" />
                            </Columns>
                        </asp:GridView>
                    </div>
                    <div id="equipmentTotalDiv" runat="server" visible="false" 
                            style="margin-top: 3px; font-size: 11px;">
                        <div id="lblFormatDiv" style="width: 60%;">
                            <%--Width of the two first gv columns--%>
                            <label style="display: inline-block; width: 440px; text-align: right;">Total:</label>
                            <asp:Label ID="equipmentTotalLbl" runat="server"
                                 style="display: inline-block; width: 90px; text-align: left;"></asp:Label>
                        </div>
                    </div>
                </ContentTemplate>

            </asp:TabPanel>

            <asp:TabPanel runat="server" ID="renewalsTab" HeaderText="Renewals">

                <ContentTemplate>

                    <div style="height: 165px;">

                       <div id="autoRenewDiv" style="margin-top: -15px;">
                                
                            <fieldset style="height: 135px;">
                                <legend>Auto Renew</legend>
                                <div id="monthlyRenewalDiv" class="orderRenewalDivs">
                                            <div>
                                                <label class="renewalDropdownLbl">Monthly: </label>
                                                <asp:DropDownList ID="renewalMonthlyDropdown" runat="server" Width="110px"
                                                                onchange="setRenewTypeRadioState();">
                                                </asp:DropDownList>

                                                <label>Renew:</label>
                                                <asp:CheckBox ID="monthlyRenewalChk" onclick="setRenewTypeRadioState();" runat="server" />
                                            </div>
                                        </div>

                                <div id="cashRenewalDiv" class="orderRenewalDivs">
                                            <div>
                                                <label class="renewalDropdownLbl">Cash: </label>
                                                <asp:DropDownList ID="renewalCashDropdown" runat="server" Width="110px"
                                                                onchange="setRenewTypeRadioState();">
                                                </asp:DropDownList>

                                                <label>Renew:</label>
                                                <asp:CheckBox ID="cashRenewalChk" onclick="setRenewTypeRadioState();" runat="server"/>
                                            </div>
                                        </div>

                                <div id="intlRenewalDiv" class="orderRenewalDivs">
                                            <div>
                                                <label class="renewalDropdownLbl">Intl: </label>
                                                <asp:DropDownList ID="renewalIntlDropdown" runat="server" Width="110px"
                                                                onchange="setRenewTypeRadioState();">
                                                     <asp:ListItem Value="0.00">None</asp:ListItem>
                                                     <asp:ListItem Value="7.00">$7.00</asp:ListItem>
                                                     <asp:ListItem Value="14.00">$14.00</asp:ListItem>
                                                     <asp:ListItem Value="21.00">$21.00</asp:ListItem> 
                                                </asp:DropDownList>

                                                <label>Renew:</label>
                                                <asp:CheckBox ID="intlRenewalChk" onclick="setRenewTypeRadioState();" runat="server" />
                                            </div>

                                        </div>

                                <div style="margin-top: 5px;">
                                    <div id="setRenewalChargeTypeDiv" style="width: 70%; float: left;">
                                        <span style="width: 100px; float: left; padding-top: 3px;">Charge Type</span>

                                        <asp:RadioButtonList ID="renewChargeTypeRadio" runat="server"
                                            RepeatDirection="Horizontal" style=" float: left;" >
                                            <asp:ListItem Value="1">CC Only</asp:ListItem>
                                            <asp:ListItem Value="2">Agent Only</asp:ListItem>
                                            <asp:ListItem Value="3">CC With Agent Backup</asp:ListItem>
                                        </asp:RadioButtonList>

                                    </div>

                                    <div style="width: 25%; float: right;">
                                        <input id="saveRenewalsBtn" type="button" style="float: right;"
                                                value="Set Renewals" onclick="return SaveRenewals();" />
                                    </div>
                                </div>

                            </fieldset>

                        </div>

                    </div>

                </ContentTemplate>
            </asp:TabPanel>
                      
            <asp:TabPanel runat="server" ID="TabPanel6" HeaderText="Call Details">

                <ContentTemplate>
                    <div>
                        <div id="callDetailDatesDiv" style="margin-bottom: 5px;">
                         
                         <asp:HiddenField ID="txtFrom" runat="server" EnableViewState="false" OnValueChanged="GetVerizonCdr" />
                         <asp:HiddenField ID="txtTo" runat="server" EnableViewState="false"  OnValueChanged="GetVerizonCdr"/>
                         <%--<asp:TextBox ID="txtFrom" runat="server" OnTextChanged="GetVerizonCdr" ></asp:TextBox>
                          <asp:TextBox ID="txtTo" runat="server"  OnTextChanged="GetVerizonCdr"></asp:TextBox>--%>
                          <asp:HiddenField ID="useCalendar"  runat="server" EnableViewState="false"  onValueChanged="GetVerizonCdr"/>
                         
                        
                            <span style="display: inline-block; width: 300px;">
                            <label>From:</label>
                                <asp:Label ID="callDetailFromLbl" runat="server" OnTextChanged="GetVerizonCdr"></asp:Label>
                           <asp:Button ID="showCalendar1" runat="server" Class="btnCalendar"   />
                           
                            </span>

                            <span style="display: inline-block; width: 300px;">
                                <label>To:</label>
                                <asp:Label ID="callDetailToLbl" runat="server" OnTextChanged="GetVerizonCdr" ></asp:Label>
                                <asp:Button ID="showCalendar2" runat="server" Class="btnCalendar" />
                                 
                              
                            </span>
                        
                      
                        </div>
                        <div style="overflow:auto; width: 900px; height: 90%;">
       
                                 <asp:GridView ID="orderCallDetails" runat="server" AutoGenerateColumns="true"  ShowHeader="true"
                                               HeaderStyle-Wrap="false" RowStyle-Wrap="false"
                                                      AllowPaging="true" PageSize="10" Font-Size="11px"  Width="100%" 
                                                      EmptyDataText="This order doesn't have any calls." >

                                            <PagerSettings Mode="NumericFirstLast" FirstPageText="First" LastPageText="Last"/>

                                 </asp:GridView>

                            </div>

                            <div>
                                <div style="margin: 10px;">

                                    <div style="display: inline;">

                                        <label style="font-weight: bold;">Incoming Minutes:</label>
                                        <asp:Label ID="inMinutesForTab" runat="server" Width="50" Text="0"></asp:Label>
                                
                                        <label style="font-weight: bold;">Outgoing Minutes:</label>
                                        <asp:Label ID="outMinutesForTab" runat="server" Width="50" Text="0"></asp:Label>

                                        <label style="font-weight: bold;">Total Minutes:</label>
                                        <asp:Label ID="totalMinutesForTab" runat="server" Width="50" Text="0"></asp:Label>

                                    </div>

                                    <div style="display: block; height: 20px;">
                                        <input id="downloadCdrBtn" type="button" style=" float: right;" value="Download"
                                                runat="server" onserverclick="downloadCdrBtn_Click" />
                                    </div>

                                </div>

                                <hr />
                            
                                <div style="text-align: center; margin: 5px;">
                                    <asp:HiddenField ID="hdnNumOfCallDetailsIntervals" runat="server"  />
                                                     
                                    <asp:LinkButton ID="prevCallDetails" runat="server" Enabled="false">&laquo; Previous Cycle</asp:LinkButton>
                                    <asp:HiddenField ID="callDetailFromIndex" runat="server" Value="1" />

                                    <span style="display: inline-block; width: 50px;"></span>

                                    <asp:LinkButton ID="nextCallDetail" runat="server" Enabled="false">Next Cycle &raquo;</asp:LinkButton>
                                    <asp:HiddenField ID="callDetailEndIndex" runat="server" Value="0" />
                                </div>

                            </div>

                        </div>

                </ContentTemplate>
            </asp:TabPanel>

            <asp:TabPanel ID="intlCallsTab" OnClientClick="refreshIntlCalls" runat="server" HeaderText="Intl Calls">
                <ContentTemplate>
                    <asp:UpdatePanel ID="intlCallsTabDiv" runat="server">
                        <ContentTemplate>
                            <asp:GridView ID="intlCallsGridView" runat="server" DataSourceID="intlCallSqlDataSource"
                                        AutoGenerateColumns="false" AllowPaging="true" PageSize="20" Width="100%"
                                        Font-Size="11px" ShowHeaderWhenEmpty="true">
                                <Columns>
                                    <asp:BoundField DataField="CallDate" HeaderText="Date" SortExpression="CallDate" />
                                    <asp:BoundField DataField="CalledFrom" HeaderText="Called From" SortExpression="CalledFrom" />
                                    <asp:BoundField DataField="DialedTo" HeaderText="Dialed To" SortExpression="DialedTo" />
                                    <asp:BoundField DataField="CallCost" DataFormatString="{0:c}" HtmlEncode="false" HeaderText="Cost" SortExpression="CallCost" />
                                    <asp:BoundField DataField="Duration" DataFormatString="{0:N0}" HtmlEncode="False" HeaderText="Minutes" SortExpression="Duration" />
                                    <asp:BoundField DataField="DNIS" HeaderText="DNIS" SortExpression="DNIS" />
                                    <asp:BoundField DataField="Retrieved" HeaderText="Retrieved" SortExpression="Retrieved" />
                                </Columns>
                            </asp:GridView>
                            <asp:SqlDataSource ID="intlCallSqlDataSource" runat="server"></asp:SqlDataSource>
                            <%--Hack to reload gridview; upon tab selection javascript triggers a click on this button
                                which refreshes the update panel--%>
                            <asp:Button ID="refreshIntlCallGV" runat="server" style="display: none;" />
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    
                </ContentTemplate>
            </asp:TabPanel>

            <asp:TabPanel ID="notesTab" runat="server" HeaderText="Notes">
                
                <ContentTemplate>
                    <div id="notesTabMainDiv">

                        <div style="height: 40px;">
                            <h2>Order Notes</h2>
                        </div>
                        
                        <div id="editArea">
                            <textarea id="orderNoteTextArea" style="width: 99%;" cols="100" rows="8" runat="server"></textarea>
                        </div>

                        <div style="margin: 5px 0px 0px 0px;height: 25px;">
                            <asp:Button ID="saveOrderNoteBtn" OnCommand="SaveOrderNote" runat="server" Text="Save" style="float: right;" />
                        </div>

                    </div>
                </ContentTemplate>

            </asp:TabPanel>

            <asp:TabPanel ID="activityTab" runat="server" HeaderText="Activity" OnClientClick="controlFreezeHeader">
                <ContentTemplate>
                    <div id="activityGvWrapper" style="font-size: 11px;">
                        <asp:GridView ID="activityGv" runat="server" Width="100%" ShowHeaderWhenEmpty="true"
                            HeaderStyle-Wrap="false" RowStyle-Wrap="false"
                            DataSourceID="activityDataSource" AutoGenerateColumns="false">
                            <Columns>
                                <asp:BoundField DataField="activity" HeaderText="Activity" SortExpression="activity" />
                                <asp:BoundField DataField="Agent" HeaderText="Agent" SortExpression="Agent" />
                                <asp:BoundField DataField="Date" HeaderText="Date" SortExpression="Date" />
                                <asp:BoundField DataField="ChargeType" HeaderText="Method" SortExpression="ChargeType" />
                                <asp:BoundField DataField="balance" HeaderText="Balance" DataFormatString="{0:c}" HtmlEncode="False" SortExpression="balance" />
                                <%--Note: white-space: nowrap; isn't effective in IE7 which breaks the frozen headers, so we give it a width.--%>
                                <asp:BoundField DataField="PlanType" HeaderText="Plan Type" SortExpression="PlanType" ItemStyle-Width="75px" />
                                <asp:BoundField DataField="PlanExp" HeaderText="Plan Exp" DataFormatString="{0:MM/dd/yyyy}" HtmlEncode="False" SortExpression="PlanExp" />                         
                                <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="{0:c}" HtmlEncode="False" SortExpression="Amount" />
                                <asp:BoundField DataField="TransactionId" HeaderText="Trans Id" SortExpression="TransactionId" />
                                <asp:BoundField DataField="Pin" HeaderText="Pin" SortExpression="Pin" />
                                <asp:BoundField DataField="Control" HeaderText="Control" SortExpression="Control" />
                                <asp:BoundField DataField="Status" HeaderText="Status" SortExpression="Status" HtmlEncode="False"
                                     DataFormatString="&lt;div style=&quot;text-transform:capitalize&quot;&gt;{0}&lt;/div&gt;" />
                            </Columns>
                        </asp:GridView>
                        <asp:SqlDataSource ID="activityDataSource" runat="server"
                            ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>" >
                        </asp:SqlDataSource>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>

            <asp:TabPanel ID="verRegisterTab" runat="server" HeaderText="Summary">
                <ContentTemplate>
                    <div style="overflow: auto; min-height: 0%;">
                        <asp:SqlDataSource ID="verRegisterSqlDataSrc"
                            ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>"
                            runat="server"></asp:SqlDataSource>
                        <asp:GridView ID="verRegisterGv" runat="server" DataSourceID="verRegisterSqlDataSrc"
                            AllowPaging="true" PageSize="25" Width="100%"
                            Font-Size="11px" HeaderStyle-Wrap="false" RowStyle-Wrap="false">
                        </asp:GridView>
                    </div>
                </ContentTemplate>
            </asp:TabPanel>

            <asp:TabPanel ID="administrationTab" runat="server" HeaderText="Admin">
                <ContentTemplate>
                    <div>
                        
                        <div id="selectAdminTaskDiv">
                            <fieldset style="margin-top: 0px;">
                                <legend>Tasks</legend>
                                <asp:RadioButtonList ID="adminTasksRadiolist" runat="server" TextAlign="Right">
                                   <%-- <asp:ListItem Text="Convert to Verizon" Value="convertVER"></asp:ListItem>--%>
                                   <asp:ListItem Text="Back-End Converted to Concord" Value="convertedToCON"></asp:ListItem>
                                    
                                   <%-- <asp:ListItem Text="Convert to All Talk" Value="convertAT"></asp:ListItem>--%>
                                   <%-- <asp:ListItem Text="All Talk Provision" Value ="ATprovision"></asp:ListItem>--%>
                                    <asp:ListItem Text="Disconnect" Value="disconnect"></asp:ListItem>
                                    <asp:ListItem Text="Convert to Page Plus" Value="convertPP"></asp:ListItem>
                                   <%-- <asp:ListItem Text="Convert to New Verizon MDN" Value="newVER"></asp:ListItem>--%>
                                    <asp:ListItem Text="Back-End Converted to Telco" Value="convertTELCO"></asp:ListItem>
                                    <asp:ListItem Text="Early Renewal" Value="updateStack"></asp:ListItem>
                                    <asp:ListItem Text="Decrement Stack" Value="removeStack"></asp:ListItem>
                                    <asp:ListItem Text="Refund Payment" Value="refund"></asp:ListItem>
                                    
                                   <%-- <asp:ListItem Text="Convert stacked to Concord" Value="ConvertStackToConcord"></asp:ListItem>--%>
                                </asp:RadioButtonList>
                            </fieldset>
                        </div>

                        <div>
                            <asp:Button ID="executeAdminTasks" runat="server" Text="Execute" style="float: right;" />
                        </div>
                        <span id="adminErrorMsg" runat="server" style="color: Red; float: right; padding-right: 15px;"></span>
                        <div>&nbsp;</div>
                        </div>

                        <div id="displayRefundDiv" runat="server" visible="false" style="overflow: auto; min-height: 0%; width:100%">
                            <fieldset style="margin-top: 0px; margin-right: 0px;">
                                <legend>Recent Payment</legend>
<div>
                                <asp:GridView ID="refundtransgridview" runat="server" AutoGenerateColumns="false" 
                                    RowStyle-CssClass="highlightedGvRow" AllowSorting="true" AllowPaging="True" PageSize="25"
                                            ShowHeader="true" Width="100%" DataSourceID="RefundDataSource" Font-Size="11px" >
                                <Columns>
                                    <asp:BoundField DataField="trans_type" HeaderText="Type" SortExpression="trans_type"/>
                                    <asp:BoundField DataField="user" HeaderText="User" SortExpression="user" />
                                    <asp:BoundField DataField="agent" HeaderText="Agent" SortExpression="agent" />
                                    <asp:BoundField DataField="paydate" HeaderText="Pay Date" SortExpression="paydate" />
                                    <asp:BoundField DataField="monthly_amt" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Monthly" SortExpression="monthly_amt" />
                                    <asp:BoundField DataField="cash_amt" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Cash" SortExpression="cash_amt" />
                                    <asp:BoundField DataField="intl_amt" DataFormatString="{0:c}" HtmlEncode="false" HeaderText="Intl" SortExpression="intl_amt" />
                                    <asp:BoundField DataField="item_amt" DataFormatString="{0:c}" HtmlEncode="false" HeaderText="Items" SortExpression="item_amt" />
                                    <asp:BoundField DataField="authmessage" HeaderText="Authorization Message" SortExpression="authmessage" />
                                    <asp:BoundField DataField="total" DataFormatString="{0:c}" HtmlEncode="False" HeaderText="Total" SortExpression="total" />
                                </Columns>
                                    
                            </asp:GridView>

                            <asp:SqlDataSource ID="RefundDataSource" runat="server" CancelSelectOnNullParameter="false"></asp:SqlDataSource>
    </div>
                                <div>&nbsp;</div>
                                 <div>
                                     <asp:Label ID="errorLbl" runat="server" Font-Bold="true" Font-Names="Arial" Font-Size="9pt"
                                            ForeColor="black"></asp:Label>
                          
                                     <div id="refundInfoDiv" runat="server">
                                     <label for="PayDate">Refund Date:</label>
                                     <asp:Label ID="Label1" runat="server" ></asp:Label>
                                     <label for="Amount">   Amount:</label>
                                     <asp:TextBox ID="rfAmount" runat="server" ></asp:TextBox>
                                         <asp:HiddenField ID="transIdHF" runat="server" />
                                         </div>
                            <asp:Button ID="RefundBtn" runat="server" Text="Refund" style="float: right;" />
                        </div>
        </div>

                     
                    
                </ContentTemplate>
            </asp:TabPanel>            

        </asp:TabContainer>

        <asp:HiddenField ID="hdnFieldChanged" runat="server" />
        
        <div style="margin: 5px 0px 5px 0px; height: 20px;">
            
            <asp:Button ID="closeOrderButton" runat="server" Text="Lock Order" 
                   OnCommand="ToggleOrderState" CommandName="close" style="float: right;" 
                   ToolTip="Set order to be disabled." />

            <span id="toggleOrderErrorMsg" runat="server" style="color: Red; float: right; padding-right: 15px;"></span>

        </div>

    </div>
     
     
     
     
     

      <script type="text/javascript">    
  




         Calendar.setup(
        {
            inputField: "txtFrom",
//            ifFormat: "%Y-%m-%d",
            button: "showCalendar1",
           // date: new Date(yr, mo, dy),
            onSelect: function () {
                this.eventName = "from";
                dateChanged(this);
            }
        }
      );
        Calendar.setup(
        {
            inputField: "txtTo",
//            ifFormat: "%Y-%m-%d",
            button: "showCalendar2",
            // date: new Date(yr, mo, dy),
            onSelect: function () {
                this.eventName = "to";
                dateChanged(this);
            }
        }
      );
        
</script>

    
</asp:Content>