// Generic functions.

// Taken from RemoteLandlord/PayablesBanking.aspx
function StripMoney(val) {
    val = val.replace(/\$/g, '').replace(/\s/g, '').replace(/,/g, '');
    return val;
}

// Taken from RemoteLandlord/PayablesBanking.aspx
function FormatMoney(amount) {
    // returns the amount in the .99 format 
    amount -= 0;
    amount = (Math.round(amount * 100)) / 100;
    return AddComas((amount == Math.floor(amount)) ? amount + '.00' : ((amount * 10 == Math.floor(amount * 10)) ? amount + '0' : amount));

}

// Taken from RemoteLandlord/PayablesBanking.aspx
function AddComas(str) {
    str = str + '';

    var rxSplit = new RegExp('([0-9])([0-9][0-9][0-9][,.])');

    var arrNumber = str.split('.');
    
    arrNumber[0] += '.';

    do {
        arrNumber[0] = arrNumber[0].replace(rxSplit, '$1,$2');
    } while (rxSplit.test(arrNumber[0]));

    var amt = arrNumber.join('');
    if (amt.charAt(0) == '-')
        amt = '-$' + amt.substring(1, amt.length);
    else
        amt = '$' + amt;

    return amt
}

function nextElementNode(elem) {
    do {
        elem = elem.nextSibling;
    } while (elem && elem.nodeType != 1);   // Nodetype 1 is a element node.
    return elem;
}

function isValidMoneyAmnt(value) {
    value = StripMoney(value);
    return !isNaN(parseFloat(value)) && isFinite(value);
}

Array.prototype.inArray =  function ( val ) {
    for( var i = 0; i < this.length; i++ ) {
        if( val == this[ i ] ) {
            return true;
        }
    }
    return false;
}

var getElemsByClass = function ( className, context ) {
    'use strict';
    var classArr = [], htmlCol = [];
    context = context || document.body;

    function traverse( className, context ) {
        var arr;
        for ( var child = 0; child < context.children.length; child++ ) {
            // Check if it has the class.
            // (The Array.inArray is dependant on the definition in this file)
            if ( context.children[child].className.split( ' ' ).inArray( className ) ) {
                classArr.push( context.children[child] );
            }

            // Check if there are children.
            if ( context.children[child].children.length > 0 ) {
                traverse( className, context.children[child] );
            }
        }
    }

    if ( typeof context.getElementsByClassName !== 'undefined' ) {
        // Native getElementsByClassName return HTMLCollections.
        // Standardize it by always returning an array.
        htmlCol = context.getElementsByClassName( className );
        for ( var i = 0; i < htmlCol.length; i++ ) {
            classArr.push( htmlCol[i] );
        }
    } else {
        traverse( className, context );
    }

    return classArr;
}

var str2Json = function ( str ) {
    // This way of parsing a string to JSON taken from JQuery
    if ( window.JSON && window.JSON.parse ) {
        return JSON.parse( str );
    } else {
        return ( new Function( "return " + str ) )();
    }
}



// Non-generic functions

// Admin.aspx - users
function GetUserInfo(userId) {
    var args = "GetUserDetails:" + userId;
    UseCallBack(args, "GetUserInfo");
}

function populateUserFields( str ) {
    
    var json = str2Json( str );

    document.getElementById( "userNameEditFld" ).value = json.fullName;
    document.getElementById( "userIdEditFld" ).value = json.userName;
    document.getElementById( "hiddenUpdateUserId" ).value = json.userName;
    document.getElementById( "emailEditFld" ).value = json.email;

    if ( json.level !== '' ) {
        document.getElementById( "userLevelDropdown" ).value = json.level;
    } else {
        document.getElementById("userLevelDropdown").selectedIndex = 2 // Default to lowest user level
    }

    if ( json.comPlan !== '' ) {
        document.getElementById( "commissionPlansDrop" ).value = json.comPlan;
    } else {
        document.getElementById( "commissionPlansDrop" ).selectedIndex = 0 // Default to lowest user level
    }

    document.getElementById( "monitorAgent" ).checked = parseInt( json.monitor, 10 );
    document.getElementById( "creditLimitField" ).value = json.creditLimit === '' ? 0 :
        Math.round( parseFloat( json.creditLimit ) * 100 ) / 100;
    
    // Pass a parameter indicating whether to show the Apply Commissions button
    var showAc = ( json.comPlan != '' && json.comPlan.toLowerCase() != 'no commission' && parseInt( json.comNum ) == 0 );
    toggleUpdateUserBtnDiv( showAc );
    togglePasswordFields("none");
    clearUserErrorMessages();
    setUpdatableFields(true);

}

function toggleUpdateUserBtnDiv( showAc ) {
    
    // Note: When updating user the user id is maintained by the code behind.
    var userId = document.getElementById( "userIdEditFld" ).value;
    if ( typeof showAc != 'undefined' ) {
        document.getElementById( "retroCommissionsBtn" ).style.display = ( showAc === true ) ? 'inline' : 'none';
    }
     
    if (userId.length > 0) {
        document.getElementById("createUserBtnDiv").style.display = "none";
        document.getElementById("updateUserBtnDiv").style.display = "inline";
    } else {
        document.getElementById("updateUserBtnDiv").style.display = "none";
        document.getElementById("createUserBtnDiv").style.display = "inline";
    }

}

function togglePasswordFields(isShow) {
    document.getElementById("passwordFieldsContainer").style.display = isShow;
}

function setUpdatableFields(updatable) {
    document.getElementById("userIdEditFld").disabled = updatable;
}

function createUser() {
    
    var hasError = false;

    if (!requireUserId()) {
        hasError = true;
    }

    if (!requireEmail()) {
        hasError = true;
    }

    if (!requirePassword()) {
        hasError = true;
    } else if (!isPasswordRequiredLength()) {
        hasError = true;
    }

    if (!isPasswordConfirmed()) {
        hasError = true;
    } else if (!isPasswordConfirmMatch()) {
        hasError = true;
    }

    if (!isCreditLimitValid()) {
        return false;
    }

    if (hasError) {
        return false;
    }

    return true;

}

function requireUserId() {
    // User id is required
    var userId = document.getElementById("userIdEditFld").value;
    document.getElementById("userIdErrorMsg").innerText = "";
    if (userId.length <= 0) {
        document.getElementById("userIdErrorMsg").innerText = "User Id is required.";
        return false;
    }
    return true;
}

function requireEmail() {
    // Email is required
    var email = document.getElementById("emailEditFld").value;
    document.getElementById("emailErrorMsg").innerText = "";
    if (email.length <= 0) {
        document.getElementById("emailErrorMsg").innerText = "E-mail is required.";
        return false;
    }
    return true;
}

function requirePassword(showError) {

    if (typeof showError === "undefined") {
        showError = true;
    }

    // Password is required
    var password = document.getElementById("passwordEditFld").value;
    document.getElementById("passwordErrorMsg").innerText = "";
    if (password.length <= 0) {
        if (showError) {
            document.getElementById("passwordErrorMsg").innerText = "Password is required.";
        }
        return false;
    }
    return true;
}

function isPasswordRequiredLength() {
    // Password is required to have a minimum length
    var password = document.getElementById("passwordEditFld").value;

    var minPwLen = getPasswordMinLength();
    document.getElementById("passwordErrorMsg").innerText = "";
    if (password.length < minPwLen) {
        document.getElementById("passwordErrorMsg").innerText = "Password must be at least " + minPwLen + " characters.";
        return false;
    }

    return true;

}

function getPasswordMinLength() {
    var minLength = parseInt(document.getElementById("pwLenSpan").innerText);

    if (isNaN(minLength)) {
        return false;
    }

    return minLength;
}

function isPasswordConfirmed() {
    // Password confirm is required
    var password = document.getElementById("confirmPassword").value;
    document.getElementById("confirmPasswordErrorMsg").innerText = "";
    if (password.length <= 0) {
        document.getElementById("confirmPasswordErrorMsg").innerText = "Confirm Password is required.";
        return false;
    }
    return true;
}

function isPasswordConfirmMatch() {
    // Matchup passwords
    var mainPassword = document.getElementById("passwordEditFld").value;
    var confirmPassword = document.getElementById("confirmPassword").value;
    document.getElementById("confirmPasswordErrorMsg").innerText = "";

    if (mainPassword != confirmPassword) {
        document.getElementById("confirmPasswordErrorMsg").innerText = "The Password and Confirmation Password must match.";
        return false;
    }

    return true;

}

function isCreditLimitValid() {
    var creditLimit = document.getElementById("creditLimitField").value;
    
    document.getElementById("creditLimitErrorMsg").innerText = "";
    if (creditLimit.length > 0) {
        if (!isValidMoneyAmnt(creditLimit)) {
            document.getElementById("creditLimitErrorMsg").innerText = "Invalid dollar amount.";
            return false;
        }
    }

    return true;
}

function clearUserErrorMessages() {

    var manageUsersMsgLbl = document.getElementById("manageUsersErrorMsgDiv").innerText;
    if (manageUsersMsgLbl != null && manageUsersMsgLbl.length > 0) {
        document.getElementById("manageUsersErrorMsgDiv").innerText = ""; 
    }

    document.getElementById("userIdErrorMsg").innerText = "";
    document.getElementById("emailErrorMsg").innerText = "";
    document.getElementById("passwordErrorMsg").innerText = "";
    document.getElementById("confirmPasswordErrorMsg").innerText = "";
    document.getElementById("creditLimitErrorMsg").innerText = "";

}

function RemoveUser() {

    if (!requireUserId()) {
        return false;
    }    

    if (!confirm('This user will be permanently deleted.\nAre you sure you want to continue?')) {
        return false;
    }

    return true;

}

function updateUser() {
    var hasError = false;

    if (!requireUserId()) {
        hasError = true;
    }

    if (!isCreditLimitValid()) {
        return false;
    }

    if (hasError) {
        return false;
    }

    return true;
}

function clearUserFields() {

    document.getElementById("userNameEditFld").value = "";
    document.getElementById( "userIdEditFld" ).value = "";
    document.getElementById( "hiddenUpdateUserId" ).value = "";
    document.getElementById("passwordEditFld").value = "";
    document.getElementById("confirmPassword").value = "";
    document.getElementById("emailEditFld").value = "";
    document.getElementById( "userLevelDropdown" ).selectedIndex = 2;  // Default to lowest user level
    document.getElementById( "commissionPlansDrop" ).selectedIndex = 0;
    document.getElementById("monitorAgent").checked = true;
    document.getElementById("creditLimitField").value = ""

    clearUserErrorMessages();
    toggleUpdateUserBtnDiv();
    setUpdatableFields(false);
    togglePasswordFields("block");

}

function ResetPassword() {

    document.getElementById("resetPwErrorMsg").innerText = "";

    var newPw = document.getElementById("resetPwField").value;
    var confirmNewPw = document.getElementById("resetConfirmPassword").value;

    var pwMinLength = getPasswordMinLength();

    if (newPw.length < pwMinLength) {
        document.getElementById("resetPwErrorMsg").innerText = "Passwords need to be a minimum of " + pwMinLength + " characters long.";
        return false;
    }

    if (confirmNewPw.length <= 0) {
        document.getElementById("resetPwErrorMsg").innerText = "Please confirm the password.";
        return false;
    }

    if (newPw != confirmNewPw) {
        document.getElementById("resetPwErrorMsg").innerText = "The passwords don't match.";
        return false;
    }

    return true;

}

function ClearResetPanel() {

    document.getElementById("resetPwErrorMsg").innerText = "";

    document.getElementById("resetPwField").value = "";
    document.getElementById("resetConfirmPassword").value = "";

}



// Admin.aspx - ESN
function PopulateEsnInfo(serial, esn, intl, cusPin) {

    document.getElementById("manageSerialFld").value = serial;
    document.getElementById("originalSerial").value = serial;
    
    document.getElementById("manageEsnFld").value    = esn;
    document.getElementById("manageIntlFld").value   = intl;
    document.getElementById("manageCusPinFld").value = cusPin;

    document.getElementById("manageEsnErrorMsg").innerText = "";

    toggleUpdateEsnBtnDiv();

}

function toggleUpdateEsnBtnDiv() {
    var serial = document.getElementById("manageSerialFld").value;

    if (serial.length > 0) {
        document.getElementById("createEsnDiv").style.display = "none";
        document.getElementById("udEsnDiv").style.display = "inline";
    } else {
        document.getElementById("udEsnDiv").style.display = "none";
        document.getElementById("createEsnDiv").style.display = "inline";
    }

}

function clearManageEsnFields() {

    document.getElementById("manageSerialFld").value = "";
    document.getElementById("manageEsnFld").value    = "";
    document.getElementById("manageIntlFld").value   = "";
    document.getElementById("manageCusPinFld").value = "";

    document.getElementById("manageEsnErrorMsg").innerText = "";

    toggleUpdateEsnBtnDiv();

}

function requireAllEsnFields() {

    var serial = document.getElementById("manageSerialFld").value;
    var esn    = document.getElementById("manageEsnFld").value;
    var intl   = document.getElementById("manageIntlFld").value;
    var cusPin = document.getElementById("manageCusPinFld").value;

    var allow = false;

    if (serial.length <= 0) {
        allow = false;
    } else if (esn.length <= 0) {
        allow = false;
    } else if (intl.length <= 0) {
        allow = false;
    } else if (cusPin.length <= 0) {
        allow = false;
    } else {
        allow = true;
    }

    if (!allow) {
        document.getElementById("manageEsnErrorMsg").innerText = "All fields required.";
    }

    // Test that Intl and cusomer pin are numeric values (thats how they are stored in th db).
    if (isNaN(parseFloat(intl))) {
        document.getElementById("manageEsnErrorMsg").innerText = "International must be a valid number.";
        return false;
    }

    if (isNaN(parseFloat(cusPin))) {
        document.getElementById("manageEsnErrorMsg").innerText = "Customer pin must be a valid number.";
        return false;
    }

    return allow;

}

function deleteEsn() {

    var serial = document.getElementById("manageSerialFld").value;

    if (serial.length <= 0) {
        document.getElementById("manageEsnErrorMsg").innerText = "Serial number required.";
        return false;
    }

    return confirm("Are you sure you want to permanently delete this ESN?");

}

// Admin.aspx - Pins & ESN
function validateUpload( fileUploadId, errorElemId, validExtensions ) {
    var file = document.getElementById( fileUploadId );

    if (!file) return false;

    if(isFileEmpty(file)) {
        document.getElementById(errorElemId).innerText = "No file uploaded.";
        return false;
    }

    if ( !validateFileExt( file, validExtensions ) ) {
        document.getElementById(errorElemId).innerText = "Only " + validExtensions.join( ', ' ) + " files allowed.";
        return false;
    }

    return true;

}

function isFileEmpty(file) {
    return file.value.length <= 0 ? true : false;
}

function validateFileExt( file, validExtensions ) {
    validExtensions = validExtensions.constructor === Array ? validExtensions : [ validExtensions ];
    return validExtensions.inArray( file.value.split( '.' ).pop() ) ? true : false;
}


// Admin.aspx - Accounts
function GetAccountInfo(accountId) {
    var args = "GetAccountDetails:" + accountId;
    UseCallBack(args, "GetAccountInfo");
}

function populateAccountFields(xml) {

    var obj = new ActiveXObject("MsXml2.DOMDocument");
    obj.loadXML(xml);

    var accountName = obj.getElementsByTagName("AccountName");
    if (accountName.length > 0) {
        document.getElementById("editAccountNameFld").value = accountName[0].text
        document.getElementById("hiddenAccountName").value = accountName[0].text
    } else {
        document.getElementById("editAccountNameFld").value = ""
        document.getElementById("hiddenAccountName").value = ""
    }

    var password = obj.getElementsByTagName("Password");
    if (password.length > 0) {
        document.getElementById("editPasswordFld").value = password[0].text
    } else {
        document.getElementById("editPasswordFld").value = ""
    }

    var accountActive = obj.getElementsByTagName("Active");
    if (accountActive.length > 0) {
        document.getElementById("accountActiveChk").checked = accountActive[0].text == 'true' ? true : false;
    } else {
        document.getElementById("accountActiveChk").checked = true;
    }

    toggleUpdateAccountBtnDiv();
    clearAccountErrorMessages();

    var id = obj.getElementsByTagName("id");
    if (id.length > 0) {
        document.getElementById("hiddenAccountId").value = id[0].text
    } else {
        document.getElementById("hiddenAccountId").value = ""
    }

}

function toggleUpdateAccountBtnDiv() {

    var accountName = document.getElementById("editAccountNameFld").value;
    if (accountName.length > 0) {
        document.getElementById("updateAccountBtnDiv").style.display = "inline";
    } else {
        document.getElementById("updateAccountBtnDiv").style.display = "none";
    }

}

function CreateAccount() {

    var hasError = false;

    if (!requireAccountName()) {
        hasError = true;
    }

    if (!requireAccountPassword()) {
        hasError = true;
    }

    if (hasError) {
        return false;
    }

    return true;

}

function UpdateAccount() {

    var hasError = false;

    if (!requireAccountName()) {
        hasError = true;
    }

    if (!requireAccountPassword()) {
        hasError = true;
    }

    if (hasError) {
        return false;
    }

    return true;

}

function ClearAccountFields() {

    document.getElementById("editAccountNameFld").value = "";
    document.getElementById("editPasswordFld").value = "";
    document.getElementById("accountActiveChk").checked = true;

    document.getElementById("hiddenAccountName").value = "";
    document.getElementById("hiddenAccountId").value = ""

    clearAccountErrorMessages();
    toggleUpdateAccountBtnDiv();

}

function clearAccountErrorMessages() {

    document.getElementById("editAccoutsErrorMsg").innerText = "";
    document.getElementById("accountNameMsgLbl").innerText = "";
    document.getElementById("accountPasswordMsgLbl").innerText = "";

}

function requireAccountName() {
    var accountName = document.getElementById("editAccountNameFld").value;
    document.getElementById("accountNameMsgLbl").innerText = "";
    if (accountName.length <= 0) {
        document.getElementById("accountNameMsgLbl").innerText = "Account name is required.";
        return false;
    }
    return true;
}

function requireAccountPassword() {
    var pw = document.getElementById("editPasswordFld").value;
    document.getElementById("accountPasswordMsgLbl").innerText = "";
    if (pw.length <= 0) {
        document.getElementById("accountPasswordMsgLbl").innerText = "Account password is required.";
        return false;
    }
    return true;
}

function validateMiscValues() {

    var miscName = document.getElementById("miscNameFld").value;
    var miscCost = document.getElementById("miscCostFld").value;

    if (miscName.length > 0 && miscCost.length <= 0) {
        return false;
    } else if (miscName.length <= 0 && miscCost.length > 0) {
        return false;
    } else if (miscName.length > 0 && miscCost.length > 0) {
        if (!isValidMoneyAmnt(miscCost)) {
            return false;
        }
    }

    return true;

}

//cmb 7/5/12
function fieldChanged() {
    document.getElementById("hdnFieldChanged").value = "changed";
}


function setClickBGColor( row, e ) {
    e = ( e ) ? e : window.event;
    var targ = ( e.target ) ? e.target : e.srcElement;
    if ( targ.id == "badTransDropdown" ) {
        return;
    }

    var rows = row.parentNode.getElementsByTagName("tr");
    for(var i = 0; i < rows.length; i++) {
        rows[i].style.backgroundColor = "White";
    }
    
    row.style.backgroundColor = "#fdecc9";

    document.getElementById("selectedRowIndex").value = row.rowIndex;
}

function setBackgroundWhite( row ) {
    var selectedIndex = document.getElementById("selectedRowIndex").value;
    
    if (row.rowIndex != selectedIndex) {
        row.style.background = 'White';
    }

}

function setCustomerTotal( total ) {
    document.getElementById( 'totalCustomerLbl' ).innerText = total;
}

// Order.aspx, Admin.aspx, Agent.aspx
function setTabPnlsMinHeight( minHeight  ) {
    // The class is provided by the AjaxToolkit so set it from JavaScript
    // instead of from the AjaxToolkit css.
    var tabPnls = getElemsByClass( 'ajax__tab_panel' );
    for ( var i = 0; i < tabPnls.length; i++ ) {
        tabPnls[i].style.minHeight = minHeight + 'px';
    }
}


// Order.aspx, Agent.aspx
function setupCCExpDropdown() {

    var mDrop = document.getElementById( 'creditCardExpirationMonth' ),
        yDrop = document.getElementById( 'creditCardExpirationYear' ),
        removeMonths = function ( drop, months ) {
            // To use the index we start with the highest so that
            // the index is as expected even as we remove elements.
            for ( var m = 11; m >= 0; m-- ) {
                parseInt( months[m].value, 10 ) < ( new Date().getMonth() + 1 ) ?
                drop.remove( drop.m ) : null;
            }
        },
        addMonths = function ( drop, months ) {

            if ( months.length >= 12 ) { return; }

            var stop = ( 12 - months.length );

            for ( var m = 0; m < stop; m++ ) {
                var option = document.createElement( "option" ),
                    month = ( m + 1 ).toString().length == 1 ? ( "0" + ( m + 1 ) ) : ( m + 1 );
                option.text = month;
                try {
                    drop.add( option, drop.options[m] );
                } catch ( e ) {      // < IE8
                    option.value = month;
                    drop.add( option, m );
                }
            }

        };
            
    if ( parseInt( yDrop.value ) <= ( new Date().getFullYear() % 100 ) ) {
        removeMonths( mDrop, mDrop.options );
    } else {
        addMonths( mDrop, mDrop.options );
    }

}

// Handle AJAX responses
function getInfoFromServer(str, context) {
    
    if (context == "getPlanInfoFromSerial") {
        populatePlanEsnAndPins(str);
    }

    if (context == "SetCostLblFromServer") {
        planCostLbls.setCostLbl( str );
    }

    if (context == "GetAgentCreditLimit") {
        agentCredit.setLimitLabels( str );
    }

    if (context == "SaveRenewals") {
        updateRenwalStatusBarLabels(str);
    }

    if (context == "GetUserInfo") {
        populateUserFields(str);
    }

    if (context == "GetAccountInfo") {
        populateAccountFields(str);
    }

    if (context == "isCellNumberUnique") {
        setCellNumNonuniqueMsg(str);
    }

    if ( context == "refreshCellNum" ) {
        // Defined in Pins.aspx
        if (str != "isNull") {
            setPinCellNum(str);
        }
    }

    if ( context == "refreshCustomerTotal" ) {
        setCustomerTotal( str );
    }

    if ( context == "refreshVerStatusMsg" ) {
        // Defined in Order.aspx
        refreshVerStatusMsg.refresh( str );
    }

    if ( context == "GetComPlan" ) {
        // Defined in Admin.aspx
        comPlans.populateComPlan( str );
    }

    if ( context == "getEncodedOid" ) {
        window.location = 'Order.aspx?oid=' + str;
    }
}