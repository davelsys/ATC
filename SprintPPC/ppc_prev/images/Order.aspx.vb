Imports System.Globalization
Imports System.Xml

Partial Class Order
    Inherits System.Web.UI.Page
    Implements System.Web.UI.ICallbackEventHandler

    Private connection As SqlConnection
    Private _callBackResult As String = Nothing
    Private _isNewOrder As Boolean = True
    Private Const _ppcConStr As String = "ppcConnectionString"

    ' Customer information
    Private varCustomerId As Integer = 0
    Private customer As Customer = Nothing

    ' Order (phone) information
    Private varOrderID As Integer = 0
    Private varOrderCellNumber As String = ""
    Private varEsn As String = ""
    Private varIntlPin As String = ""
    Private varSerialNumber As String = ""
    Private varCarrierName As String = vbNullString
    Private varVendorPin As String = vbNullString
    Private varMonitor As Boolean
    Private varStatus As String = vbNullString
    Private varExpirationDate As String = vbNullString
    Private varPlanExpirationDate As String = vbNullString
    Private varIsOrderClosed As Boolean = False
    Private varOrderNote As String = Nothing

    ' Call information
    Private varBalance As String = vbNullString
    Private varIncomingMinutes As String = vbNullString
    Private varOutgoingMinutes As String = vbNullString
    Private varTotalMinutes As String = vbNullString
    Private varVendorName As String = vbNullString
    Private varLastUpdate As String = vbNullString
    Private varSignupDate As Date = Nothing
    Private varStackedPinCount As Integer = 0
    Private varIntlBalance As Decimal = 0

    ' Transaction information
    Private varLastChargeMonthly As Decimal = 0
    Private varLastChargeMonthPlanID As Integer = 0

    ' Renewal information
    Private varRenewalMonthlyId As Integer = 0
    Private varIsMonthlyRenew As Boolean = False
    Private varRenewalCashId As Integer = 0
    Private varIsCashRenew As Boolean = False
    Private varRenewalIntl As Decimal = FormatNumber(0, 2)
    Private varIsIntlRenew As Boolean = False
    Private varRenewalChargeType As Integer = 0

    ' Plan information
    Private varPlanID As Integer = 0
    Private varPlanName As String = vbNullString
    Private varIsKosherPlan As Boolean = True

    ' Page initialization
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Response.Expires = -1

        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        If Session.Item("UserLevel") Is Nothing Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        If Session.Item("UserLevel") <> 1 Then
            closeOrderButton.Visible = False
        End If

        varMonitor = Session("UserMonitor")

        connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        Dim orderID = Request.QueryString("oid")
        If orderID IsNot Nothing Then
            _isNewOrder = False
            varOrderID = orderID
        End If

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

        If Not IsPostBack Then
            InitializePage()
        Else
            GetOrderData()
            ToggleCloseBtn()
        End If

        If Not HasPermission() Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        SetupVendorPinField()

        ' Clear the error messages.
        authNoteLbl.Text = ""
        toggleOrderErrorMsg.InnerText = ""

    End Sub

    Private Sub InitializePage()

        If _isNewOrder Then
            closeOrderButton.Visible = False
            SetSaveOrderBtnProperties("Save")
            DisableTabs()
            SetUpMonitorCheckBox(True)
            ToggleConfirmSerial()
            ToggleConfirmCellNumber()
            SetupInitialAgentDropdown()
        Else
            ' Initialize page components.
            SetSaveOrderBtnProperties("Update")
            ' Set how to display sales reps depending on user level.
            SetupSalesRep()

            PopulateAllPlanDropdowns()
            PopulateItemsList()
            SetUpCCExpDropdowns()

            GetOrderData()

            ' These function are order dependent so they work only after the GetOrderData call.
            SetOrderInfoBar()
            PopulateOrderForm()
            SetUpMonitorCheckBox(varIsKosherPlan)
            PopulateRenewals()
            SetSaveNoteBtnProps()
            SetupNumberOfCallDetailsIntervals()
            ' BindCallDetailGridview uses the value set in SetupNumberOfCallDetailsIntervals.
            BindCallDetailGridview()
            BindIntlCallsGridView()
            BindTransactionHistoryGrid()
            BindEquipmentGridview()
            ToggleConfirmSerial()
            ToggleConfirmCellNumber()
            SetupInitialAgentDropdown()
            SetDefaultRenewType()
        End If

        If varIsOrderClosed Then
            DisableOrder()
        End If

    End Sub

    Private Sub SetupSalesRep()

        connection.Open()
        Dim cmd As SqlCommand

        Dim sql As String = "SELECT [UserName], [LoweredUserName] FROM [ASPNETDB].[dbo].[aspnet_Users] "
        cmd = New SqlCommand(sql, connection)

        salesRepDropdown.DataSource = cmd.ExecuteReader
        salesRepDropdown.DataTextField = "UserName"
        salesRepDropdown.DataValueField = "LoweredUserName"
        salesRepDropdown.DataBind()

        connection.Close()

        If Session("UserLevel") = 3 Then
            ' Set default for the sales rep name
            salesRepDropdown.SelectedValue = Membership.GetUser.UserName
            salesRepDropdown.Enabled = False
        End If

    End Sub

    Private Sub SetupVendorPinField()
        If varOrderCellNumber = "" Then
            vendorPin.ReadOnly = True
            cell_number.Attributes("onchange") = "SetVendorPin(); showCellNumberConfirm();isCellNumberUnique();"
        Else
            vendorPin.ReadOnly = False
            cell_number.Attributes("onchange") = "showCellNumberConfirm();isCellNumberUnique();"
        End If
    End Sub

    Private Sub PopulateAllPlanDropdowns()
        connection.Open()
        Dim cmd As SqlCommand

        ' The monthly plan dropdown
        Dim sql As String = "SELECT [planid], [planname] FROM [Plans] WHERE planref = 'Monthly' ORDER BY [planname]"
        cmd = New SqlCommand(sql, connection)
        Dim dtr As SqlDataReader = cmd.ExecuteReader

        monthlyPlanDropdown.DataSource = dtr
        monthlyPlanDropdown.DataTextField = "planname"
        monthlyPlanDropdown.DataValueField = "planid"
        monthlyPlanDropdown.DataBind()

        monthlyPlanDropdown.Items.Insert(0, New ListItem("None", 0))

        dtr.Close()
        dtr = cmd.ExecuteReader

        renewalMonthlyDropdown.DataSource = dtr
        renewalMonthlyDropdown.DataTextField = "planname"
        renewalMonthlyDropdown.DataValueField = "planid"
        renewalMonthlyDropdown.DataBind()

        renewalMonthlyDropdown.Items.Insert(0, New ListItem("None", 0))

        dtr.Close()

        ' The cash balance plans (Pay As You Go)
        sql = "SELECT [planid], [planname] FROM [Plans] WHERE planref = 'Pay As You Go'"
        cmd = New SqlCommand(sql, connection)
        dtr = cmd.ExecuteReader

        cashBalanceDropdown.DataSource = dtr
        cashBalanceDropdown.DataTextField = "planname"
        cashBalanceDropdown.DataValueField = "planid"
        cashBalanceDropdown.DataBind()

        cashBalanceDropdown.Items.Insert(0, New ListItem("None", 0))

        dtr.Close()
        dtr = cmd.ExecuteReader

        renewalCashDropdown.DataSource = dtr
        renewalCashDropdown.DataTextField = "planname"
        renewalCashDropdown.DataValueField = "planid"
        renewalCashDropdown.DataBind()

        renewalCashDropdown.Items.Insert(0, New ListItem("None", 0))

        dtr.Close()
        connection.Close()
    End Sub

    Private Sub PopulateItemsList()
        connection.Open()
        Dim cmd As SqlCommand

        Dim sql As String = "SELECT * FROM [items] WHERE [item_cost] IS NOT NULL ORDER BY display_seq"
        cmd = New SqlCommand(sql, connection)
        Dim dtr As SqlDataReader = cmd.ExecuteReader

        itemRepeater.DataSource = dtr
        itemRepeater.DataBind()

        dtr.Close()
        connection.Close()

    End Sub

    Protected Sub itemRepeater_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.RepeaterItemEventArgs) Handles itemRepeater.ItemDataBound

        Dim item = CType(e.Item.FindControl("itemsCheck"), HtmlInputCheckBox)

        item.Value = e.Item.DataItem("item_id")

        Dim onclickJs As String = "calculateTotal(); TogglePayButton(); "
        onclickJs &= "setItemCostLbl(this, " & e.Item.DataItem("item_cost") & "); "

        item.Attributes.Add("onclick", onclickJs)

    End Sub

    Private Sub SetSaveOrderBtnProperties(ByVal state As String)
        'Customer information tab save/update button
        saveOrderInfoBtn.Text = state
        saveOrderInfoBtn.CommandName = state
    End Sub

    Private Sub SetSaveNoteBtnProps()
        'Notes tab save/update button
        If varOrderNote Is Nothing Then
            saveOrderNoteBtn.Text = "Save"
            saveOrderNoteBtn.CommandName = "save"
        Else
            saveOrderNoteBtn.Text = "Update"
            saveOrderNoteBtn.CommandName = "update"
            notesTab.HeaderText = "Notes*"
        End If
    End Sub

    Private Sub DisableTabs()
        ' On a new order we force the user to complete customer information
        ' by disabling other tabs.
        renewalsTab.Enabled = False
        TabPanel5.Enabled = False
        equipmentTab.Enabled = False
        TabPanel6.Enabled = False
        intlCallsTab.Enabled = False
        notesTab.Enabled = False
    End Sub

    Private Sub ToggleConfirmSerial()
        If varSerialNumber = "" Then
            confirmSerialSpan.Attributes.Add("style", "display: block;")
        Else
            confirmSerialSpan.Attributes.Add("style", "display: none;")
        End If

        confirmSerialNumber.Text = ""

    End Sub

    Private Sub ToggleConfirmCellNumber()
        If varOrderCellNumber = "" Then
            confirmCellNumberSpan.Attributes.Add("style", "display: block;")
        Else
            confirmCellNumberSpan.Attributes.Add("style", "display: none;")
        End If

        confirmCellNumber.Text = ""
    End Sub

    Private Sub SetUpMonitorCheckBox(ByVal show As Boolean)

        Dim level As Integer = Session("UserLevel")
        If level = 1 Then
            monitor.Enabled = True
        End If

        Dim divState As String
        If show Then
            divState = "block"
            ' When creating a order for the first time
            ' this defaults to the agent monitor status.
            monitor.Checked = varMonitor
        Else
            divState = "none"
        End If
        moniterChckBox.Attributes.Add("style", "display: " & divState & ";")

    End Sub

    Private Sub SetDefaultRenewType()

        If varRenewalChargeType = 0 Then
            If customer IsNot Nothing Then
                If customer.CCLastFour <> "" Then
                    ' Set default to credit card
                    renewChargeTypeRadio.SelectedIndex =
                     renewChargeTypeRadio.Items.IndexOf(renewChargeTypeRadio.Items.FindByValue("1"))
                Else
                    ' Set default to agent
                    renewChargeTypeRadio.SelectedIndex =
                     renewChargeTypeRadio.Items.IndexOf(renewChargeTypeRadio.Items.FindByValue("2"))
                End If
            End If
        ElseIf varRenewalChargeType = 1 Then
            ' Set to credit card
            renewChargeTypeRadio.SelectedIndex =
               renewChargeTypeRadio.Items.IndexOf(renewChargeTypeRadio.Items.FindByValue("1"))
        ElseIf varRenewalChargeType = 2 Then
            ' Set default to agent
            renewChargeTypeRadio.SelectedIndex =
               renewChargeTypeRadio.Items.IndexOf(renewChargeTypeRadio.Items.FindByValue("2"))
        ElseIf varRenewalChargeType = 3 Then
            ' Set default to either
            renewChargeTypeRadio.SelectedIndex =
               renewChargeTypeRadio.Items.IndexOf(renewChargeTypeRadio.Items.FindByValue("3"))
        End If

    End Sub

    Private Sub SetUpCCExpDropdowns()
        ' Set up the year dropdown
        Dim year As Integer = Now.Year Mod 100
        For y = year To (year + 10)
            creditCardExpirationYear.Items.Add(y)
        Next
    End Sub

    Private Sub SetupInitialAgentDropdown()

        If Session("UserLevel") = 1 Or Session("UserLevel") = 2 Then
            initialAgentDropdownDiv.Visible = True
        Else
            initialAgentDropdownDiv.Visible = False
        End If

        If initialAgentDropdownDiv.Visible Then
            Dim cmd As SqlCommand = New SqlCommand
            cmd.Connection = New SqlConnection(
                ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
            cmd.CommandType = CommandType.Text
            cmd.CommandText = "SELECT [UserName], [LoweredUserName] FROM [ASPNETDB].[dbo].[aspnet_Users] "
            cmd.Connection.Open()

            initialAgentDropdown.DataSource = cmd.ExecuteReader
            initialAgentDropdown.DataTextField = "UserName"
            initialAgentDropdown.DataValueField = "LoweredUserName"
            initialAgentDropdown.DataBind()

            cmd.Connection.Close()

            If customer IsNot Nothing Then
                Dim index = initialAgentDropdown.Items.IndexOf(
                initialAgentDropdown.Items.FindByValue(customer.InitialAgent.ToLower))
                ' If value isn't found in the dropdown -1 is returned and default to first selection. 
                initialAgentDropdown.SelectedIndex = If(index > -1, index, 0)
            Else
                initialAgentDropdown.SelectedValue = Membership.GetUser.UserName.ToLower
            End If

        End If

    End Sub

    Private Function HasPermission() As Boolean

        If Session("UserLevel") = 3 And customer IsNot Nothing Then
            If customer.InitialAgent <> "" Then
                If customer.InitialAgent.ToLower <> Membership.GetUser.UserName.ToLower Then
                    Return False
                End If
            End If
        End If

        Return True

    End Function


    ' Get data from database and set variables
    Private Sub GetOrderData()

        Dim sql As StringBuilder = New StringBuilder
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        con.Open()
        Dim cmd As SqlCommand

        sql.Append("SELECT ")
        ' Order table
        sql.Append("[order_id] ,[cell_num], [customer_id], [create_date], [plan_id] ,[esn], [serial_num] ,")
        sql.Append("[monthly_plan_id], [monthly_auto_renew], [cash_plan_id], [cash_auto_renew], [intl_amt], [intl_auto_renew], ")
        sql.Append("[intl_pin] ,[carrier_name] ,[carrier_pin] ,[vendor_pin], ")
        sql.Append("[monitor] ,[sales_rep_id], [renew_charge_type], [order_closed], ")
        ' MDN table
        sql.Append("[Status], [RatePlan], [Balance], [ExpDate], [PlanBalanceDetails], [PlanExpDate], [lastModified], [PINCount], ")
        ' Plans table
        sql.Append("[planname], [kosher], ")
        ' IntlBalances table (in the ATCIntl database)
        sql.Append("[BalAvailable] AS intl_bal ")
        sql.Append("FROM [orders] ")
        sql.Append("LEFT JOIN [MDN] ON [MDN].[PhoneNumber] = [orders].[cell_num] ")
        sql.Append("LEFT JOIN Plans ON [orders].[plan_id] = [Plans].[planid] ")
        sql.Append("LEFT JOIN [ATCIntl].[dbo].[IntlBalances] ON orders.[intl_pin] = [IntlBalances].[IntlPin] ")
        sql.Append("WHERE [order_id] = @order_id; ")

        cmd = New SqlCommand(sql.ToString(), con)

        cmd.Parameters.Add("@order_id", SqlDbType.Int).Value = varOrderID

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()

            varCustomerId = reader.Item("customer_id")

            customer = New Customer(varCustomerId)

            varOrderCellNumber = ReplaceDBNull(reader.Item("cell_num"), "")

            varStatus = StrConv(ReplaceDBNull(reader.Item("Status"), ""), VbStrConv.ProperCase)

            Dim str As String = ReplaceDBNull(reader.Item("RatePlan"), "")
            If str.Contains("n Text") = True Then
                varVendorName = str.Replace("n Text", "")
            ElseIf str.Contains("Standard") = True Then
                varVendorName = "Pay Per Minute"
            Else
                varVendorName = str
            End If

            varSignupDate = ReplaceDBNull(reader.Item("create_date"), Nothing)

            varBalance = FormatCurrency(ReplaceDBNull(reader.Item("Balance"), 0), 2)

            varExpirationDate = ReplaceDBNull(reader.Item("ExpDate"), Nothing)

            varPlanExpirationDate = ReplaceDBNull(reader.Item("PlanExpDate"), Nothing)

            Dim lastUpdate As Date = ReplaceDBNull(reader.Item("lastModified"), Nothing)
            varLastUpdate = lastUpdate.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))

            varEsn = ReplaceDBNull(reader.Item("esn"), "")

            varIntlPin = ReplaceDBNull(reader.Item("intl_pin"), "")

            varSerialNumber = ReplaceDBNull(reader.Item("serial_num"), "")

            varCarrierName = ReplaceDBNull(reader.Item("carrier_name"), "")

            varVendorPin = ReplaceDBNull(reader.Item("vendor_pin"), "")

            varMonitor = ReplaceDBNull(reader.Item("monitor"), Session("UserMonitor"))

            varRenewalChargeType = ReplaceDBNull(reader.Item("renew_charge_type"), 0)

            varIsOrderClosed = ReplaceDBNull(reader.Item("order_closed"), False)

            varPlanID = ReplaceDBNull(reader.Item("plan_id"), 0)

            varPlanName = ReplaceDBNull(reader.Item("planname"), "")

            varIsKosherPlan = ReplaceDBNull(reader.Item("kosher"), True)

            varRenewalMonthlyId = ReplaceDBNull(reader.Item("monthly_plan_id"), 0)

            varIsMonthlyRenew = ReplaceDBNull(reader.Item("monthly_auto_renew"), False)

            varRenewalCashId = ReplaceDBNull(reader.Item("cash_plan_id"), 0)

            varIsCashRenew = ReplaceDBNull(reader.Item("cash_auto_renew"), False)

            varRenewalIntl = FormatNumber(ReplaceDBNull(reader.Item("intl_amt"), 0), 2)

            varIsIntlRenew = ReplaceDBNull(reader.Item("intl_auto_renew"), False)

            varStackedPinCount = ReplaceDBNull(reader.Item("PINCount"), 0)

            varIntlBalance = ReplaceDBNull(reader.Item("intl_bal"), 0)

        End If

        cmd = New SqlCommand("CallSummary", con)
        cmd.CommandType = Data.CommandType.StoredProcedure

        cmd.Parameters.Add("@pn", SqlDbType.VarChar).Value = varOrderCellNumber

        reader.Close()
        reader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()
            varIncomingMinutes = ReplaceDBNull(reader.Item("Incoming minutes"), 0)
            varOutgoingMinutes = ReplaceDBNull(reader.Item("Outgoing minutes"), 0)
            varTotalMinutes = ReplaceDBNull(reader.Item("Total Minutes"), 0)
        End If

        reader.Close()

        cmd = New SqlCommand("SELECT [note] FROM [order_notes] WHERE [order_id] = @order_id;", con)
        cmd.CommandType = Data.CommandType.Text

        cmd.Parameters.Add("@order_id", SqlDbType.Int).Value = varOrderID
        reader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()
            varOrderNote = ReplaceDBNull(reader.Item("note"), "")
        End If

        reader.Close()
        con.Close()

    End Sub

    Private Function GetAgentCreditLimit(Optional ByVal userName As String = "", Optional ByVal isAjax As Boolean = False) As Decimal
        Dim creditLimit As Decimal = 0
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        Dim sql As String = "SELECT [CreditLimit] FROM [ASPNETDB].[dbo].[aspnet_Users] WHERE [UserName] = @UserName"

        con.Open()

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        If userName = "" Then   ' This is executing on page load
            cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text
        Else                    ' This is executing for a callback function
            cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = userName
        End If

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If isAjax Then
            Dim ds As New DataSet
            ds.Load(reader, LoadOption.PreserveChanges, "aspnet_Users")
            _callBackResult = ds.GetXml()
            _callBackResult &= "<creditUsed>" & GetAgentCreditTotal(userName) & "</creditUsed>"
            Return Nothing
        End If

        If reader.HasRows Then
            reader.Read()
            creditLimit = ReplaceDBNull(reader.Item("CreditLimit"), 0)
        End If

        reader.Close()
        con.Close()

        Return creditLimit

    End Function

    Private Function GetAgentCreditTotal(Optional ByVal userName As String = "") As Decimal
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        con.Open()

        Dim sql As String = "SELECT sum([total]) AS total FROM [authtrans] WHERE [agent] = @Agent AND charged = 1 AND trans_type = 2 "

        Dim cmd As SqlCommand = New SqlCommand(sql, con)
        If userName = "" Then
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text
        Else
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = userName
        End If

        Dim reader As SqlDataReader = cmd.ExecuteReader

        Dim total As Decimal

        If reader.HasRows Then
            reader.Read()
            total = ReplaceDBNull(reader.Item("total"), 0)
        End If

        con.Close()

        Return total

    End Function

    Private Function GetCostForCharge(ByVal planId As Integer) As Decimal
        connection.Open()

        Dim sql As String = "SELECT [planref], [plan_cost], [monthly_cost] FROM [Plans] WHERE [planid] = @PlanId"
        Dim cmd As SqlCommand
        Dim cost As Decimal = 0

        cmd = New SqlCommand(sql, connection)

        cmd.Parameters.Add("@PlanId", SqlDbType.Int).Value = planId

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            If reader.Item("planref") = "Pay As You Go" Then
                cost = ReplaceDBNull(reader.Item("plan_cost"), 0)
            ElseIf reader.Item("planref") = "Monthly" Then
                cost = ReplaceDBNull(reader.Item("monthly_cost"), 0)
            End If
        End If

        connection.Close()

        Return cost
    End Function

    Private Function GetItemTotal() As Decimal

        Dim str As String = ""
        Dim chck As HtmlInputCheckBox

        For Each item As RepeaterItem In itemRepeater.Items
            chck = CType(item.FindControl("itemsCheck"), HtmlInputCheckBox)
            If chck.Checked Then
                str &= chck.Value & ","
            End If
        Next

        If str.Length > 0 Then
            str = str.Remove(str.Length - 1, 1)
        Else
            str = "0"
        End If

        connection.Open()

        Dim sql As String = "SELECT sum([item_cost]) as cost FROM [items] WHERE [item_id] in (" & str & ")"
        Dim cmd As SqlCommand = New SqlCommand(sql, connection)

        Dim total As Decimal = 0

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()
            total = ReplaceDBNull(reader.Item("cost"), 0)
        End If

        connection.Close()

        Return total

    End Function


    ' Set page data
    Private Sub SetOrderInfoBar()

        serailNumLbl.Text = varSerialNumber
        lblCellNumber.Text = varOrderCellNumber
        lblName.Text = customer.GetFullName()

        Dim initAgnt As String = customer.InitialAgent
        If initAgnt.Length < 8 Then
            initialAgentLbl.Text = initAgnt
        Else
            initialAgentLbl.Text = initAgnt.Substring(0, 8)
        End If

        planNameBarLbl.Text = varPlanName
        statusLbl.Text = varStatus
        lastUpdatedLbl.Text = varLastUpdate

        balanceLbl.Text = varBalance
        inltBallanceLbl.Text = FormatCurrency(varIntlBalance, 2)

        expirationDateLbl.Text = varExpirationDate
        planExpirationDateLbl.Text = varPlanExpirationDate
        pinStackedStatusLbl.Text = varStackedPinCount

        signupDateBarLbl.Text = varSignupDate.ToString("d", CultureInfo.CreateSpecificCulture("en-US"))

        renewalMonthLbl.Text = FormatCurrency(GetPlanCost(varRenewalMonthlyId), 2)
        renewalCashLbl.Text = FormatCurrency(GetPlanCost(varRenewalCashId), 2)
        renewIntlLbl.Text = FormatCurrency(varRenewalIntl, 2)

        vendorNameLbl.Text = varVendorName
        incomingMinutesLbl.Text = varIncomingMinutes
        outgoingMinutesLbl.Text = varOutgoingMinutes
        totalMinutesLbl.Text = varTotalMinutes
    End Sub

    Private Sub PopulateOrderForm()

        Dim level As Integer = Session("UserLevel")

        ' Customer information is only available after the GetOrderData function has been called.
        prefix.Text = customer.Prefix
        fname.Text = customer.FirstName
        billingFname.Text = customer.BillingFName
        lname.Text = customer.LastName
        billingLname.Text = customer.BillingLName
        address.Text = customer.Address
        billingAddress.Text = customer.BillingAddress
        city.Text = customer.City
        billingCity.Text = customer.BillingCity
        state.Text = customer.State
        billingState.Text = customer.BillingState
        phone.Text = customer.Phone
        billingPhone.Text = customer.BillingPhone
        zip.Text = customer.Zip
        billingZip.Text = customer.BillingZip
        email.Text = customer.Email
        billingEmail.Text = customer.BillingEmail

        If level > 0 Then
            If level = 1 Or level = 2 Then
                If customer.InitialAgent <> "" Then
                    Dim agentIndex =
                     salesRepDropdown.Items.IndexOf(salesRepDropdown.Items.FindByValue(customer.InitialAgent))
                    salesRepDropdown.SelectedIndex = If(agentIndex > 0, agentIndex, 0)
                Else
                    salesRepDropdown.SelectedIndex = 0
                End If
                esn.Text = varEsn
                intlPin.Text = varIntlPin
            End If
            If level = 3 Then
                If varEsn.Length > 1 Then
                    esn.Text = varEsn.Substring(0, 1) & "************"
                    intlPin.Text = varIntlPin.Substring(0, 1) & "**********"
                End If
            End If

            hiddenESN.Value = varEsn
            hiddenIntlPin.Value = varIntlPin

        End If

        cell_number.Text = varOrderCellNumber

        serialNumber.Text = varSerialNumber
        If varCarrierName <> vbNullString Then
            carrierName.Items.FindByText(varCarrierName).Selected = True
        Else
            ' Set to default
            carrierName.SelectedIndex = 0
        End If

        vendorPin.Text = varVendorPin

        customerPin.Text = customer.CusPin

        monitor.Checked = varMonitor

        If customer.CCLastFour <> "" Then
            creditCardNumber.Text = "************" & customer.CCLastFour
        End If

        Dim dateParts As String() = customer.CCExpiration.Split("-")
        If dateParts.Length > 1 Then    ' If the string doesn't contain this delimiter, then skip this.
            creditCardExpirationMonth.SelectedIndex =
             creditCardExpirationMonth.Items.IndexOf(creditCardExpirationMonth.Items.FindByValue(dateParts(1)))
            ' The year is stored in the db as four digits but we only display the last two digits.
            creditCardExpirationYear.SelectedIndex =
             creditCardExpirationYear.Items.IndexOf(creditCardExpirationYear.Items.FindByValue(dateParts(0).Substring(2)))
        End If

        Dim creditLimit = GetAgentCreditLimit()
        Dim creditUsed = GetAgentCreditTotal()
        Dim available = creditLimit - creditUsed

        creditLimitLbl.Text = FormatCurrency(creditLimit, 2)
        creditUsedLbl.Text = FormatCurrency(creditUsed, 2)
        creditAvailableLbl.Text = FormatCurrency(available, 2)

        ' Notes tab.
        orderNoteTextArea.InnerText = varOrderNote

    End Sub

    Private Sub PopulateRenewals()

        ' Set monthly renewals
        Dim renewMonthlyIndex = renewalMonthlyDropdown.Items.IndexOf(renewalMonthlyDropdown.Items.FindByValue(varRenewalMonthlyId))
        renewalMonthlyDropdown.SelectedIndex = If(renewMonthlyIndex > 0, renewMonthlyIndex, 0)
        monthlyRenewalChk.Checked = varIsMonthlyRenew

        ' Set cash renewals
        Dim renewCashIndex = renewalCashDropdown.Items.IndexOf(cashBalanceDropdown.Items.FindByValue(varRenewalCashId))
        renewalCashDropdown.SelectedIndex = If(renewCashIndex > 0, renewCashIndex, 0)
        cashRenewalChk.Checked = varIsCashRenew

        ' Set international renewals
        Dim renewIntlIndex = renewalIntlDropdown.Items.IndexOf(renewalIntlDropdown.Items.FindByValue(varRenewalIntl))
        renewalIntlDropdown.SelectedIndex = If(renewIntlIndex > 0, renewIntlIndex, 0)
        intlRenewalChk.Checked = varIsIntlRenew

    End Sub

    Private Sub SetInvoiceAmounts()

        ' This function is to maintain invoice values on unsuccessful transactions.
        ' If the transaction is successful then the amounts return to default of zero.
        monthlyPlanDropdownCostLbl.Text =
         If(monthlyPlanDropdown.SelectedIndex > 0,
            FormatCurrency(GetCostForCharge(monthlyPlanDropdown.SelectedValue), 2), "")

        cashBalanceDropdownCostLbl.Text =
         If(cashBalanceDropdown.SelectedIndex > 0,
            FormatCurrency(GetCostForCharge(cashBalanceDropdown.SelectedValue), 2), "")

        intlBalanceDropdownCostLbl.Text =
         If(intlBalanceDropdown.SelectedIndex > 0, FormatCurrency(intlBalanceDropdown.SelectedValue, 2), "")

        For Each item As RepeaterItem In itemRepeater.Items
            If CType(item.FindControl("itemsCheck"), HtmlInputCheckBox).Checked Then
                CType(item.FindControl("itemCostLbl"), HtmlGenericControl).InnerText =
                 FormatCurrency(CType(item.FindControl("hiddenItemCost"), HiddenField).Value)
            End If
        Next

    End Sub

    Private Sub BindTransactionHistoryGrid()

        Dim sql As String = "SELECT [paydate], [monthly_amt], [cash_amt], [total], [user], [intl_amt], [item_amt], [agent], [authmessage], "
        sql &= "[trans_type] = CASE [trans_type]  WHEN 1 THEN 'Credit Card' WHEN 2 THEN 'Agent Account' ELSE '' End "
        sql &= "FROM [authtrans] WHERE [orderid] = " & varOrderID & " "
        sql &= "AND [charged] = 1 ORDER BY paydate desc"

        SqlDataSource3.ConnectionString = ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString
        SqlDataSource3.SelectCommandType = SqlDataSourceCommandType.Text
        SqlDataSource3.SelectCommand = sql
        SqlDataSource3.CancelSelectOnNullParameter = False

        transactionHistoryGridView.DataBind()
        connection.Close()

    End Sub

    Private Sub BindCallDetailGridview()

        ' Get values that are used by the stored procedure to determine which cycle to display.
        Dim fromIndex As Integer = Convert.ToInt32(callDetailFromIndex.Value)
        Dim endIndex As Integer = Convert.ToInt32(callDetailEndIndex.Value)
        ' This is the number of cycles there are for this order.
        Dim cdIntervals As Integer = Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value)

        SqlDataSource2.ConnectionString = ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString
        SqlDataSource2.SelectCommandType = SqlDataSourceCommandType.StoredProcedure

        SqlDataSource2.SelectParameters.Item("phone_number").DefaultValue = varOrderCellNumber
        SqlDataSource2.SelectParameters.Item("from_index").DefaultValue = fromIndex
        SqlDataSource2.SelectParameters.Item("end_index").DefaultValue = endIndex

        SqlDataSource2.SelectCommand = "CallDetailGridviewData"
        SqlDataSource2.CancelSelectOnNullParameter = False

        orderCallDetails.DataBind()

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        Dim cmd As SqlCommand = New SqlCommand("CallSummary", con)
        cmd.Connection.Open()
        cmd.CommandType = Data.CommandType.StoredProcedure

        cmd.Parameters.Add("@pn", SqlDbType.VarChar).Value = varOrderCellNumber
        cmd.Parameters.Add("@from_index", SqlDbType.Int).Value = fromIndex
        cmd.Parameters.Add("@end_index", SqlDbType.Int).Value = endIndex

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()

            inMinutesForTab.Text = ReplaceDBNull(reader.Item("Incoming minutes"), 0)
            outMinutesForTab.text = ReplaceDBNull(reader.Item("Outgoing minutes"), 0)
            totalMinutesForTab.Text = ReplaceDBNull(reader.Item("Total Minutes"), 0)

            callDetailFromLbl.Text = ReplaceDBNull(reader.Item("from_date"), varSignupDate)
            If callDetailFromLbl.Text = "1/1/1900" Then
                callDetailFromLbl.Text = varSignupDate
            End If

            callDetailToLbl.Text = ReplaceDBNull(reader.Item("end_date"), Now)
        End If

        reader.Close()

        cmd.Connection.Close()

        ' End index is zero if there are no future cycles so if it is zero disable 
        ' the next cycle button.
        If endIndex > 0 Then
            nextCallDetail.Enabled = True
        Else
            nextCallDetail.Enabled = False
        End If

        ' Call detail intervals (cdIntervals) are the number of cycles for this phone number
        ' so disable previous cycle button if there are no previous cycles. 
        If fromIndex >= cdIntervals Then
            prevCallDetails.Enabled = False
        Else
            prevCallDetails.Enabled = True
        End If

    End Sub

    Private Sub BindIntlCallsGridView()

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("SELECT [CallDate], [CalledFrom], ")
        sql.Append("([DestCountry] + ' ' + [DialedTo]) AS DialedTo, ")
        sql.Append("[StartBal], [EndBal], [DNIS], [Retrieved] FROM [IntlCalls] ")
        sql.Append("WHERE [IntlPin] = '" & varIntlPin & "' ")
        sql.Append("ORDER BY [CallDate] DESC")

        intlCallSqlDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("atcIntlConnectionString").ConnectionString
        intlCallSqlDataSource.SelectCommandType = SqlDataSourceCommandType.Text

        intlCallSqlDataSource.SelectCommand = sql.ToString
        intlCallSqlDataSource.CancelSelectOnNullParameter = False

        intlCallsGridView.DataBind()

    End Sub

    Private Sub BindEquipmentGridview()
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.Text

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("DECLARE @item_id int BEGIN ")
        sql.Append("SET @item_id = (SELECT [item_id] FROM Items WHERE [item_name] = 'misc'); ")
        sql.Append("Select [authtrans].[transid] ")
        sql.Append(",[item_name] = CASE WHEN [AuthTransItems].[item_id] = @item_id THEN ")
        sql.Append("[item_name] + ' - ' + [misc_desc] ELSE [item_name] End ")
        sql.Append(",[item_cost] = CASE WHEN [AuthTransItems].[item_id] = @item_id THEN [misc_cost] ")
        sql.Append("ELSE [item_cost] End, [paydate] ")
        sql.Append("FROM [authtrans] JOIN [AuthTransItems] ")
        sql.Append("ON [authtrans].[transid] = [AuthTransItems].[transid] ")
        sql.Append("JOIN [items] ")
        sql.Append("ON [AuthTransItems].[item_id] = [items].[item_id] ")
        sql.Append("WHERE [orderid] = @OrderId ORDER BY [paydate] DESC END ")

        cmd.CommandText = sql.ToString

        cmd.Parameters.Add("OrderId", SqlDbType.Int).Value = varOrderID

        cmd.Connection.Open()

        Dim ds As New DataSet
        ds.Load(cmd.ExecuteReader(), LoadOption.PreserveChanges, "AuthTransItems")

        Dim myTable As DataTable = ds.Tables("AuthTransItems")

        Dim objSum As Object = myTable.Compute("Sum(item_cost)", "")

        equipmentGridview.DataSource = ds
        equipmentGridview.DataBind()

        ' This only works after the gridview is databound.
        If equipmentGridview.Rows.Count > 0 Then
            equipmentTotalDiv.Visible = True
            equipmentTotalLbl.Text = FormatCurrency(objSum.ToString, 2)
        Else
            equipmentTotalDiv.Visible = False
            equipmentTotalLbl.Text = Nothing
        End If

        cmd.Connection.Close()

    End Sub


    ' Functions for ajax calls.
    Private Sub GetPlanInfoFromSerial(ByVal serialNumber As String)

        If Not IsSerialUnique(serialNumber) Then
            _callBackResult = "nonunique"
            Exit Sub
        End If

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection = connection
        cmd.CommandText = "SELECT * FROM [SerialESN] WHERE [Serial#] = @SerialNumber"

        cmd.Parameters.Add("@SerialNumber", SqlDbType.NVarChar).Value = serialNumber

        cmd.Connection.Open()
        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            Dim ds As New DataSet
            ds.Load(reader, LoadOption.PreserveChanges, "SerialESN")
            Dim serialXml As XmlDocument = New XmlDocument()
            serialXml.LoadXml(ds.GetXml())

            Dim root As XmlNode = serialXml.DocumentElement

            Dim elem As XmlElement = serialXml.CreateElement("UserLevel")
            elem.InnerText = Session("UserLevel").ToString()

            root.AppendChild(elem)

            _callBackResult = serialXml.OuterXml()
        Else
            _callBackResult = "invalid"
        End If

        cmd.Connection.Close()

    End Sub

    Private Function GetPlanCost(ByVal planId As Integer, Optional ByVal isAjax As Boolean = False) As Decimal

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        con.Open()

        Dim cost As Decimal = 0
        Dim sql As String = "SELECT [planref], [plan_cost], [monthly_cost] FROM [Plans] WHERE [planid] = @PlanId"
        Dim cmd As SqlCommand

        cmd = New SqlCommand(sql, con)

        cmd.Parameters.Add("@PlanId", SqlDbType.Int).Value = planId

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            If isAjax Then
                Dim ds As New DataSet
                ds.Load(reader, LoadOption.PreserveChanges, "Plans")
                _callBackResult = ds.GetXml()
            Else
                reader.Read()
                If reader.Item("planref") = "Pay As You Go" Then
                    cost = reader.Item("plan_cost")
                ElseIf reader.Item("planref") = "Monthly" Then
                    cost = reader.Item("monthly_cost")
                End If

            End If

        End If

        con.Close()

        Return cost

    End Function

    Private Sub SaveRenwals(ByVal args As String)

        Dim monthlyPlanId As Integer
        Dim monthlyRenew As Boolean

        Dim cashPlanId As Integer
        Dim cashRenew As Boolean

        Dim intlCost As Integer
        Dim intlRenew As Boolean

        Dim argsParts As String() = args.Split("~")

        Dim monthlyParts = argsParts(0).Split(",")
        monthlyPlanId = monthlyParts(0)
        monthlyRenew = monthlyParts(1)

        Dim cashParts = argsParts(1).Split(",")
        cashPlanId = cashParts(0)
        cashRenew = cashParts(1)

        Dim intlParts = argsParts(2).Split(",")
        intlCost = intlParts(0)
        intlRenew = intlParts(1)
        ' Make sure that an id isn't saved when there is no renewal selected
        If Not monthlyRenew Then
            monthlyPlanId = 0
        End If

        If Not cashRenew Then
            cashPlanId = 0
        End If

        If Not intlRenew Then
            intlCost = 0
        End If

        ' Get the renewal charge type
        Dim renewChargeType As Integer
        If Not monthlyRenew And Not cashRenew And Not intlRenew Then
            renewChargeType = 0
        Else
            renewChargeType = Integer.Parse(argsParts(3))
        End If

        connection.Open()

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("UPDATE orders SET [monthly_plan_id] = @MonthlyID , ")
        sql.Append("[monthly_auto_renew] = @MonthlyRenew, ")
        sql.Append("[cash_plan_id] = @CashID, ")
        sql.Append("[cash_auto_renew] = @CashRenew, ")
        sql.Append("[intl_amt] = @IntlAmnt, ")
        sql.Append("[intl_auto_renew] = @IntlRenew, ")
        sql.Append("[renew_charge_type] = @RenewChargeType ")
        sql.Append("WHERE [order_id] = @OrderID ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), connection)

        cmd.Parameters.Add("@MonthlyID", SqlDbType.Int).Value = monthlyPlanId
        cmd.Parameters.Add("@MonthlyRenew", SqlDbType.Bit).Value = monthlyRenew

        cmd.Parameters.Add("@CashID", SqlDbType.Int).Value = cashPlanId
        cmd.Parameters.Add("@CashRenew", SqlDbType.Bit).Value = cashRenew

        cmd.Parameters.Add("@IntlAmnt", SqlDbType.Money).Value = intlCost
        cmd.Parameters.Add("@IntlRenew", SqlDbType.Bit).Value = intlRenew

        cmd.Parameters.Add("@RenewChargeType", SqlDbType.Int).Value = renewChargeType

        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID

        Dim rowsAffected = cmd.ExecuteNonQuery()

        If rowsAffected = 1 Then

            Dim str As String = ""

            If monthlyRenew Then
                str &= monthlyPlanId & ","
            End If

            If cashRenew Then
                str &= cashPlanId & ","
            End If

            If str.Length > 0 Then
                str = str.Remove(str.Length - 1, 1)
            Else
                str = "0"
            End If

            sql.Clear()

            sql.Append("SELECT [planref], [plan_cost], [monthly_cost] FROM plans ")
            sql.Append("WHERE [planid] in (" & str & ") ")

            cmd = New SqlCommand(sql.ToString(), connection)
            Dim reader As SqlDataReader = cmd.ExecuteReader()

            If reader.HasRows Then
                Dim ds As New DataSet
                ds.Load(reader, LoadOption.PreserveChanges, "plans")
                _callBackResult = ds.GetXml()
            End If

        End If

        connection.Close()

    End Sub


    ' Handle user events
    Protected Sub SaveOrderBtnClick(ByVal s As Object, ByVal e As CommandEventArgs)
        If e.CommandName = "Save" Then
            SaveOrder()
        ElseIf e.CommandName = "Update" Then
            UpdateOrder()
            ClearInvoiceFields()
        End If

        If serialNumber.Text.Length > 0 And hiddenIntlPin.Value.Length > 0 Then
            AssocIntlPin()
        End If

    End Sub

    Protected Sub payButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles payButton.Click
        ChargeOrder()
    End Sub

    Protected Sub CancelOrder(ByVal s As Object, ByVal e As CommandEventArgs)
        Response.Redirect("~/SearchOrder.aspx")
    End Sub

    Protected Sub saveCustomerBillingInfoBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles saveCustomerBillingInfoBtn.Click

        ClearInvoiceFields()
        SaveCustomerBillingInfo()

        Dim noChargeRunCharge As CreditCardCharge = New CreditCardCharge

        noChargeRunCharge.OrderId = varOrderID
        noChargeRunCharge.CellNumber = varOrderCellNumber

        noChargeRunCharge.CCNumber = creditCardNumber.Text
        noChargeRunCharge.CCExpiration = GetCCExpDate()
        noChargeRunCharge.CCCode = creditCardCode.Text

        noChargeRunCharge.User = Membership.GetUser.UserName
        noChargeRunCharge.Agent = salesRepDropdown.SelectedItem.Text

        noChargeRunCharge.RunCharge()

        RefreshData()

    End Sub

    Protected Sub prevCallDetails_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles prevCallDetails.Click

        ' These values are passed to the stored procedure in the BindCallDetailGridview function
        Dim fromIndex As Integer = Convert.ToInt32(callDetailFromIndex.Value)
        Dim endIndex As Integer = Convert.ToInt32(callDetailEndIndex.Value)

        Dim cdIntervals As Integer = Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value)

        If fromIndex < cdIntervals Then
            callDetailFromIndex.Value = fromIndex + 1
            fromIndex += 1
            callDetailEndIndex.Value = endIndex + 1
            endIndex += 1
        End If

        orderCallDetails.PageIndex = 0
        BindCallDetailGridview()

    End Sub

    Protected Sub nextCallDetail_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles nextCallDetail.Click

        ' These values are passed to the stored procedure in the BindCallDetailGridview function
        Dim fromIndex As Integer = Convert.ToInt32(callDetailFromIndex.Value)
        Dim endIndex As Integer = Convert.ToInt32(callDetailEndIndex.Value)

        Dim cdIntervals As Integer = Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value)

        ' endIndex should never be below 0
        If endIndex > 0 Then
            callDetailEndIndex.Value = endIndex - 1
            endIndex -= 1
            callDetailFromIndex.Value = fromIndex - 1
            fromIndex -= 1
        End If

        orderCallDetails.PageIndex = 0
        BindCallDetailGridview()

    End Sub

    Protected Sub SaveOrderNote(ByVal s As Object, ByVal e As CommandEventArgs)

        ClearInvoiceFields()

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.Text

        If e.CommandName = "save" Then
            cmd.CommandText = "INSERT INTO [order_notes] ([order_id], [create_date], [note]) VALUES "
            cmd.CommandText &= "(@OrderId, getdate(), @Note) "
        ElseIf e.CommandName = "update" Then
            cmd.CommandText = "UPDATE [order_notes] SET [note] = @Note WHERE [order_id] = @OrderId "
        Else
            Exit Sub
        End If

        cmd.Connection.Open()
        cmd.Parameters.Add("OrderId", SqlDbType.Int).Value = varOrderID
        cmd.Parameters.Add("Note", SqlDbType.VarChar).Value = orderNoteTextArea.InnerText

        Dim rowAffected As Integer = cmd.ExecuteNonQuery()

        If rowAffected > 0 And e.CommandName = "save" Then
            varOrderNote = orderNoteTextArea.InnerText
            SetSaveNoteBtnProps()   ' Has a dependency on varOrderNote
        End If

        cmd.Connection.Close()

    End Sub

    Protected Sub downloadCdrBtn_Click()

        orderCallDetails.AllowPaging = False
        orderCallDetails.AllowSorting = False
        orderCallDetails.EditIndex = -1

        orderCallDetails.DataBind()

        Response.Clear()
        Response.AddHeader("content-disposition", "attachment; filename=Call Details.xls")
        Response.ContentType = "application/ms-excel"
        Response.Charset = ""

        Dim sw As System.IO.StringWriter = New System.IO.StringWriter
        Dim hw As System.Web.UI.HtmlTextWriter = New HtmlTextWriter(sw)

        orderCallDetails.RenderControl(hw)

        ' Write out the data  
        Response.Write(sw.ToString())
        Response.End()

        ' Try this to get data without the gridview and try to print it in bob-html format.
        'Dim dv As DataView = CType(SqlDataSource2.Select(DataSourceSelectArguments.Empty), DataView)
        'Response.Write(dv.Table.Rows(0)(0).ToString)

    End Sub

    Protected Sub ToggleOrderState(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.CommandEventArgs)
        If e.CommandName = "close" Then
            If CloseOrder(True) Then
                DisableOrder()
                ClearInvoiceFields()
            End If
        ElseIf e.CommandName = "open" Then
            If cell_number.Text.Length > 0 Then
                If Not IsCellNumberUnique(cell_number.Text) Then
                    toggleOrderErrorMsg.InnerText = "Cell number already reassigned."
                    Exit Sub
                End If
            End If

            If serialNumber.Text.Length > 0 Then
                If Not IsSerialUnique(serialNumber.Text) Then
                    toggleOrderErrorMsg.InnerText = "Serial number already reassigned."
                    Exit Sub
                End If
            End If

            If CloseOrder(False) Then
                EnableOrder()
            End If

        End If
    End Sub

    ' In order for a gridview to be exported, this method must be overridden
    Public Overrides Sub VerifyRenderingInServerForm(ByVal control As Control)

    End Sub


    ' Charge functions
    Private Function CreditCardCharge() As Boolean

        Dim ccCharge As CreditCardCharge = New CreditCardCharge

        ccCharge.OrderId = varOrderID
        ccCharge.CellNumber = varOrderCellNumber
        ccCharge.CCNumber = creditCardNumber.Text
        ccCharge.CCExpiration = GetCCExpDate()
        ccCharge.CCCode = creditCardCode.Text
        ccCharge.MonthlyPlanId = monthlyPlanDropdown.SelectedValue
        ccCharge.CashPlanId = cashBalanceDropdown.SelectedValue

        ccCharge.MonthlyAmnt = GetCostForCharge(monthlyPlanDropdown.SelectedValue)
        ccCharge.CashAmnt = GetCostForCharge(cashBalanceDropdown.SelectedValue)
        ccCharge.IntlAmnt = If(intlBalanceDropdown.SelectedIndex > 0, Decimal.Parse(intlBalanceDropdown.SelectedValue), 0)
        ccCharge.ItemAmnt = GetItemTotal()

        ccCharge.MiscellaneousName = miscNameFld.Text
        ccCharge.MiscellaneousCost =
         If(miscCostFld.Text.Length > 0, Decimal.Parse(miscCostFld.Text, NumberStyles.Any), 0)

        ccCharge.User = Membership.GetUser.UserName
        ccCharge.Agent = salesRepDropdown.SelectedItem.Text
        ccCharge.RunCharge()

        If ccCharge.hasCharged Then
            SaveAuthTransItems(ccCharge)
        End If

        authNoteLbl.Text = ccCharge.AuthMessage

        Return ccCharge.hasCharged

    End Function

    Private Function AgentAccountCharge() As Boolean

        Dim agentCharge As AgentCharge = New AgentCharge

        agentCharge.OrderId = varOrderID
        agentCharge.CellNumber = varOrderCellNumber
        agentCharge.MonthlyPlanId = monthlyPlanDropdown.SelectedValue
        agentCharge.CashPlanId = cashBalanceDropdown.SelectedValue

        agentCharge.MonthlyAmnt = GetCostForCharge(monthlyPlanDropdown.SelectedValue)
        agentCharge.CashAmnt = GetCostForCharge(cashBalanceDropdown.SelectedValue)
        agentCharge.IntlAmnt = If(intlBalanceDropdown.SelectedIndex > 0, Decimal.Parse(intlBalanceDropdown.SelectedValue), 0)
        agentCharge.ItemAmnt = GetItemTotal()

        agentCharge.MiscellaneousName = miscNameFld.Text
        agentCharge.MiscellaneousCost =
         If(miscCostFld.Text.Length > 0, Decimal.Parse(miscCostFld.Text, NumberStyles.Any), 0)

        agentCharge.Agent = salesRepDropdown.SelectedItem.Text
        agentCharge.User = Membership.GetUser.UserName

        agentCharge.BillingFName = billingFname.Text.Trim
        agentCharge.BillingLName = billingLname.Text.Trim
        agentCharge.BillingAddress = billingAddress.Text.Trim
        agentCharge.BillingCity = billingCity.Text.Trim
        agentCharge.BillingState = billingState.Text.Trim
        agentCharge.BillingZip = billingZip.Text.Trim
        agentCharge.BillingPhone = billingPhone.Text.Trim
        agentCharge.BillingEmail = billingEmail.Text.Trim

        agentCharge.RunAgentAccountCharge()

        If agentCharge.hasCharged Then
            SaveAuthTransItems(agentCharge)
        End If

        authNoteLbl.Text = agentCharge.AuthMessage

        Return agentCharge.hasCharged

    End Function


    ' Helper functions
    Private Sub RefreshData()
        GetOrderData()

        PopulateRenewals()
        SetOrderInfoBar()
        PopulateOrderForm()
        ToggleConfirmSerial()
        ToggleConfirmCellNumber()

        ' Reset the vendor pin field
        SetupVendorPinField()

        SetUpMonitorCheckBox(varIsKosherPlan)
        SetDefaultRenewType()

    End Sub

    Private Sub SetupNumberOfCallDetailsIntervals()

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        Dim cmd As SqlCommand = New SqlCommand("GetCallDetailIntervals", con)
        cmd.Connection.Open()

        cmd.CommandType = Data.CommandType.StoredProcedure

        cmd.Parameters.Add("PhoneNumber", SqlDbType.VarChar).Value = varOrderCellNumber

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            If reader.Item("cd_intervals") IsNot DBNull.Value Then
                hdnNumOfCallDetailsIntervals.Value = reader.Item("cd_intervals")
            Else
                hdnNumOfCallDetailsIntervals.Value = 0
            End If
        Else
            hdnNumOfCallDetailsIntervals.Value = 0
        End If

        cmd.Connection.Close()

        If Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value) > 0 Then
            prevCallDetails.Enabled = True
        Else
            prevCallDetails.Enabled = False
        End If

    End Sub

    Private Function IsCellNumberUnique(ByVal args As String) As Boolean

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        Dim sql As String = "SELECT [cell_num] FROM orders WHERE [cell_num] = @CellNum "
        sql &= "AND (order_closed != 1 OR order_closed IS NULL) "
        sql &= "AND order_id != @OrderId "
        Dim cmd As SqlCommand = New SqlCommand(sql, con)
        cmd.Connection.Open()

        cmd.Parameters.Add("CellNum", SqlDbType.VarChar).Value = args
        cmd.Parameters.Add("OrderId", SqlDbType.Int).Value = varOrderID

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            _callBackResult = "nonunique"
            cmd.Connection.Close()
            Return False
        Else
            _callBackResult = "unique"
            cmd.Connection.Close()
            Return True
        End If

    End Function

    Private Function IsSerialUnique(ByVal serial As String) As Boolean

        Dim sql As String = "SELECT serial_num FROM orders WHERE serial_num = @SerialNumber "
        sql &= "AND isnull(order_closed, 0) != 1  "
        sql &= "AND [order_id] != @OrderId "

        Dim cmd As SqlCommand = New SqlCommand()
        cmd.Connection = connection
        cmd.Connection.Open()
        cmd.CommandText = sql

        cmd.Parameters.Add("SerialNumber", SqlDbType.VarChar).Value = serial
        cmd.Parameters.Add("OrderId", SqlDbType.Int).Value = varOrderID

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            cmd.Connection.Close()
            Return False
        Else
            cmd.Connection.Close()
            Return True
        End If

    End Function

    Private Sub SaveOrder()

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        Dim sql As StringBuilder = New StringBuilder

        ' Insert into cutomer
        sql.Append("INSERT INTO [customers] ([initial_agent], [fname], [lname], [prefix], [address], ")
        sql.Append("[city], [state], [phone], [zip], [email], [cus_pin]) ")
        sql.Append("VALUES (@Agent, @fname, @lname, @prefix, @address, @city, @state, @phone, @zip, @email, @customer_pin); ")

        ' Insert into orders
        sql.Append("INSERT INTO [orders] ([customer_id], [update_date], [cell_num], [serial_num], [esn],  ")
        sql.Append("[intl_pin], [carrier_name], [vendor_pin], [monitor]) ")
        sql.Append("VALUES (SCOPE_IDENTITY(), getdate(), @cell_number, @serial_number, @esn, ") ' Insert customer_id into the orders table
        sql.Append("@intl_pin, @carrier_name, @vendor_pin, @monitor); ")

        sql.Append("SELECT SCOPE_IDENTITY() AS order_id; ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)

        If Session("UserLevel") = 3 Then
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = Membership.GetUser.UserName
        Else
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = initialAgentDropdown.SelectedItem.Text
        End If

        If prefix.Text.Length > 0 Then
            cmd.Parameters.Add("@prefix", SqlDbType.VarChar).Value = prefix.Text.Trim()
        Else
            cmd.Parameters.Add("@prefix", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If fname.Text.Length > 0 Then
            cmd.Parameters.Add("@fname", SqlDbType.VarChar).Value = fname.Text.Trim()
        Else
            cmd.Parameters.Add("@fname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If lname.Text.Length > 0 Then
            cmd.Parameters.Add("@lname", SqlDbType.VarChar).Value = lname.Text.Trim()
        Else
            cmd.Parameters.Add("@lname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If address.Text.Length > 0 Then
            cmd.Parameters.Add("@address", SqlDbType.VarChar).Value = address.Text.Trim()
        Else
            cmd.Parameters.Add("@address", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If city.Text.Length > 0 Then
            cmd.Parameters.Add("@city", SqlDbType.VarChar).Value = city.Text.Trim()
        Else
            cmd.Parameters.Add("@city", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If state.Text.Length > 0 Then
            cmd.Parameters.Add("@state", SqlDbType.VarChar).Value = state.Text.Trim()
        Else
            cmd.Parameters.Add("@state", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If phone.Text.Length > 0 Then
            cmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = phone.Text.Trim()
        Else
            cmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If zip.Text.Length > 0 Then
            cmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = zip.Text.Trim()
        Else
            cmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If email.Text.Length > 0 Then
            cmd.Parameters.Add("@email", SqlDbType.VarChar).Value = email.Text.Trim()
        Else
            cmd.Parameters.Add("@email", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If cell_number.Text.Length > 0 Then
            cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = cell_number.Text
        Else
            cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = ""
        End If

        If hiddenESN.Value.Length > 0 Then
            cmd.Parameters.Add("@esn", SqlDbType.VarChar).Value = hiddenESN.Value
        Else
            cmd.Parameters.Add("@esn", SqlDbType.VarChar).Value = ""
        End If

        If hiddenIntlPin.Value.Length > 0 Then
            cmd.Parameters.Add("@intl_pin", SqlDbType.VarChar).Value = hiddenIntlPin.Value
        Else
            cmd.Parameters.Add("@intl_pin", SqlDbType.VarChar).Value = ""
        End If

        If serialNumber.Text.Length > 0 Then
            cmd.Parameters.Add("@serial_number", SqlDbType.VarChar).Value = serialNumber.Text
        Else
            cmd.Parameters.Add("@serial_number", SqlDbType.VarChar).Value = ""
        End If

        cmd.Parameters.Add("@carrier_name", SqlDbType.VarChar).Value = carrierName.SelectedItem.Text

        If vendorPin.ReadOnly Then
            ' Note: The vendor pin is the last four digits of the phone number so check that first.
            If cell_number.Text.Length > 0 Then
                Dim startIndex = cell_number.Text.Length - 4
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = cell_number.Text.Substring(startIndex)
            Else
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = DBNull.Value
            End If
        Else
            If vendorPin.Text.Length > 0 Then
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = vendorPin.Text
            Else
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = DBNull.Value
            End If
        End If

        If customerPin.Text.Length > 0 Then
            cmd.Parameters.Add("@customer_pin", SqlDbType.VarChar).Value = customerPin.Text
        Else
            cmd.Parameters.Add("@customer_pin", SqlDbType.VarChar).Value = DBNull.Value
        End If

        cmd.Parameters.Add("@monitor", SqlDbType.Bit).Value = monitor.Checked

        cmd.Connection.Open()

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()
            Response.Redirect("~/Order.aspx?oid=" & reader.Item("order_id"))
        End If

        cmd.Connection.Close()

    End Sub

    Private Sub UpdateOrder()

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        Dim sql As StringBuilder = New StringBuilder

        ' Update customers
        sql.Append("UPDATE [customers] SET [prefix] = @prefix, ")
        If Session("UserLevel") = 1 Or Session("UserLevel") = 2 Then
            sql.Append("[initial_agent] = @Agent, ")
        End If
        sql.Append("[fname] = @fname, [lname] = @lname, [address] = @address, ")
        sql.Append("[city] = @city, [state] = @state, [phone] = @phone, [zip] = @zip, ")
        sql.Append("[email] = @email, [cus_pin] = @customer_pin WHERE customer_id = @customer_id; ")

        ' Update orders
        sql.Append("UPDATE [orders] SET [update_date] = getdate(), ")
        sql.Append("[cell_num] = @cell_number, [serial_num] = @serial_number, ")
        sql.Append("[esn] = @esn, [intl_pin] = @intl_pin, [carrier_name] = @carrier_name, ")
        sql.Append("[vendor_pin] = @vendor_pin, [monitor] = @monitor ")
        sql.Append("WHERE [order_id] = @OrderID ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)

        If Session("UserLevel") = 1 Or Session("UserLevel") = 2 Then
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = initialAgentDropdown.SelectedItem.Text
        End If

        cmd.Parameters.Add("@customer_id", SqlDbType.Int).Value = varCustomerId

        ' Order ID for updating the order table
        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID

        If prefix.Text.Length > 0 Then
            cmd.Parameters.Add("@prefix", SqlDbType.VarChar).Value = prefix.Text.Trim()
        Else
            cmd.Parameters.Add("@prefix", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If fname.Text.Length > 0 Then
            cmd.Parameters.Add("@fname", SqlDbType.VarChar).Value = fname.Text.Trim()
        Else
            cmd.Parameters.Add("@fname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If lname.Text.Length > 0 Then
            cmd.Parameters.Add("@lname", SqlDbType.VarChar).Value = lname.Text.Trim()
        Else
            cmd.Parameters.Add("@lname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If address.Text.Length > 0 Then
            cmd.Parameters.Add("@address", SqlDbType.VarChar).Value = address.Text.Trim()
        Else
            cmd.Parameters.Add("@address", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If city.Text.Length > 0 Then
            cmd.Parameters.Add("@city", SqlDbType.VarChar).Value = city.Text.Trim()
        Else
            cmd.Parameters.Add("@city", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If state.Text.Length > 0 Then
            cmd.Parameters.Add("@state", SqlDbType.VarChar).Value = state.Text.Trim()
        Else
            cmd.Parameters.Add("@state", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If phone.Text.Length > 0 Then
            cmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = phone.Text.Trim()
        Else
            cmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If zip.Text.Length > 0 Then
            cmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = zip.Text.Trim()
        Else
            cmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If email.Text.Length > 0 Then
            cmd.Parameters.Add("@email", SqlDbType.VarChar).Value = email.Text.Trim()
        Else
            cmd.Parameters.Add("@email", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If cell_number.Text.Length > 0 Then
            cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = cell_number.Text
        Else
            cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = ""
        End If

        If hiddenESN.Value.Length > 0 Then
            cmd.Parameters.Add("@esn", SqlDbType.VarChar).Value = hiddenESN.Value
        Else
            cmd.Parameters.Add("@esn", SqlDbType.VarChar).Value = ""
        End If

        If hiddenIntlPin.Value.Length > 0 Then
            cmd.Parameters.Add("@intl_pin", SqlDbType.VarChar).Value = hiddenIntlPin.Value
        Else
            cmd.Parameters.Add("@intl_pin", SqlDbType.VarChar).Value = ""
        End If

        If serialNumber.Text.Length > 0 Then
            cmd.Parameters.Add("@serial_number", SqlDbType.VarChar).Value = serialNumber.Text
        Else
            cmd.Parameters.Add("@serial_number", SqlDbType.VarChar).Value = ""
        End If

        cmd.Parameters.Add("@carrier_name", SqlDbType.VarChar).Value = carrierName.SelectedItem.Text

        If vendorPin.ReadOnly Then
            ' Note: The vendor pin is the last four digits of the phone number so check that first.
            If cell_number.Text.Length > 0 Then
                Dim startIndex = cell_number.Text.Length - 4
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = cell_number.Text.Substring(startIndex)
            Else
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = DBNull.Value
            End If
        Else
            If vendorPin.Text.Length > 0 Then
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = vendorPin.Text
            Else
                cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = DBNull.Value
            End If
        End If

        If customerPin.Text.Length > 0 Then
            cmd.Parameters.Add("@customer_pin", SqlDbType.VarChar).Value = customerPin.Text
        Else
            cmd.Parameters.Add("@customer_pin", SqlDbType.VarChar).Value = DBNull.Value
        End If

        cmd.Parameters.Add("@monitor", SqlDbType.Bit).Value = monitor.Checked

        cmd.Connection.Open()

        Dim rowsAffected As Integer = cmd.ExecuteNonQuery

        If rowsAffected > 0 Then
            RefreshData()
        End If

        cmd.Connection.Close()

    End Sub

    Private Sub AssocIntlPin()

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        cmd.Connection.Open()
        cmd.CommandType = Data.CommandType.StoredProcedure
        cmd.CommandText = "AssocIntlPin"

        cmd.Parameters.Add("cellnum", SqlDbType.VarChar).Value = varOrderCellNumber
        cmd.Parameters.Add("intlpin", SqlDbType.VarChar).Value = hiddenIntlPin.Value

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

    End Sub

    Private Sub SaveAuthTransItems(ByVal charge As Charge)

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        con.Open()

        Dim sql As StringBuilder = New StringBuilder
        Dim chck As HtmlInputCheckBox
        Dim id As Integer = 0

        For Each item As RepeaterItem In itemRepeater.Items
            chck = CType(item.FindControl("itemsCheck"), HtmlInputCheckBox)

            id = chck.Value

            If chck.Checked Then
                sql.Append("INSERT INTO [AuthTransItems] ([transid], [item_id]) VALUES (" & charge.TransactionId & "," & id & ");")
            End If
        Next

        If charge.MiscellaneousCost > 0 And charge.MiscellaneousName.Length > 0 Then
            sql.Append("INSERT INTO [AuthTransItems] ([transid], [item_id], [misc_desc], [misc_cost]) ")
            sql.Append("VALUES (" & charge.TransactionId & ", (SELECT [item_id] FROM Items WHERE [item_name] = 'misc'), ")
            sql.Append("'" & charge.MiscellaneousName & "', " & charge.MiscellaneousCost & ") ")
        End If

        If sql.Length > 0 Then
            Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), con)
            cmd.ExecuteNonQuery()
        End If

        con.Close()

    End Sub

    Private Sub PayIntlPin()
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "PayIntlPin"

        cmd.Parameters.Add("intlpin", SqlDbType.VarChar).Value = hiddenIntlPin.Value
        ' This function shouldn't be called only if the intlBalanceDropdown.SelectedIndex > 0
        ' but we doublle check anyway, to be consistent with the other times we get the value.
        cmd.Parameters.Add("amount", SqlDbType.Money).Value =
         If(intlBalanceDropdown.SelectedIndex > 0, Decimal.Parse(intlBalanceDropdown.SelectedValue), 0)

        cmd.Connection.Open()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

    End Sub

    Private Sub ClearInvoiceFields()

        monthlyPlanDropdown.SelectedIndex = 0
        cashBalanceDropdown.SelectedIndex = 0
        intlBalanceDropdown.SelectedIndex = 0

        monthlyPlanDropdownCostLbl.Text = ""
        cashBalanceDropdownCostLbl.Text = ""
        intlBalanceDropdownCostLbl.Text = ""

        miscNameFld.Text = ""
        miscCostFld.Text = ""

        ' The checkboxes are cleared because the are databound again.
        PopulateItemsList()

    End Sub

    Private Sub ChargeOrder()

        SaveCustomerBillingInfo()

        connection.Open()

        Dim cmd As SqlCommand = Nothing
        Dim sql As New StringBuilder

        ' Update phone (order)
        sql.Append("UPDATE [orders] SET [update_date] = getdate(), ")
        sql.Append("[plan_id] = @PlanId ")
        sql.Append("WHERE [order_id] = @OrderID")

        cmd = New SqlCommand(sql.ToString, connection)

        ' Order ID for updating the order table
        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID

        ' Get plan id
        Dim planId As Integer = 0
        Dim monthlyId = monthlyPlanDropdown.SelectedValue
        Dim cashId = cashBalanceDropdown.SelectedValue
        If monthlyId > 0 Then
            planId = monthlyId
        ElseIf cashId > 0 Then
            planId = cashId
        End If

        If planId > 0 Then
            cmd.Parameters.Add("@PlanId", SqlDbType.Int).Value = planId
        Else
            cmd.Parameters.Add("@PlanId", SqlDbType.Int).Value = DBNull.Value
        End If

        cmd.ExecuteNonQuery()
        connection.Close()

        Dim chargeSuccessful As Boolean = False

        If selectPaymentMethod.Items(0).Selected Then   ' Selected method of payment: Credit card
            ' Charge the credit card
            chargeSuccessful = CreditCardCharge()
        ElseIf selectPaymentMethod.Items(1).Selected Then   ' Selected method of payment: Agent account
            ' Charge the agent account
            chargeSuccessful = AgentAccountCharge()
        End If

        creditLimitLbl.Text = FormatCurrency(GetAgentCreditLimit(salesRepDropdown.SelectedItem.Text), 2)

        If Not chargeSuccessful Then
            ' Reset the itemized cost labels.
            SetInvoiceAmounts()

            ' Register functions that get the invoice total and sets the pay button to enabled.
            Dim strScript As New StringBuilder
            strScript.Append("<SCRIPT> calculateTotal();")
            strScript.Append("TogglePayButton();</SCRIPT>")
            ClientScript.RegisterStartupScript(Me.GetType(), "recalculateInvoice", strScript.ToString)

            ' If there is an error, set the color to brown.
            authNoteLbl.ForeColor = Drawing.Color.Brown
        Else
            ' This condition only returns true before ClearInvoiceFields is called.
            If intlBalanceDropdown.SelectedIndex > 0 Then
                PayIntlPin()
                BalanceIntlPin()
            End If

            BindEquipmentGridview()
            ClearInvoiceFields()
            authNoteLbl.ForeColor = Drawing.Color.Black
        End If

        ' Repopulate the transaction history gridview
        BindTransactionHistoryGrid()

        RefreshData()

    End Sub

    Private Sub SaveCustomerBillingInfo()

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        Dim sql As StringBuilder = New StringBuilder

        sql.Append("UPDATE customers SET [billing_fname] = @bFname,")
        sql.Append("[billing_lname] =  @bLname,")
        sql.Append("[billing_address] = @bAddress,")
        sql.Append("[billing_phone] = @bPhone,")
        sql.Append("[billing_city] = @bCity,")
        sql.Append("[billing_state] = @bState,")
        sql.Append("[billing_zip] = @bZip,")
        sql.Append("[billing_email] = @bEmail,")
        sql.Append("[cc_last_four] = @ccLastFour,")
        sql.Append("[cc_expiration_date] = @ccExpDate ")
        sql.Append("WHERE customer_id = @customer_id;")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)
        cmd.Connection.Open()

        cmd.Parameters.Add("@customer_id", SqlDbType.Int).Value = varCustomerId

        ' Collect the data
        If billingFname.Text.Length > 0 Then
            cmd.Parameters.Add("@bFname", SqlDbType.VarChar).Value = billingFname.Text.Trim()
        Else
            cmd.Parameters.Add("@bFname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If billingLname.Text.Length > 0 Then
            cmd.Parameters.Add("@bLname", SqlDbType.VarChar).Value = billingLname.Text.Trim()
        Else
            cmd.Parameters.Add("@bLname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If billingAddress.Text.Length > 0 Then
            cmd.Parameters.Add("@bAddress", SqlDbType.VarChar).Value = billingAddress.Text.Trim()
        Else
            cmd.Parameters.Add("@bAddress", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If billingPhone.Text.Length > 0 Then
            cmd.Parameters.Add("@bPhone", SqlDbType.VarChar).Value = billingPhone.Text.Trim()
        Else
            cmd.Parameters.Add("@bPhone", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If billingCity.Text.Length > 0 Then
            cmd.Parameters.Add("@bCity", SqlDbType.VarChar).Value = billingCity.Text.Trim()
        Else
            cmd.Parameters.Add("@bCity", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If billingState.Text.Length > 0 Then
            cmd.Parameters.Add("@bState", SqlDbType.VarChar).Value = billingState.Text.Trim()
        Else
            cmd.Parameters.Add("@bState", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If billingZip.Text.Length > 0 Then
            cmd.Parameters.Add("@bZip", SqlDbType.VarChar).Value = billingZip.Text.Trim()
        Else
            cmd.Parameters.Add("@bZip", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If billingEmail.Text.Length > 0 Then
            cmd.Parameters.Add("@bEmail", SqlDbType.VarChar).Value = billingEmail.Text.Trim()
        Else
            cmd.Parameters.Add("@bEmail", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If creditCardNumber.Text.Length > 4 Then
            Dim ccStr As String = creditCardNumber.Text.Trim()
            cmd.Parameters.Add("@ccLastFour", SqlDbType.VarChar).Value = ccStr.Substring(ccStr.Length - 4)
        Else
            cmd.Parameters.Add("@ccLastFour", SqlDbType.VarChar).Value = ""
        End If

        cmd.Parameters.Add("@ccExpDate", SqlDbType.VarChar).Value = GetCCExpDate()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

    End Sub

    Private Sub BalanceIntlPin()

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "BalanceIntlPin"
        cmd.Parameters.Add("intlpin", SqlDbType.VarChar).Value = varIntlPin

        cmd.Connection.Open()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

    End Sub

    Private Sub DisableOrder()
        TabContainer1.Enabled = False
        closeOrderButton.ToolTip = "Set order to be enabled."
        closeOrderButton.Text = "Open Order"
        closeOrderButton.CommandName = "open"
    End Sub

    Private Sub EnableOrder()
        TabContainer1.Enabled = True
        closeOrderButton.ToolTip = "Set order to be disabled."
        closeOrderButton.Text = "Close Order"
        closeOrderButton.CommandName = "close"
    End Sub

    Private Function CloseOrder(ByVal orderClosed As Boolean) As Boolean

        Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        Dim sql As String = "UPDATE orders SET [order_closed] = @OrderClosed WHERE order_id = " & varOrderID
        Dim cmd As SqlCommand = New SqlCommand(sql, con)
        cmd.Connection.Open()

        cmd.Parameters.Add("OrderClosed", SqlDbType.Bit).Value = orderClosed

        Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
        cmd.Connection.Close()

        If rowsAffected = 1 Then
            Return True
        Else
            Return False
        End If

    End Function

    Private Function GetCCExpDate() As String

        Dim returnVal As String = ""

        If creditCardNumber.Text.Length > 0 Then
            Dim month As String = creditCardExpirationMonth.SelectedValue
            Dim year As String = "20" & creditCardExpirationYear.SelectedValue
            ' Assumes that were before year 2100
            returnVal = year & "-" & month
        End If

        Return returnVal

    End Function

    Private Sub ToggleCloseBtn()
        ' Function to hide the close button with CSS
        ' and not with closeOrderButton.visible = false
        If TabContainer1.ActiveTabIndex = 0 Then
            closeOrderButton.Style("display") = "inline"
        Else
            closeOrderButton.Style("display") = "none"
        End If
    End Sub

    Private Function ReplaceDBNull(ByVal val As Object, ByVal replace As Object) As System.Object
        If val Is DBNull.Value Then
            Return replace
        Else
            Return val
        End If
    End Function


    ' Handle ajax calls
    Public Sub RaiseCallbackEvent(ByVal eventArgument As String) _
     Implements System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent

        Dim getAction As String = eventArgument.Substring(0, (InStr(eventArgument, ":") - 1))

        If getAction = "getPlanInfoFromSerial" Then
            Dim serialNumber As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetPlanInfoFromSerial(serialNumber)
        End If

        If getAction = "SetCostLblFromServer" Then
            Dim planId As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetPlanCost(planId, True)
        End If

        If getAction = "GetAgentCreditLimit" Then
            Dim agent As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetAgentCreditLimit(agent, True)
        End If

        If getAction = "SaveRenewals" Then
            Dim args As String = eventArgument.Substring(InStr(eventArgument, ":"))
            SaveRenwals(args)
        End If

        If getAction = "isCellNumberUnique" Then
            Dim args As String = eventArgument.Substring(InStr(eventArgument, ":"))
            IsCellNumberUnique(args)
        End If

    End Sub

    Public Function GetCallbackResult() As String _
     Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function

End Class