Imports System.Globalization

Partial Class Order
    Inherits System.Web.UI.Page
    Implements System.Web.UI.ICallbackEventHandler

    Private Property connection As SqlConnection
    Dim _callBackResult As String = Nothing

    Public varCustomerId As Integer = 0
    Public varOrderID As Integer = 0

    Public varInitialAgent As String = vbNullString
    Public varPrefix As String = vbNullString
    Public varFirstName As String = vbNullString
    Public varLastName As String = vbNullString
    Public varAddress As String = vbNullString
    Public varCity As String = vbNullString
    Public varState As String = vbNullString
    Public varZip As String = vbNullString
    Public varPhone As String = vbNullString
    Public varEmail As String = vbNullString

    Public varAuthProfileID As Integer = 0
    Public varBillingFirstName As String = vbNullString
    Public varBillingLastName As String = vbNullString
    Public varBillingAddress As String = vbNullString
    Public varBillingCity As String = vbNullString
    Public varBillingState As String = vbNullString
    Public varBillingZip As String = vbNullString
    Public varBillingPhone As String = vbNullString
    Public varBillingEmail As String = vbNullString
    Public varCCLastDigits As String = vbNullString
    Public varCCExpiration As String = vbNullString

    Public varCustomerPin As String = vbNullString
    Public varSalesRepID As String = vbNullString
    Public varOrderCellNumber As String = ""
    Public varEsn As String = ""
    Public varIntlPin As String = ""
    Public varSerialNumber As String = vbNullString
    Public varCarrierName As String = vbNullString
    Public varVendorPin As String = vbNullString
    Public varMonitor As Boolean = Nothing
    Public varStatus As String = vbNullString
    Public varBalance As String = vbNullString
    Public varExpirationDate As String = vbNullString
    Public varPlanExpirationDate As String = vbNullString
    Public varIncomingMinutes As String = vbNullString
    Public varOutgoingMinutes As String = vbNullString
    Public varTotalMinutes As String = vbNullString
    Public varVendorName As String = vbNullString
    Public varLastUpdate As String = vbNullString
    Public varSignupDate As Date = Nothing
    Public varStackedPinCount As Integer = 0

    Public varLastChargeMonthly As Decimal = 0
    Public varLastChargeMonthPlanID As Integer = 0
    Public varLastChargeCash As Decimal = 0
    Public varLastChargeCashPlanID As Integer = 0
    Public varLastChargeIntl As Decimal = 0
    Public varLastChargeItem As Decimal = 0

    Public varRenewalMonthlyId As Integer = 0
    Public varIsMonthlyRenew As Boolean = False
    Public varRenewalCashId As Integer = 0
    Public varIsCashRenew As Boolean = False
    Public varRenewalIntl As Decimal = FormatNumber(0, 2)
    Public varIsIntlRenew As Boolean = False

    Public varPlanID As Integer = 0
    Public varPlanName As String = vbNullString
    Public varIsKosherPlan As Boolean = True

    ' Page initialization
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Response.Expires = -1

        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            Response.Redirect(FormsAuthentication.LoginUrl)
        End If

        If Session.Item("UserLevel") Is Nothing Then
            Response.Redirect(FormsAuthentication.LoginUrl)
        End If

        connection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        Dim orderID = Request.QueryString("oid")
        GetOrderData(orderID)

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

        If Not IsPostBack Then
            InitializePage()
        End If

        BindTransactionHistoryGridTab()
        SetupVendorPinField()

    End Sub

    Private Sub InitializePage()
        Dim orderID = Request.QueryString("oid")

        If orderID = Nothing Then
            SetButtonProperties("Save")
            SetTabsEnabled(False)
        Else
            SetButtonProperties("Update")
            PopulateAllPlanDropdowns()

            BindTransactionHistoryGrid()

            PopulateItemsList()

            ' Set parameter for Call Detail select statement
            SqlDataSource2.SelectParameters.Item("PhoneNumber").DefaultValue = varOrderCellNumber
            ' Set how to display sales reps. Either dropdown or label, depending on privilage.
            SetupSalesRep()
            ToggleConfirmSerial()
            ' Populate the plan details
            PopulatePlanDetails()
            SetOrderInfoBar()
            PopulateOrderForm()
        End If

    End Sub

    Private Sub SetupSalesRep()
        Dim level As Integer = Session("UserLevel")

        PopulateSalesRepDropdown(salesRepDropdown)

        If level = 3 Then
            ' Set default for the sales rep name
            salesRepDropdown.SelectedValue = Membership.GetUser.UserName
            salesRepDropdown.Enabled = False
        End If

    End Sub

    Private Sub SetupVendorPinField()
        If varOrderCellNumber = vbNullString Then
            vendorPin.ReadOnly = True
            cell_number.Attributes.Add("onchange", "SetVendorPin()")
        Else
            vendorPin.ReadOnly = False
            cell_number.Attributes.Remove("onchange")
        End If
    End Sub

    Private Sub PopulateSalesRepDropdown(ByRef salesRepDropdown As System.Web.UI.WebControls.DropDownList)

        connection.Open()
        Dim cmd As SqlCommand

        Dim sql As String = "SELECT [UserName] FROM [ASPNETDB].[dbo].[aspnet_Users]"
        cmd = New SqlCommand(sql, connection)
        Dim dtr As SqlDataReader = cmd.ExecuteReader

        salesRepDropdown.DataSource = dtr
        salesRepDropdown.DataTextField = "UserName"
        salesRepDropdown.DataBind()

        dtr.Close()
        connection.Close()

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
        sql = "SELECT TOP 1000 [planid], [planname] FROM [Plans] WHERE planref = 'Pay As You Go'"
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

        Dim sql As String = "SELECT * FROM [items] ORDER BY display_seq"
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

        item.Attributes.Add("onclick", "calculateTotal();TogglePayButton();")

        CType(e.Item.FindControl("itemLbl_"), HtmlGenericControl).ID = "itemLbl_" & e.Item.DataItem("item_id")

    End Sub

    Private Sub SetButtonProperties(ByVal state As String)

        'Basic customer information button
        order_basic.Text = state
        order_basic.CommandName = state

    End Sub

    Private Sub SetTabsEnabled(ByVal isEnabled As Boolean)
        TabPanel3.Enabled = isEnabled
        TabPanel4.Enabled = isEnabled
        TabPanel5.Enabled = isEnabled
        TabPanel6.Enabled = isEnabled
    End Sub

    Private Sub ToggleConfirmSerial()
        If varSerialNumber = vbNullString Then
            confirmSerialSpan.Attributes.Add("style", "display: block;")
            ' If the confirm box is shown always empty it.
            confirmSerialNumber.Text = ""
        Else
            confirmSerialSpan.Attributes.Add("style", "display: none;")
        End If
    End Sub

    Private Sub SetUpMonitorCheckBox(ByVal show As Boolean)

        Dim level As Integer = Session("UserLevel")
        If level = 1 Then
            monitor.Enabled = True
        End If

        Dim divState As String
        If show Then
            divState = "block"
        Else
            divState = "none"
        End If
        moniterChckBox.Attributes.Add("style", "display: " & divState & ";")

    End Sub


    ' Get data from database and set variables
    Private Sub GetOrderData(ByVal orderID As Integer)

        Dim sql As StringBuilder = New StringBuilder
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        con.Open()
        Dim cmd As SqlCommand

        sql.Append("SELECT")
        ' Order table
        sql.Append("[order_id] ,[cell_num] ,[create_date], [plan_id] ,[esn], [serial_num] ,")
        sql.Append("[monthly_plan_id], [monthly_auto_renew], [cash_plan_id], [cash_auto_renew], [intl_amt], [intl_auto_renew], ")
        sql.Append("[intl_pin] ,[carrier_name] ,[carrier_pin] ,[vendor_pin] ,[monitor] ,[sales_rep_id] , ")
        ' Customer table
        sql.Append("[customers].[customer_id], [initial_agent], [cus_pin] ,[fname] ,[lname] ,[prefix] ,[address] ,[city] ,[state] ,[phone] ,[zip] ,[email], ")
        sql.Append("[billing_fname] ,[billing_lname] ,[billing_address] ,[billing_phone] ,[billing_city] ,[billing_state], ")
        sql.Append("[billing_zip] ,[billing_email], [cc_last_four] ,[cc_expiration_date] ,[auth_custprof_id] ,[auth_payprof_id], ")
        ' MDN table
        sql.Append("[Status], [RatePlan], [Balance], [ExpDate], [PlanBalanceDetails], [PlanExpDate], [lastModified], [PINCount], ")
        ' Plans table
        sql.Append("[planname], [kosher] ")
        sql.Append("FROM [customers] INNER JOIN [orders] ON [customers].[customer_id] = [orders].[customer_id] ")
        sql.Append("LEFT JOIN [MDN] ON [MDN].[PhoneNumber] = [orders].[cell_num] ")
        sql.Append("LEFT JOIN Plans ON [orders].[plan_id] = [Plans].[planid] ")
        sql.Append("WHERE [order_id] = @order_id")

        cmd = New SqlCommand(sql.ToString(), con)

        cmd.Parameters.Add("@order_id", SqlDbType.Int).Value = orderID

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            varCustomerId = reader.Item("customer_id")

            If reader.Item("prefix") IsNot DBNull.Value Then
                varPrefix = reader.Item("prefix")
            End If

            If reader.Item("initial_agent") IsNot DBNull.Value Then
                varInitialAgent = reader.Item("initial_agent")
            End If

            If reader.Item("fname") IsNot DBNull.Value Then
                varFirstName = reader.Item("fname")
            End If

            If reader.Item("lname") IsNot DBNull.Value Then
                varLastName = reader.Item("lname")
            End If

            If reader.Item("address") IsNot DBNull.Value Then
                varAddress = reader.Item("address")
            End If

            If reader.Item("city") IsNot DBNull.Value Then
                varCity = reader.Item("city")
            End If

            If reader.Item("state") IsNot DBNull.Value Then
                varState = reader.Item("state")
            End If

            If reader.Item("phone") IsNot DBNull.Value Then
                varPhone = reader.Item("phone")
            End If

            If reader.Item("zip") IsNot DBNull.Value Then
                varZip = reader.Item("zip")
            End If

            If reader.Item("email") IsNot DBNull.Value Then
                varEmail = reader.Item("email")
            End If

            If reader.Item("cell_num") IsNot DBNull.Value Then
                varOrderCellNumber = reader.Item("cell_num")
            End If

            If reader.Item("Status") IsNot DBNull.Value Then
                varStatus = StrConv(reader.Item("Status"), VbStrConv.ProperCase)
            End If

            If reader.Item("RatePlan") IsNot DBNull.Value Then
                Dim str As String = reader.Item("RatePlan")
                If str.Contains("n Text") = True Then
                    varVendorName = str.Replace("n Text", "")
                ElseIf str.Contains("Standard") = True Then
                    varVendorName = "Pay Per Minute"
                Else
                    varVendorName = str
                End If

            End If

            If reader.Item("create_date") IsNot DBNull.Value Then
                varSignupDate = reader.Item("create_date")
            End If

            If reader.Item("Balance") IsNot DBNull.Value Then
                varBalance = FormatCurrency(reader.Item("Balance"), 2)
            End If

            If reader.Item("ExpDate") IsNot DBNull.Value Then
                varExpirationDate = reader.Item("ExpDate")
            End If

            If reader.Item("PlanExpDate") IsNot DBNull.Value Then
                varPlanExpirationDate = reader.Item("PlanExpDate")
            End If

            If reader.Item("lastModified") IsNot DBNull.Value Then
                Dim lastUpdate As Date = reader.Item("lastModified")
                varLastUpdate = lastUpdate.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))
            End If

            If reader.Item("esn") IsNot DBNull.Value Then
                varEsn = reader.Item("esn")
            End If

            If reader.Item("intl_pin") IsNot DBNull.Value Then
                varIntlPin = reader.Item("intl_pin")
            End If

            If reader.Item("serial_num") IsNot DBNull.Value Then
                varSerialNumber = reader.Item("serial_num")
            End If

            If reader.Item("carrier_name") IsNot DBNull.Value Then
                varCarrierName = reader.Item("carrier_name")
            End If

            If reader.Item("vendor_pin") IsNot DBNull.Value Then
                varVendorPin = reader.Item("vendor_pin")
            End If

            If reader.Item("monitor") IsNot DBNull.Value Then
                varMonitor = reader.Item("monitor")
            End If

            If reader.Item("sales_rep_id") IsNot DBNull.Value Then
                varSalesRepID = reader.Item("sales_rep_id")
            End If

            If reader.Item("cus_pin") IsNot DBNull.Value Then
                varCustomerPin = reader.Item("cus_pin")
            End If

            If reader.Item("order_id") IsNot DBNull.Value Then
                varOrderID = reader.Item("order_id")
            End If

            If reader.Item("plan_id") IsNot DBNull.Value Then
                varPlanID = reader.Item("plan_id")
            End If

            If reader.Item("planname") IsNot DBNull.Value Then
                varPlanName = reader.Item("planname")
            End If

            If reader.Item("kosher") IsNot DBNull.Value Then
                varIsKosherPlan = reader.Item("kosher")
            End If

            If reader.Item("monthly_plan_id") IsNot DBNull.Value Then
                varRenewalMonthlyId = reader.Item("monthly_plan_id")
            End If

            If reader.Item("monthly_auto_renew") IsNot DBNull.Value Then
                varIsMonthlyRenew = reader.Item("monthly_auto_renew")
            End If

            If reader.Item("cash_plan_id") IsNot DBNull.Value Then
                varRenewalCashId = reader.Item("cash_plan_id")
            End If

            If reader.Item("cash_auto_renew") IsNot DBNull.Value Then
                varIsCashRenew = reader.Item("cash_auto_renew")
            End If

            If reader.Item("intl_amt") IsNot DBNull.Value Then
                varRenewalIntl = FormatNumber(reader.Item("intl_amt"), 2)
            End If

            If reader.Item("intl_auto_renew") IsNot DBNull.Value Then
                varIsIntlRenew = reader.Item("intl_auto_renew")
            End If

            If reader.Item("PINCount") IsNot DBNull.Value Then
                varStackedPinCount = reader.Item("PINCount")
            End If

            If reader.Item("auth_custprof_id") IsNot DBNull.Value Then
                varAuthProfileID = reader.Item("auth_custprof_id")
                If reader.Item("billing_fname") IsNot DBNull.Value Then
                    varBillingFirstName = reader.Item("billing_fname")
                End If
                If reader.Item("billing_lname") IsNot DBNull.Value Then
                    varBillingLastName = reader.Item("billing_lname")
                End If
                If reader.Item("billing_address") IsNot DBNull.Value Then
                    varBillingAddress = reader.Item("billing_address")
                End If
                If reader.Item("billing_city") IsNot DBNull.Value Then
                    varBillingCity = reader.Item("billing_city")
                End If
                If reader.Item("billing_state") IsNot DBNull.Value Then
                    varBillingState = reader.Item("billing_state")
                End If
                If reader.Item("billing_zip") IsNot DBNull.Value Then
                    varBillingZip = reader.Item("billing_zip")
                End If
                If reader.Item("billing_phone") IsNot DBNull.Value Then
                    varBillingPhone = reader.Item("billing_phone")
                End If
                If reader.Item("billing_email") IsNot DBNull.Value Then
                    varBillingEmail = reader.Item("billing_email")
                End If
                If reader.Item("cc_last_four") IsNot DBNull.Value Then
                    varCCLastDigits = reader.Item("cc_last_four")
                End If
                If reader.Item("cc_expiration_date") IsNot DBNull.Value Then
                    varCCExpiration = reader.Item("cc_expiration_date")
                End If
            Else
                varBillingFirstName = varFirstName
                varBillingLastName = varLastName
                varBillingAddress = varAddress
                varBillingCity = varCity
                varBillingState = varState
                varBillingZip = varZip
                varBillingPhone = varPhone
                varBillingEmail = varEmail
            End If

        End If

        cmd = New SqlCommand("CallSummary", con)
        cmd.CommandType = Data.CommandType.StoredProcedure

        cmd.Parameters.Add("@pn", SqlDbType.VarChar).Value = varOrderCellNumber

        reader.Close()
        reader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()
            If reader.Item("Incoming minutes") IsNot DBNull.Value Then
                varIncomingMinutes = reader.Item("Incoming minutes")
            End If
            If reader.Item("Outgoing minutes") IsNot DBNull.Value Then
                varOutgoingMinutes = reader.Item("Outgoing minutes")
            End If
            If reader.Item("Total Minutes") IsNot DBNull.Value Then
                varTotalMinutes = reader.Item("Total Minutes")
            End If
        End If

        reader.Close()
        con.Close()

        ' Get data for last charges
        GetLastChargeAmounts()

    End Sub

    Private Sub GetLastChargeAmounts()
        Dim renewals As String = ""

        Dim sql As StringBuilder = New StringBuilder
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        sql.Append("SELECT monthly_amt, cash_amt, intl_amt, item_amt, month_plan_id, cash_plan_id FROM authtrans  ")
        sql.Append("WHERE orderid = @OrderID AND paydate = (SELECT MAX(paydate) FROM authtrans WHERE orderid = @OrderID)")

        con.Open()
        Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), con)

        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()

            If reader.Item("monthly_amt") IsNot DBNull.Value Then
                varLastChargeMonthly = FormatNumber(reader.Item("monthly_amt"), 2)
            End If

            If reader.Item("month_plan_id") IsNot DBNull.Value Then
                varLastChargeMonthPlanID = reader.Item("month_plan_id")
            End If

            If reader.Item("cash_amt") IsNot DBNull.Value Then
                varLastChargeCash = FormatNumber(reader.Item("cash_amt"), 2)
            End If

            If reader.Item("cash_plan_id") IsNot DBNull.Value Then
                varLastChargeCashPlanID = reader.Item("cash_plan_id")
            End If

            If reader.Item("intl_amt") IsNot DBNull.Value Then
                varLastChargeIntl = FormatNumber(reader.Item("intl_amt"), 2)
            End If

            If reader.Item("item_amt") IsNot DBNull.Value Then
                varLastChargeItem = FormatNumber(reader.Item("item_amt"), 2)
            End If

        End If

        con.Close()
    End Sub

    Private Function GetAgentCreditLimit(Optional ByVal userName As String = "", Optional ByVal isAjax As Boolean = False) As Decimal
        Dim creditLimit As Decimal = 0
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim sql As String = "SELECT [CreditLimit] FROM [ASPNETDB].[dbo].[aspnet_Users] WHERE [UserName] = @UserName"

        con.Open()

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        If userName = "" Then   ' This is executing on page load
            If varSalesRepID <> vbNullString Then
                cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = varSalesRepID
            Else
                cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text
            End If
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
            If reader.Item("CreditLimit") IsNot DBNull.Value Then
                creditLimit = reader.Item("CreditLimit")
            End If
        End If

        reader.Close()
        con.Close()

        Return creditLimit

    End Function

    Private Function GetAgentCreditTotal(Optional ByVal userName As String = "") As Decimal
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As String = "SELECT sum([total]) AS total FROM [authtrans] WHERE [agent] = @Agent AND charged = 1 AND trans_type = 2"

        Dim cmd As SqlCommand = New SqlCommand(sql, con)
        If userName = "" Then
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text
        Else
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = userName
        End If


        Dim reader As SqlDataReader = cmd.ExecuteReader

        Dim total As Decimal = 0

        If reader.HasRows Then
            reader.Read()
            If reader.Item("total") IsNot DBNull.Value Then
                total = reader.Item("total")
            End If
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
            If reader.Item("planref") = "Pay As You Go" And reader.Item("plan_cost") IsNot DBNull.Value Then
                cost = reader.Item("plan_cost")
            ElseIf reader.Item("planref") = "Monthly" And reader.Item("monthly_cost") IsNot DBNull.Value Then
                cost = reader.Item("monthly_cost")
            End If
        End If

        connection.Close()

        Return cost
    End Function

    Private Function GetTotalChargesForOrder() As Decimal
        connection.Open()

        Dim sql As String = "SELECT sum([total]) as total FROM [authtrans] WHERE [charged] = 1 AND [orderid] = @OrderID"
        Dim cmd As SqlCommand
        Dim total As Decimal = 0

        cmd = New SqlCommand(sql, connection)

        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            If reader.Item("total") IsNot DBNull.Value Then
                total = reader.Item("total")
            Else
                total = 0
            End If
        End If

        connection.Close()

        Return total
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
            If reader.Item("cost") IsNot DBNull.Value Then
                total = reader.Item("cost")
            End If
        End If

        connection.Close()

        Return total

    End Function

    Private Sub RefreshData()

        ' Need to reset the items list
        PopulateItemsList()

        GetOrderData(varOrderID)
        GetLastChargeAmounts()
        PopulatePlanDetails()
        SetOrderInfoBar()
        PopulateOrderForm()
        ToggleConfirmSerial()

        ' Reset the vendor pin field
        SetupVendorPinField()

    End Sub


    ' Functions for ajax calls.
    Private Sub GetPlanInfoFromSerial(ByVal serialNumber As String)
        connection.Open()

        Dim sql As String = "SELECT serial_num FROM orders WHERE serial_num = @SerialNumber AND [order_id] != @OrderId"

        Dim cmd As SqlCommand = New SqlCommand(sql, connection)

        cmd.Parameters.Add("@SerialNumber", SqlDbType.VarChar).Value = serialNumber
        cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            _callBackResult = "nonunique"
            Exit Sub
        End If
        reader.Close()

        sql = "SELECT * FROM [SerialESN] WHERE [Serial#] = @SerialNumber"
        cmd = New SqlCommand(sql, connection)
        cmd.Parameters.Add("@SerialNumber", SqlDbType.NVarChar).Value = serialNumber

        reader = cmd.ExecuteReader()

        If reader.HasRows Then
            Dim ds As New DataSet
            ds.Load(reader, LoadOption.PreserveChanges, "SerialESN")
            _callBackResult = ds.GetXml()
        Else
            _callBackResult = "invalid"
        End If

        connection.Close()
    End Sub

    Private Function GetPlanCost(ByVal planId As Integer, Optional ByVal isAjax As Boolean = False) As Decimal

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
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

        Dim argsParts As String() = args.Split("~")

        Dim monthlyParts = argsParts(0).Split(",")
        Dim monthlyPlanId As Integer = monthlyParts(0)
        Dim monthlyRenew As Boolean = monthlyParts(1)
        If Not monthlyRenew Then
            monthlyPlanId = 0
        End If

        Dim cashParts = argsParts(1).Split(",")
        Dim cashPlanId = cashParts(0)
        Dim cashRenew As Boolean = cashParts(1)
        If Not cashRenew Then
            cashPlanId = 0
        End If

        Dim intlParts = argsParts(2).Split(",")
        Dim intlCost = intlParts(0)
        Dim intlRenew As Boolean = intlParts(1)
        If Not intlRenew Then
            intlCost = 0
        End If

        connection.Open()

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("UPDATE orders SET [monthly_plan_id] = @MonthlyID , ")
        sql.Append("[monthly_auto_renew] = @MonthlyRenew, ")
        sql.Append("[cash_plan_id] = @CashID, ")
        sql.Append("[cash_auto_renew] = @CashRenew, ")
        sql.Append("[intl_amt] = @IntlAmnt, ")
        sql.Append("[intl_auto_renew] = @IntlRenew ")
        sql.Append("WHERE [order_id] = @OrderID ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), connection)

        cmd.Parameters.Add("@MonthlyID", SqlDbType.Int).Value = monthlyPlanId
        cmd.Parameters.Add("@MonthlyRenew", SqlDbType.Bit).Value = monthlyRenew

        cmd.Parameters.Add("@CashID", SqlDbType.Int).Value = cashPlanId
        cmd.Parameters.Add("@CashRenew", SqlDbType.Bit).Value = cashRenew

        cmd.Parameters.Add("@IntlAmnt", SqlDbType.Money).Value = intlCost
        cmd.Parameters.Add("@IntlRenew", SqlDbType.Bit).Value = intlRenew

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


    ' Set page data
    Private Sub SetOrderInfoBar()
        serailNumLbl.Text = varSerialNumber
        lblCellNumber.Text = varOrderCellNumber
        lblName.Text = varPrefix & " " & varFirstName & " " & varLastName

        initialAgentLbl.Text = varInitialAgent

        planNameBarLbl.Text = varPlanName
        statusLbl.Text = varStatus
        lastUpdatedLbl.Text = varLastUpdate
        balanceLbl.Text = varBalance
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

        prefix.Text = varPrefix
        fname.Text = varFirstName
        billingFname.Text = varBillingFirstName
        lname.Text = varLastName
        billingLname.Text = varBillingLastName
        address.Text = varAddress
        billingAddress.Text = varBillingAddress
        city.Text = varCity
        billingCity.Text = varBillingCity
        state.Text = varState
        billingState.Text = varBillingState
        phone.Text = varPhone
        billingPhone.Text = varBillingPhone
        zip.Text = varZip
        billingZip.Text = varBillingZip
        email.Text = varEmail
        billingEmail.Text = varBillingEmail

        If level > 0 Then
            If level = 1 Or level = 2 Then
                If varInitialAgent <> vbNullString Then
                    salesRepDropdown.SelectedIndex =
                        salesRepDropdown.Items.IndexOf(salesRepDropdown.Items.FindByValue(varInitialAgent))
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

        customerPin.Text = varCustomerPin

        If varMonitor = Nothing Then
            monitor.Checked = Session("UserMonitor")
        Else
            monitor.Checked = varMonitor
        End If

        If varCCLastDigits <> vbNullString Then
            creditCardNumber.Text = "************" & varCCLastDigits
        End If

        creditCardExpirationDate.Text = varCCExpiration

        Dim creditLimit = GetAgentCreditLimit()
        Dim creditUsed = GetAgentCreditTotal()
        Dim available = creditLimit - creditUsed

        creditLimitLbl.Text = FormatCurrency(creditLimit, 2)
        creditUsedLbl.Text = FormatCurrency(creditUsed, 2)
        creditAvailableLbl.Text = FormatCurrency(available, 2)

    End Sub

    Private Sub PopulatePlanDetails()

        SetUpMonitorCheckBox(varIsKosherPlan)

        invoiceFromDateLbl.Text = varExpirationDate

        ' Set monhtly invoice details
        monthlyPlanDropdown.Items.FindByValue(varLastChargeMonthPlanID).Selected = True
        monthlyPlanDropdownCostLbl.Text = FormatCurrency(varLastChargeMonthly, 2)

        ' Set monthly renewals
        If varRenewalMonthlyId > 0 Then
            renewalMonthlyDropdown.Items.FindByValue(varRenewalMonthlyId).Selected = True
        Else
            renewalMonthlyDropdown.SelectedIndex = 0
        End If
        monthlyRenewalChk.Checked = varIsMonthlyRenew

        ' Set cash invoice details
        cashBalanceDropdown.Items.FindByValue(varLastChargeCashPlanID).Selected = True
        cashBalanceDropdownCostLbl.Text = FormatCurrency(varLastChargeCash, 2)

        ' Set cash renewals
        If varRenewalCashId > 0 Then
            renewalCashDropdown.Items.FindByValue(varRenewalCashId).Selected = True
        Else
            renewalCashDropdown.SelectedIndex = 0
        End If
        cashRenewalChk.Checked = varIsCashRenew

        ' Set international balance invoice
        If intlBalanceDropdown.Items.FindByValue(varLastChargeIntl) IsNot Nothing Then
            intlBalanceDropdown.Items.FindByValue(varLastChargeIntl).Selected = True
        Else
            intlBalanceDropdown.SelectedIndex = 0
        End If
        intlBalanceDropdownCostLbl.Text = FormatCurrency(varLastChargeIntl, 2)

        ' Set international renewals
        renewalIntlDropdown.Items.FindByValue(varRenewalIntl).Selected = True
        intlRenewalChk.Checked = varIsIntlRenew

    End Sub

    Private Sub BindTransactionHistoryGrid()

        Dim sql As String = "SELECT [paydate], [monthly_amt], [cash_amt], [total], [user], [intl_amt], [item_amt], [agent], [authmessage], "
        sql &= "[trans_type] = CASE [trans_type]  WHEN 1 THEN 'Credit Card' WHEN 2 THEN 'Agent Account' ELSE '' End "
        sql &= "FROM [authtrans] WHERE [orderid] = " & varOrderID & " "
        sql &= "AND [charged] = 1 ORDER BY paydate desc"

        SqlDataSource3.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        SqlDataSource3.SelectCommandType = SqlDataSourceCommandType.Text
        SqlDataSource3.SelectCommand = sql
        SqlDataSource3.CancelSelectOnNullParameter = False

        transactionHistoryGridView.DataBind()
        connection.Close()
    End Sub

    Private Sub BindTransactionHistoryGridTab()

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("SELECT [paydate], [monthly_amt], [cash_amt], [total], [user], [intl_amt], [item_amt], [agent], [authmessage], ")
        sql.Append("[authtransid], [trans_type] = CASE [trans_type]  WHEN 1 THEN 'Credit Card' WHEN 2 THEN 'Agent Account' ELSE '' End ")
        sql.Append("FROM [authtrans] WHERE [orderid] = @OrderID ")
        sql.Append("AND [charged] = 1 ")
        sql.Append("ORDER BY paydate desc")

        SqlDataSource1.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        SqlDataSource1.SelectCommandType = SqlDataSourceCommandType.Text
        SqlDataSource1.SelectParameters.Item("OrderID").DefaultValue = varOrderID
        SqlDataSource1.SelectCommand = sql.ToString()
        SqlDataSource1.CancelSelectOnNullParameter = False

        transactionHistoryTab.DataBind()
        connection.Close()

        totalOrderChargesLbl.Text = FormatCurrency(GetTotalChargesForOrder(), 2)

    End Sub


    ' Handle user events
    Protected Sub OrderButton(ByVal s As Object, ByVal e As CommandEventArgs)
        If e.CommandName = "Save" Then
            SaveOrder()
        ElseIf e.CommandName = "Update" Then
            UpdateOrder()
        ElseIf e.CommandName = "Pay" Then
            ChargeOrder()
        End If
    End Sub

    Protected Sub CloseOrder(ByVal s As Object, ByVal e As CommandEventArgs)
        Response.Redirect("~/SearchOrder.aspx")
    End Sub


    ' Save order data to the database
    Private Sub SaveOrder()

        connection.Open()
        Dim cmd As SqlCommand = Nothing

        cmd = GetCustomerInformationSqlCommand("save")

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            Response.Redirect("~/Order.aspx?oid=" & reader.Item("order_id"))
        End If

        reader.Close()
        connection.Close()

    End Sub

    Private Sub UpdateOrder()

        Dim tabId As Integer = TabContainer1.ActiveTabIndex

        connection.Open()
        Dim cmd As SqlCommand = Nothing

        Select Case tabId
            Case 0      ' Basic information tab
                cmd = GetCustomerInformationSqlCommand("update")
            Case 1      ' Plan tab
                cmd = GetPlanInformationSqlCommand()
                ' Charges tab has its own function ChargeOrder
            Case Else
                Exit Sub
        End Select

        cmd.ExecuteNonQuery()
        connection.Close()

        RefreshData()

    End Sub

    Private Sub ChargeOrder()

        connection.Open()
        Dim cmd As SqlCommand = Nothing
        Dim sql As New StringBuilder
        ' Update customer information
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
        ' Update phone (order)
        sql.Append("UPDATE [orders] SET [update_date] = getdate(), ")
        sql.Append("[plan_id] = @PlanId, ")
        sql.Append("[sales_rep_id] = @SalesRep ")
        sql.Append("WHERE [order_id] = @OrderID")

        cmd = New SqlCommand(sql.ToString, connection)

        cmd.Parameters.Add("@customer_id", SqlDbType.Int).Value = varCustomerId
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

        If creditCardExpirationDate.Text.Length > 0 Then
            cmd.Parameters.Add("@ccExpDate", SqlDbType.VarChar).Value = creditCardExpirationDate.Text.Trim()
        Else
            cmd.Parameters.Add("@ccExpDate", SqlDbType.VarChar).Value = ""
        End If

        Dim level As Integer = Session("UserLevel")

        If level > 0 Then
            If level = 1 Or level = 2 Then
                cmd.Parameters.Add("@SalesRep", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text
            End If
            If level = 3 Then
                cmd.Parameters.Add("@SalesRep", SqlDbType.VarChar).Value = Membership.GetUser().UserName
            End If
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


        ' Get the currently selected items to maintain their state
        Dim ids As ArrayList = New ArrayList
        Dim chkc As HtmlInputCheckBox

        If Not chargeSuccessful Then
            For Each item As RepeaterItem In itemRepeater.Items
                chkc = CType(item.FindControl("itemsCheck"), HtmlInputCheckBox)
                If chkc.Checked Then
                    ids.Add(chkc.Value)
                End If
            Next
        End If

        RefreshData()

        If Not chargeSuccessful Then
            ' Reset the checkboxes
            For Each item As RepeaterItem In itemRepeater.Items
                chkc = CType(item.FindControl("itemsCheck"), HtmlInputCheckBox)
                If ids.Contains(chkc.Value) Then
                    chkc.Checked = True
                End If
            Next
        End If

        ' Repopulate the transaction history gridview
        BindTransactionHistoryGrid()
        BindTransactionHistoryGridTab()

    End Sub

    Private Function GetCustomerInformationSqlCommand(ByVal action As String) As SqlCommand

        Dim sql As String = vbNullString
        Dim cmd As SqlCommand

        If action = "save" Then
            sql = "INSERT INTO [customers] ([initial_agent], [fname], [lname], [prefix], [address], [city], [state], [phone], [zip], [email]) "
            sql = sql & "VALUES (@Agent, @fname, @lname, @prefix, @address, @city, @state, @phone, @zip, @email);"
            sql = sql & "INSERT INTO [orders] ([customer_id], [update_date]) VALUES (SCOPE_IDENTITY(), getdate());"  ' Insert customer_id into the orders table
            sql = sql & "SELECT SCOPE_IDENTITY() AS order_id;"
        End If

        If action = "update" Then
            sql = "UPDATE [customers] SET [prefix] = @prefix, "
            sql = sql & "[fname] = @fname, [lname] = @lname, [address] = @address, "
            sql = sql & "[city] = @city, [state] = @state, [phone] = @phone, [zip] = @zip, "
            sql = sql & "[email] = @email WHERE customer_id = @customer_id;"
            sql &= "UPDATE [orders] SET [update_date] = getdate() WHERE [order_id] = @OrderID"
        End If

        cmd = New SqlCommand(sql, connection)

        cmd.Parameters.Add("@customer_id", SqlDbType.Int).Value = varCustomerId

        ' Order ID for updating the order table
        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID

        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = Membership.GetUser.UserName

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

        Return cmd

    End Function

    Private Function GetPlanInformationSqlCommand() As SqlCommand
        Dim strB As New StringBuilder
        Dim cmd As SqlCommand

        strB.Append("UPDATE [orders] SET ")
        strB.Append("[cell_num] = @cell_number, ")
        strB.Append("[serial_num] = @serial_number, ")
        strB.Append("[esn] = @esn, ")
        strB.Append("[intl_pin] = @intl_pin, ")
        strB.Append("[carrier_name] = @carrier_name, ")
        strB.Append("[vendor_pin] = @vendor_pin, ")
        strB.Append("[monitor] = @monitor, ")
        strB.Append("[update_date] = getdate() ")
        strB.Append("WHERE order_id = @order_id;")
        strB.Append("UPDATE [customers] SET cus_pin = @customer_pin WHERE customer_id = @customer_id")

        cmd = New SqlCommand(strB.ToString(), connection)

        cmd.Parameters.Add("@customer_id", SqlDbType.Int).Value = varCustomerId
        cmd.Parameters.Add("@order_id", SqlDbType.Int).Value = varOrderID

        If cell_number.Text.Length > 0 Then
            cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = cell_number.Text
        Else
            cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = DBNull.Value
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

        Return cmd
    End Function


    ' Charge functions
    Private Function CreditCardCharge() As Boolean

        Dim chargeSuccessful As Boolean = False

        Dim AuthCode As String = ""
        Dim AuthMsg As String = ""

        Dim monthlyCost As Decimal = GetCostForCharge(monthlyPlanDropdown.SelectedValue)
        Dim cashCost As Decimal = GetCostForCharge(cashBalanceDropdown.SelectedValue)
        Dim intlCost As Decimal = intlBalanceDropdown.SelectedValue
        Dim itemsCost As Decimal = GetItemTotal()

        Dim total As Decimal = monthlyCost + cashCost + intlCost + itemsCost

        chargeSuccessful = RunCharge(varOrderID, creditCardNumber.Text, creditCardExpirationDate.Text, creditCardCode.Text,
                                monthlyCost, cashCost, intlCost, itemsCost, total, AuthCode, AuthMsg)

        authNoteLbl.Text = AuthMsg

        Return chargeSuccessful

    End Function

    Private Function AgentAccountCharge() As Boolean

        Dim chargeSuccessful As Boolean = False

        Dim monthlyCost As Decimal = GetCostForCharge(monthlyPlanDropdown.SelectedValue)
        Dim cashCost As Decimal = GetCostForCharge(cashBalanceDropdown.SelectedValue)
        Dim intlCost As Decimal = intlBalanceDropdown.SelectedValue

        Dim itemsCost As Decimal = GetItemTotal()

        Dim total As Decimal = monthlyCost + cashCost + intlCost + itemsCost

        Dim msg As String = ""

        chargeSuccessful = RunAgentAccountCharge(varOrderID, monthlyCost, cashCost, intlCost, itemsCost, total, msg)

        authNoteLbl.Text = msg

        Return chargeSuccessful

    End Function

    Private Sub SaveAuthTransItems(ByVal transId As Integer)

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As StringBuilder = New StringBuilder
        Dim chck As HtmlInputCheckBox
        Dim id As Integer = 0

        For Each item As RepeaterItem In itemRepeater.Items
            chck = CType(item.FindControl("itemsCheck"), HtmlInputCheckBox)

            id = chck.Value

            If chck.Checked Then
                sql.Append("INSERT INTO [AuthTransItems] ([trans_id], [item_id]) VALUES (" & transId & "," & id & ");")
            End If
        Next

        If sql.Length > 0 Then
            Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), con)
            cmd.ExecuteNonQuery()
        End If

        con.Close()

    End Sub


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

    End Sub

    Public Function GetCallbackResult() As String _
        Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function


    ' Auth functions
    Protected Function RunCharge(ByVal ordernum As Int16, ByVal ccnum As String,
                                 ByVal ccexp As String, ByVal code As String, ByVal monthly_amt As Decimal,
                                 ByVal cash_amt As Decimal, ByVal intl_amt As Decimal,
                                 ByVal item_amt As Decimal, ByVal total As Decimal,
                                 ByRef AuthCode As String, ByRef AuthMsg As String) As Boolean

        Dim svc1 = New svc.Service()

        connection.Open()
        Dim rdr As SqlDataReader
        Dim cmdstr As String

        Dim cclastfour As String = ccnum.Substring(ccnum.Length() - 4)

        cmdstr = "insert authtrans"
        cmdstr = cmdstr + "(orderid,trans_type,paydate,monthly_amt,cash_amt,"
        cmdstr = cmdstr + "intl_amt, item_amt, total, agent, [User], AuthCode, "
        cmdstr = cmdstr + "authtransid,authmessage,charged,cc_last_four,cc_expiration,"
        cmdstr = cmdstr + "billing_fname,billing_lname,"
        cmdstr = cmdstr + "billing_address,billing_city,billing_state,"
        cmdstr = cmdstr + "billing_zip,billing_phone,billing_email,auth_resp, "
        cmdstr = cmdstr + "month_plan_id, cash_plan_id)"
        cmdstr = cmdstr + "values(" + ordernum.ToString() + ", 1,"
        cmdstr = cmdstr + "getdate(),"
        cmdstr = cmdstr + monthly_amt.ToString() + ", " + cash_amt.ToString() + ", " + intl_amt.ToString() + ", " + item_amt.ToString() + ",  0, "
        cmdstr = cmdstr + "@Agent, '" & Membership.GetUser.UserName & "',"
        cmdstr = cmdstr + " '', '', '', 0, "
        cmdstr = cmdstr + "'" + cclastfour + "','" + ccexp + "',"
        'cmdstr = cmdstr + "'" + payprof.billTo.firstName + "','" + payprof.billTo.lastName + "','"
        'cmdstr = cmdstr + "'" + payprof.billTo.address + "','" + payprof.billTo.city + "','" + payprof.billTo.state + "','"
        'cmdstr = cmdstr + "'" + payprof.billTo.zip + ",'" 
        cmdstr = cmdstr + "'','','','','','',"
        cmdstr = cmdstr + "'','', "  'phone/email
        cmdstr = cmdstr + " '', @MonthPlanID, @CashPlanID );"
        cmdstr = cmdstr + "SELECT SCOPE_IDENTITY() AS trans_id; "

        Dim cmd0 As New SqlCommand(cmdstr, connection)

        cmd0.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text

        cmd0.Parameters.Add("@MonthPlanID", SqlDbType.Int).Value = monthlyPlanDropdown.SelectedValue
        cmd0.Parameters.Add("@CashPlanID", SqlDbType.Int).Value = cashBalanceDropdown.SelectedValue

        Dim insertTransReader = cmd0.ExecuteReader()
        Dim transId As Integer = 0

        If insertTransReader.HasRows Then
            insertTransReader.Read()
            transId = insertTransReader.Item("trans_id")
        End If
        ' Close the reader
        insertTransReader.Close()

        Try

            cmdstr = "select * from orders o join customers c on o.customer_id = c.customer_id where order_id = " + ordernum.ToString()

            Dim cmd As New SqlCommand(cmdstr, connection)

            rdr = cmd.ExecuteReader()
            If Not rdr.Read() Or Not rdr.HasRows() Then
                AuthMsg = "Invalid Order"
                Dim sql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transId
                Dim invalidOrderCmd = New SqlCommand(sql, connection)
                invalidOrderCmd.ExecuteNonQuery()
                Return False
            End If

        Catch ex As Exception
            AuthMsg = ex.Message
            Dim orderExSql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transId
            Dim exCmd = New SqlCommand(orderExSql, connection)
            exCmd.ExecuteNonQuery()
            Return False
        End Try

        Dim merchant = New svc.MerchantAuthenticationType()
        merchant.name = ConfigurationManager.ConnectionStrings("authnet.Merchant").ConnectionString
        merchant.transactionKey = ConfigurationManager.ConnectionStrings("authnet.Key").ConnectionString

        Dim custid As Long = GetIntValue(rdr, "customer_id")
        Dim custprofid As Long = GetIntValue(rdr, "auth_custprof_id")
        Dim payprofid As Long = GetIntValue(rdr, "auth_payprof_id")

        '7/3/12 cmb added these 10 variables
        Dim custProfDescription As String = GetStrValue(rdr, "lname")
        Dim custProfEmail As String = GetStrValue(rdr, "billing_email")
        Dim custProfMerchCustId As String = GetStrValue(rdr, "customer_id")
        Dim paymProfFirstName As String = GetStrValue(rdr, "billing_fname")
        Dim paymProfLastName As String = GetStrValue(rdr, "billing_lname")
        Dim paymProfAddress As String = GetStrValue(rdr, "billing_address")
        Dim paymProfCity As String = GetStrValue(rdr, "billing_city")
        Dim paymProfState As String = GetStrValue(rdr, "billing_state")
        Dim paymProfZip As String = GetStrValue(rdr, "billing_zip")
        Dim paymProfPhoneNumber As String = GetStrValue(rdr, "billing_phone")

        Dim custprof = New svc.CustomerProfileType()
        Dim custprofresp = New svc.CreateCustomerProfileResponseType()
        Dim payprof As New svc.CustomerPaymentProfileType()

        If custprofid = 0 Then

            custprof.description = custProfDescription
            custprof.email = custProfEmail
            custprof.merchantCustomerId = custProfMerchCustId
            custprofresp = svc1.CreateCustomerProfile(merchant, custprof, svc.ValidationModeEnum.none)

            If Not custprofresp.messages(0).code = "I00001" Then
                If custprofresp.messages(0).code = "E00039" Then
                    AuthMsg = "Duplicate Customer"
                Else
                    AuthMsg = custprofresp.messages(0).text
                End If
                ' Close the reader
                rdr.Close()

                Dim failedAuthSql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transId
                Dim failedAuthCmd = New SqlCommand(failedAuthSql, connection)
                failedAuthCmd.ExecuteNonQuery()
                connection.Close() '7/3/12 cmb
                Return False
            End If

            custprofid = custprofresp.customerProfileId

            payprof.billTo = New svc.CustomerAddressType
            payprof.billTo.firstName = paymProfFirstName
            payprof.billTo.lastName = paymProfLastName
            payprof.billTo.address = paymProfAddress
            payprof.billTo.city = paymProfCity
            payprof.billTo.state = paymProfState
            payprof.billTo.zip = paymProfZip
            payprof.billTo.phoneNumber = paymProfPhoneNumber

            payprof.payment = New svc.PaymentType
            payprof.payment.Item = New svc.CreditCardType
            payprof.payment.Item.cardNumber = ccnum
            payprof.payment.Item.expirationDate = ccexp
            payprof.payment.Item.cardCode = code

            Dim payprofileresp = New svc.CreateCustomerPaymentProfileResponseType()
            payprofileresp = svc1.CreateCustomerPaymentProfile(merchant, custprofid, payprof, svc.ValidationModeEnum.none)

            If Not payprofileresp.resultCode = "0" Then
                Dim msg As New svc.MessagesTypeMessage
                msg = payprofileresp.messages(0)
                AuthMsg = msg.text
                ' Close the reader
                rdr.Close()

                Dim failedAuthSql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transId
                Dim failedAuthCmd = New SqlCommand(failedAuthSql, connection)
                failedAuthCmd.ExecuteNonQuery()
                connection.Close() '7/3/12 cmb

                '''''''''''''''''
                If Session.Item("UserLevel") = "admin" Then
                    Response.Write("ccexp: " & ccexp)
                End If
                Return False
            End If

            payprofid = payprofileresp.customerPaymentProfileId

            rdr.Close()

            'Update Db
            cmdstr = "update customers set auth_custprof_id = " + custprofid.ToString() + ", auth_payprof_id = " + payprofid.ToString() + " where customer_id = " + custid.ToString()
            Dim cmd1 As New SqlCommand(cmdstr, connection)
            Try
                cmd1.ExecuteNonQuery()
            Catch e As Exception
                AuthMsg = "error - Could Not save payprof"
                Dim failedCustSql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transId
                Dim failedCustCmd = New SqlCommand(failedCustSql, connection)
                failedCustCmd.ExecuteNonQuery()
                Return False
            End Try
        Else
            rdr.Close()
        End If

        ' Set profile ids for transaction
        Dim setProfileIds As String = "UPDATE [authtrans] SET [auth_custprof_id] = @CusProfId, "
        setProfileIds &= "[auth_payprof_id] = @PayProfId WHERE [transId] = " & transId
        Dim profileIdsCmd = New SqlCommand(setProfileIds, connection)

        profileIdsCmd.Parameters.Add("@CusProfId", SqlDbType.Int).Value = custprofid
        profileIdsCmd.Parameters.Add("@PayProfId", SqlDbType.Int).Value = payprofid

        profileIdsCmd.ExecuteNonQuery()


        Dim authpay = New svc.ProfileTransactionType
        authpay.Item = New svc.ProfileTransAuthCaptureType
        authpay.Item.amount = total
        authpay.Item.cardCode = code
        authpay.Item.customerProfileId = custprofid
        authpay.Item.customerPaymentProfileId = payprofid

        Dim authresp = New svc.CreateCustomerProfileTransactionResponseType
        authresp = svc1.CreateCustomerProfileTransaction(merchant, authpay, "x_duplicate_window=1") '7/2/12 cmb - added extraOptions to remove 2 minute security block on a customer's account


        '7/2/12 cmb - if a customer's data changed, update the Auth.net profile
        If StrComp(lname.Text, custProfDescription) <> 0 Or StrComp(email.Text, custProfEmail) <> 0 Or _
            StrComp(billingFname.Text, paymProfFirstName) <> 0 Or StrComp(billingLname.Text, paymProfLastName) <> 0 Or _
            StrComp(billingAddress.Text, paymProfAddress) <> 0 Or StrComp(billingCity.Text, paymProfCity) <> 0 Or _
            StrComp(billingState.Text, paymProfState) <> 0 Or StrComp(billingZip.Text, paymProfZip) <> 0 Or _
            StrComp(billingPhone.Text, paymProfPhoneNumber) <> 0 Or StrComp(creditCardNumber.Text, ccnum) <> 0 Or _
            StrComp(creditCardExpirationDate.Text, ccexp) <> 0 Or StrComp(creditCardCode.Text, code) <> 0 Then


            'update customer profile
            Dim updProfile = New svc.CustomerProfileExType
            updProfile.customerProfileId = custprofid
            updProfile.merchantCustomerId = custProfMerchCustId
            updProfile.description = custProfDescription
            updProfile.email = custProfEmail

            Dim updCustprofresp = New svc.UpdateCustomerProfileResponseType()
            updCustprofresp = svc1.UpdateCustomerProfile(merchant, updProfile)


            'update payment profile
            Dim updPayProf As New svc.CustomerPaymentProfileExType()
            updPayProf.billTo = New svc.CustomerAddressType
            updPayProf.billTo.firstName = paymProfFirstName
            updPayProf.billTo.lastName = paymProfLastName
            updPayProf.billTo.address = paymProfAddress
            updPayProf.billTo.city = paymProfCity
            updPayProf.billTo.state = paymProfState
            updPayProf.billTo.zip = paymProfZip
            updPayProf.billTo.phoneNumber = paymProfPhoneNumber

            updPayProf.payment = New svc.PaymentType
            updPayProf.payment.Item = New svc.CreditCardType
            updPayProf.payment.Item.cardNumber = ccnum
            updPayProf.payment.Item.expirationDate = ccexp
            updPayProf.payment.Item.cardCode = code

            Dim updPayProfileResp = New svc.UpdateCustomerPaymentProfileResponseType()
            updPayProfileResp = svc1.UpdateCustomerPaymentProfile(merchant, custprofid, updPayProf, svc.ValidationModeEnum.none)


        End If

        '  Dim settings(1) As svc.SettingType

        ' settings(0) = New svc.SettingType()
        ' settings(0).settingName = "hostedprofilereturnurl"
        ' settings(0).settingValue = "http://localhost/authrun/credit.aspx"
        ' settings(1) = New svc.SettingType()
        ' settings(1).settingName = "hostedprofilepagebordervisible"
        ' settings(1).settingValue = "true"


        'Dim resp = svc1.GetHostedProfilePage(merchant, custprofid.customerProfileId, settings)
        'strToken = resp.token
        'token.value = strToken

        Dim charged As Boolean = False
        Dim resparr() As String
        resparr = Split(authresp.directResponse, ",")

        If resparr.Length() > 6 Then

            AuthCode = resparr(4).ToString() ' authresp.messages(0).text 'resparr(3)
            AuthMsg = resparr(3)  'authresp.directResponse    'resparr(3)


            If authresp.resultCode = 0 Then
                charged = True
            Else
                charged = False
                total = 0
            End If

            Dim successSql As StringBuilder = New StringBuilder
            successSql.Append("UPDATE [authtrans] SET [charged] = @Charged, ")
            successSql.Append("[total] = @Total, ")
            successSql.Append("[authcode] = @AuthCode, ")
            successSql.Append("[authtransid] = @AuthTransId, ")
            successSql.Append("[authmessage] = @AuthMsg, ")
            successSql.Append("[auth_resp] = @AuthResponse ")

            successSql.Append("WHERE [transid] = @TransID ")

            'cmdstr = "insert authtrans"
            'cmdstr = cmdstr + "(orderid,trans_type,paydate,monthly_amt,cash_amt,"
            'cmdstr = cmdstr + "intl_amt, item_amt, total, agent, [User], AuthCode, "
            'cmdstr = cmdstr + "authtransid,authmessage,charged,cc_last_four,cc_expiration,"
            'cmdstr = cmdstr + "billing_fname,billing_lname,"
            'cmdstr = cmdstr + "billing_address,billing_city,billing_state,"
            'cmdstr = cmdstr + "billing_zip,billing_phone,billing_email,auth_resp)"
            'cmdstr = cmdstr + "values(" + ordernum.ToString() + ", 1,"
            'cmdstr = cmdstr + "getdate(),"
            'cmdstr = cmdstr + monthly_amt.ToString() + ", " + cash_amt.ToString() + ", " + intl_amt.ToString() + ", " + item_amt.ToString() + "," + total.ToString() + ","
            'cmdstr = cmdstr + "@Agent, '" & Membership.GetUser.UserName & "',"
            'cmdstr = cmdstr + "'" + resparr(4).ToString() + "','" + resparr(6).ToString() + "','" + resparr(3).ToString() + "'," + charged + ","
            'cmdstr = cmdstr + "'" + cclastfour + "','" + ccexp + "',"
            ''cmdstr = cmdstr + "'" + payprof.billTo.firstName + "','" + payprof.billTo.lastName + "','"
            ''cmdstr = cmdstr + "'" + payprof.billTo.address + "','" + payprof.billTo.city + "','" + payprof.billTo.state + "','"
            ''cmdstr = cmdstr + "'" + payprof.billTo.zip + ",'" 
            'cmdstr = cmdstr + "'','','','','','',"
            'cmdstr = cmdstr + "'','',"  'phone/email
            'cmdstr = cmdstr + "'" + authresp.directResponse + " ')"

            Dim cmd3 As New SqlCommand(successSql.ToString(), connection)

            cmd3.Parameters.Add("@Charged", SqlDbType.Bit).Value = charged
            cmd3.Parameters.Add("@Total", SqlDbType.Money).Value = total
            cmd3.Parameters.Add("@TransID", SqlDbType.Int).Value = transId

            cmd3.Parameters.Add("@AuthCode", SqlDbType.VarChar).Value = resparr(4).ToString()
            cmd3.Parameters.Add("@AuthTransId", SqlDbType.VarChar).Value = resparr(6).ToString()
            cmd3.Parameters.Add("@AuthMsg", SqlDbType.VarChar).Value = resparr(3).ToString()

            cmd3.Parameters.Add("@AuthResponse", SqlDbType.VarChar).Value = authresp.directResponse

            cmd3.ExecuteNonQuery()

            SaveAuthTransItems(transId)

            'Try
            '    cmd3.ExecuteNonQuery()
            'Catch e As Exception
            '    AuthMsg = "error - Could Not save authtrans"
            '    Return False
            'End Try

        Else
            AuthMsg = authresp.messages(0).text
            rdr.Close()
            Dim emptyResponseSql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transId
            Dim emptyResponseCmd = New SqlCommand(emptyResponseSql, connection)
            emptyResponseCmd.ExecuteNonQuery()
        End If

        connection.Close()
        Return charged

    End Function

    Function GetStrValue(ByRef rdr As SqlDataReader, ByRef Field As String) As String
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            Return ""
        Else
            Return rdr.GetValue(rdr.GetOrdinal(Field)).ToString()
        End If
    End Function

    Function GetIntValue(ByRef rdr As SqlDataReader, ByRef Field As String) As Int32
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            Return 0
        Else
            Return rdr.GetValue(rdr.GetOrdinal(Field))
        End If
    End Function

    Private Function RunAgentAccountCharge(ByVal orderID As Integer, ByVal monthlyAmnt As Decimal,
                                           ByVal cashamt As Decimal, ByVal intlAmnt As Decimal,
                                           ByVal itemsAmnt As Decimal, ByVal total As Decimal,
                                           ByRef msg As String) As Boolean

        connection.Open()

        Dim charged As Boolean = False

        Dim creditLimit = GetAgentCreditLimit(salesRepDropdown.SelectedItem.Text)
        Dim creditUsed = GetAgentCreditTotal()

        Dim allAgentCharges = total + creditUsed

        If allAgentCharges >= creditLimit Then
            msg = "Transaction amount exceeds credit limit."
            charged = False
            total = 0
        Else
            msg = "Transaction successful"
            charged = True
        End If

        Dim sql As StringBuilder = New StringBuilder

        sql.Append("INSERT INTO [authtrans] ")
        sql.Append("(orderID, trans_type, paydate, monthly_amt, cash_amt, ")
        sql.Append("intl_amt, item_amt, total, [user], agent,")
        sql.Append("authmessage, charged, billing_fname, billing_lname, ")
        sql.Append("billing_address, billing_city, billing_state,")
        sql.Append("billing_zip, billing_phone, billing_email, month_plan_id, cash_plan_id)")
        sql.Append("VALUES (@OrderID, 2, getdate(), @MonthlyAmt, @CashAmt, @IntlAmt, @ItemAmt, @Total, @User, @Agent, @Msg, @Charged,")
        sql.Append("@bFname, @bLname, @bAddress, @bCity, @bState, @bZip, @bPhone, @bEmail, @MonthPlanID, @CashPlanID);")
        sql.Append("SELECT SCOPE_IDENTITY() AS trans_id; ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, connection)

        cmd.Parameters.Add("@OrderID", SqlDbType.VarChar).Value = varOrderID

        cmd.Parameters.Add("@MonthlyAmt", SqlDbType.VarChar).Value = monthlyAmnt

        cmd.Parameters.Add("@CashAmt", SqlDbType.VarChar).Value = cashamt

        cmd.Parameters.Add("@IntlAmt", SqlDbType.VarChar).Value = intlAmnt

        cmd.Parameters.Add("@ItemAmt", SqlDbType.VarChar).Value = itemsAmnt

        cmd.Parameters.Add("@Total", SqlDbType.VarChar).Value = total

        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text

        cmd.Parameters.Add("@User", SqlDbType.VarChar).Value = Membership.GetUser.UserName

        cmd.Parameters.Add("@Msg", SqlDbType.VarChar).Value = msg

        If varBillingFirstName <> vbNullString Then
            cmd.Parameters.Add("@bFname", SqlDbType.VarChar).Value = varBillingFirstName
        Else
            cmd.Parameters.Add("@bFname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If varBillingLastName <> vbNullString Then
            cmd.Parameters.Add("@bLname", SqlDbType.VarChar).Value = varBillingLastName
        Else
            cmd.Parameters.Add("@bLname", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If varBillingAddress <> vbNullString Then
            cmd.Parameters.Add("@bAddress", SqlDbType.VarChar).Value = varBillingAddress
        Else
            cmd.Parameters.Add("@bAddress", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If varBillingCity <> vbNullString Then
            cmd.Parameters.Add("@bCity", SqlDbType.VarChar).Value = varBillingCity
        Else
            cmd.Parameters.Add("@bCity", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If varBillingState <> vbNullString Then
            cmd.Parameters.Add("@bState", SqlDbType.VarChar).Value = varBillingState
        Else
            cmd.Parameters.Add("@bState", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If varBillingZip <> vbNullString Then
            cmd.Parameters.Add("@bZip", SqlDbType.VarChar).Value = varBillingZip
        Else
            cmd.Parameters.Add("@bZip", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If varBillingPhone <> vbNullString Then
            cmd.Parameters.Add("@bPhone", SqlDbType.VarChar).Value = varBillingPhone
        Else
            cmd.Parameters.Add("@bPhone", SqlDbType.VarChar).Value = DBNull.Value
        End If

        If varBillingEmail <> vbNullString Then
            cmd.Parameters.Add("@bEmail", SqlDbType.VarChar).Value = varBillingEmail
        Else
            cmd.Parameters.Add("@bEmail", SqlDbType.VarChar).Value = DBNull.Value
        End If

        cmd.Parameters.Add("@Charged", SqlDbType.Bit).Value = charged

        cmd.Parameters.Add("@MonthPlanID", SqlDbType.Int).Value = monthlyPlanDropdown.SelectedValue
        cmd.Parameters.Add("@CashPlanID", SqlDbType.Int).Value = cashBalanceDropdown.SelectedValue

        Dim reader As SqlDataReader = cmd.ExecuteReader()
        Dim transId As Integer = 0
        If reader.HasRows Then
            reader.Read()
            transId = reader.Item("trans_id")
        End If

        If charged Then
            SaveAuthTransItems(transId)
        End If

        connection.Close()

        Return charged

    End Function

End Class