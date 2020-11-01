Imports System.Globalization
Imports System.Xml
Imports System.Net
Imports System.IO
Imports System.Data.SqlClient

Partial Class Order
    Inherits System.Web.UI.Page
    Implements System.Web.UI.ICallbackEventHandler

    Private connection As SqlConnection
    Private _callBackResult As String = Nothing
    Private _isNewOrder As Boolean = False
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
    Private varCarrierName As String = ""
    Private varVendorPin As String = vbNullString
    Private varMonitor As Boolean
    Private varStatus As String = vbNullString
    Private varExpirationDate As String = vbNullString
    Private varPlanExpirationDate As String = vbNullString
    Private varIsOrderClosed As Boolean = False
    Private varOrderNote As String = Nothing
    Private varVerStatusMsg As String = ""
    Private varValidESN As Boolean = True
    Private varATCVerify As String = ""
    Private varLastValidated As String = ""

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
    'Calendar
    'Private startDate As String = "2012-10-17 16:45:05.500"

    'Private startDate As String = ""
    'Private endDate As String = "2014-07-16 13:38:15.353"
    Private startDate As String
    Private endDate As String
    'Private useCalendar As Boolean = True



    ' Page initialization
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Response.Expires = -1
        'ToggleCalendar()
        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        If Session.Item("UserLevel") Is Nothing Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        varMonitor = Session("UserMonitor")
        connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        Dim orderID As Integer = Integer.Parse(StringDecode(Request.QueryString("oid")))
        If orderID <> Nothing Then
            _isNewOrder = False
            varOrderID = orderID
        Else
            _isNewOrder = True
        End If

        ' To prevent resubmits we redirect, however we lose ViewState so we maintain
        ' some states in the session as a serialized string
        If Not IsNothing(Session("_RedirectSaveState")) Then
            Dim parts() As String = CType(Session("_RedirectSaveState"), String).Split("&")
            TabContainer1.ActiveTabIndex = CType(CType(parts(0), String).Split("=")(1), Integer)
            selectPaymentMethod.SelectedIndex = CType(CType(parts(1), String).Split("=")(1), Integer)
            Session.Remove("_RedirectSaveState")
        End If

        ' Register client code.
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
            'ToggleCalendar()

        End If

        If Not HasPermission() Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        SetupVendorPinField()
        ToggleAdministrativeComponents()

        ' Clear the error messages.
        toggleOrderErrorMsg.InnerText = ""

    End Sub

    Private Sub InitializePage()

        If _isNewOrder Then
            SetSaveOrderBtnProperties("Save")
            DisableTabs()
            ToggleConfirmSerial()
            ToggleConfirmCellNumber()
            SetupInitialAgentDropdown()
        Else
            ' Initialize page components.
            SetSaveOrderBtnProperties("Update")

            ' Set how to display sales reps depending on user level.
            SetupSalesRep()
            PopulateItemsList()
            SetUpCCExpDropdowns()
            GetOrderData()

            ' These function are order dependent so they work only after the GetOrderData call.
            SetOrderInfoBar()
            PopulateOrderForm()
            PopulateAllPlanDropdowns()
            PopulateRenewals()
            SetSaveNoteBtnProps()
            SetupNumberOfCallDetailsIntervals()
            ' BindCallDetailGridview uses the value set in SetupNumberOfCallDetailsIntervals.
            BindCallDetailGridview()
            BindTransactionHistoryGrid()
            BindEquipmentGridview()
            BindActivityGv()
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
        If _isNewOrder Then
            vendorPin.ReadOnly = True
            cell_number.Attributes("onchange") = "SetVendorPin();showCellNumberConfirm();isCellNumberUnique();"
        Else
            vendorPin.ReadOnly = False
            cell_number.Attributes("onchange") = "showCellNumberConfirm();isCellNumberUnique();"
        End If
    End Sub

    Private Sub PopulateAllPlanDropdowns()
        connection.Open()
        Dim cmd As SqlCommand

        ' The monthly plan dropdown
        Dim sql As String = "SELECT [planid], [planname] FROM [Plans] WHERE planref = 'Monthly' AND [carrier] = @carrier ORDER BY [planname]"
        cmd = New SqlCommand(sql, connection)
        cmd.Parameters.Add("@carrier", SqlDbType.VarChar).Value = varCarrierName

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
        sql = "SELECT [planid], [planname] FROM [Plans] WHERE planref = 'Pay As You Go' AND [carrier] = @carrier"
        cmd = New SqlCommand(sql, connection)
        cmd.Parameters.Add("@carrier", SqlDbType.VarChar).Value = varCarrierName

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
        If IsNothing(varOrderNote) OrElse varOrderNote.Trim().Length = 0 Then
            saveOrderNoteBtn.Text = "Save"
            saveOrderNoteBtn.CommandName = "save"
            notesTab.HeaderText = "Notes"
        Else
            saveOrderNoteBtn.Text = "Update"
            saveOrderNoteBtn.CommandName = "update"
            notesTab.HeaderText = "Notes*"
        End If

    End Sub

    Private Sub DisableTabs()
        ' On a new order we force the user to complete customer information
        ' by disabling other tabs.
        For Each tab As AjaxControlToolkit.TabPanel In TabContainer1.Tabs()
            If tab.ID = "TabPanel1" Then   ' Customer tab
                tab.Enabled = True
            Else
                tab.Enabled = False
            End If
        Next
    End Sub
    'Public Sub useCalendarBtn()
    '    useCalendar.Value = "true"
    '    Response.Write(useCalendar.Value)
    'End Sub

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

    Private Sub ToggleAdministrativeComponents()

        Dim userLevel As Integer = CType(Session.Item("UserLevel"), Integer)
        
        Select Case userLevel
            Case 1  'Admin
                If _isNewOrder Then
                    closeOrderButton.Visible = False
                Else
                    closeOrderButton.Visible = True
                End If
                administrationTab.Visible = True
            Case 2  ' Office
                closeOrderButton.Visible = False
                'administrationTab.Visible = False 'FK commented this out on 4/29/2014 as per Mr. Lieberman/Mr. Greenberg's request
            Case 3  ' Agent
                closeOrderButton.Visible = False
                ' administrationTab.Visible = False '12/23/14 - Moved to adminTasksRadioListLoad to allow some choices for agents
            Case Else   ' Somethings is wrong, logout user.
                FormsAuthentication.SignOut()
                Response.Redirect("~/")
            End Select

    End Sub

    Protected Sub carrierName_Load(sender As Object, e As System.EventArgs) Handles carrierName.Load
        carrierName.Enabled = _isNewOrder

        If CType(Session.Item("UserLevel"), Integer) < 3 Then
            carrierName.Enabled = False - 10 / 22 / 14
            'If carrierName.Items.Count() > 2 Then
            '  carrierName.Items.RemoveAt(2)
            'End If
        Else
            carrierName.Enabled = _isNewOrder
        End If
     
    End Sub
    
    Protected Sub adminTasksRadiolist_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles adminTasksRadiolist.Load

        Dim userLevel As Integer = CType(Session.Item("UserLevel"), Integer)

        If Not _isNewOrder Then

            ' <asp:ListItem Text="Convert to Verizon" Value="convertVER"></asp:ListItem>
            ' <asp:ListItem Text="Convert to All Talk" Value="convertAT"></asp:ListItem>
            ' <asp:ListItem Text="All Talk Provision" Value ="ATprovision"></asp:ListItem>
            ' <asp:ListItem Text="Disconnect" Value="disconnect"></asp:ListItem>
            ' <asp:ListItem Text="Convert to Page Plus" Value="convertPP"></asp:ListItem>
            '  <asp:ListItem Text="New Verizon MDN" Value="newVER"></asp:ListItem>
            ' <asp:ListItem Text="Early Renewal" Value="updateStack"></asp:ListItem>
            ' <asp:ListItem Text="Decrement Stack" Value="removeStack"></asp:ListItem>

           
            Try
                If varCarrierName.ToLower = "verizon" Then

                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertAT")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertVER")))
                    'adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertPP")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("removeStack")))
                    'adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("disconnect")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("ATprovision")))
                    'adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("newVER")))

                    If Not varStackedPinCount > 0 Then
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("updateStack")))
                    End If

                    If userLevel = 3 Then 'Agent   
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertPP")))
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("disconnect")))
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("newVER")))
                    End If

                    If Not varStackedPinCount > 0 Then adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("updateStack")))


                ElseIf varCarrierName.ToLower = "page plus" Then


                    'adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertAT")))
                    'adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertVER")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertPP")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("updateStack")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("removeStack")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("disconnect")))
                    'adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("ATprovision")))
                    'adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("newVER")))

                    If userLevel = 3 Then
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertAT")))
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("ATprovision")))
                    End If

                ElseIf varCarrierName.ToLower = "all talk" Then
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("convertAT")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("disconnect")))
                    adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("ATprovision")))

                    If Not varStackedPinCount > 0 Then
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("updateStack")))
                        adminTasksRadiolist.Items().RemoveAt(adminTasksRadiolist.Items().IndexOf(adminTasksRadiolist.Items().FindByValue("removeStack")))
                    End If
                End If

            Catch ex As ArgumentOutOfRangeException
            End Try

            If String.IsNullOrEmpty(varOrderCellNumber) Then
                adminTasksRadiolist.Enabled = False
            Else
                adminTasksRadiolist.Enabled = True
            End If

        End If
    End Sub
    
    Protected Sub moniterChckBox_Load(sender As Object, e As System.EventArgs) Handles moniterChckBox.Load
        
        Dim level As Integer = Session("UserLevel")
        If level = 1 Then
            monitor.Enabled = True
        Else
            monitor.Enabled = False
        End If
        
        If Not _isNewOrder Then
            If varCarrierName.ToLower = "page plus" Then
                moniterChckBox.Visible = True
                moniterChckBox.Style("display") = If( varIsKosherPlan, "block", "none" )
            ElseIf varCarrierName.ToLower = "verizon" Then
                moniterChckBox.Visible = False
            End If
        Else
            moniterChckBox.Visible = True
            monitor.Checked = varMonitor
        End If

    End Sub
    
    Protected Sub orderCallDetails_DataBound(sender As Object, e As System.EventArgs) Handles orderCallDetails.DataBound
        If orderCallDetails.Rows.Count > 0 Then
            downloadCdrBtn.Visible = True
        Else
            downloadCdrBtn.Visible = False
        End If
    End Sub
    
    Protected Sub authNoteLbl_Load(sender As Object, e As System.EventArgs) Handles authNoteLbl.Load
        If Not IsPostBack Then
            If Not IsNothing(Session("TransMsg")) Then
                authNoteLbl.Text = CType(Session("TransMsg"), String)
                Session.Remove("TransMsg")
                If Not IsNothing(Session("statusmsg")) Then
                    authNoteLbl.Text = authNoteLbl.Text & "; " & CType(Session("statusmsg"), String)
                    Session.Remove("statusmsg")
                    If Not IsNothing(Session("statusmsg2")) Then
                        authNoteLbl.Text = authNoteLbl.Text & "; " & CType(Session("statusmsg2"), String)
                        Session.Remove("statusmsg2")
                    End If
                End If
            End If
        End If
    End Sub
    
    Protected Sub verRegisterGv_Load(sender As Object, e As System.EventArgs) Handles verRegisterGv.Load
        Dim sql As StringBuilder = Nothing

        If Not _isNewOrder Then
            If varCarrierName.ToLower = "verizon" Then
                sql = New StringBuilder

                sql.Append("SELECT top 1 PlanStartDate [Renewed], PlanStartDate [Effective], ISNULL(NumofDays,0) [NumDays], ")
                sql.Append("MonthExpDate[Expires], p.planname AS [Plan], isnull(MinutesAvailable,0),[MinutesAvailable], ")
                sql.Append(" 0 [Used],1.0  AS Fee, 99 [Available],PlanExpDate [Completed] ")
                sql.Append(" FROM VerMDN m JOIN Plans p ON m.Planid = p.planid WHERE MID = @CellNum ORDER BY [Renewed] DESC; ")
                ' sql.Append("SELECT top 1 Effective [Renewed], [Effective], isnull(p.NumofDays,0) [NumDays],Expiration [Expires], p.planname AS [Plan], isnull(Minutes,0) [Minutes], ")
                'sql.Append(" 0 [Used],1.0  AS Fee,  [Available],Expiration [Completed] ")
                ' sql.Append("FROM VerRegister v JOIN Plans p ON v.plan_id  = p.planid WHERE MID = @CellNum ORDER BY [Renewed] DESC; ")

                verRegisterSqlDataSrc.SelectCommandType = SqlDataSourceCommandType.Text
                verRegisterSqlDataSrc.SelectCommand = sql.ToString
                verRegisterSqlDataSrc.CancelSelectOnNullParameter = False

                If IsNothing(verRegisterSqlDataSrc.SelectParameters("CellNum")) Then
                    verRegisterSqlDataSrc.SelectParameters.Add("CellNum", DbType.String, varOrderCellNumber)
                End If

                'verRegisterGv.DataBind()
                ' verRegisterTab.Visible = True
                'Else
                '     verRegisterTab.Visible = False
            End If
        End If
    End Sub



    ' Get data from database and set variables
    Private Sub GetOrderData()

        Dim cmd As New SqlCommand
        Dim reader As SqlDataReader
        cmd.Connection = 
            New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT carrier_name FROM orders WHERE order_id = @OrderId; "

        cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID

        cmd.Connection.Open

        reader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read
            varCarrierName = ReplaceDBNull(reader.Item("carrier_name"), "")
        End If

        cmd.Connection.Close

        If Not _isNewOrder Then
            If varCarrierName.ToLower = "page plus" Then
                GetPPData()
            ElseIf varCarrierName.ToLower = "verizon" Then
                GetVerData()
                'Dim strMsg As String = "Test"
                'Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "alert", "alert('" + strMsg + "')", True)
            ElseIf varCarrierName.ToLower = "all talk" Then
                GetATData()
            End If
        End If


    End Sub

    Private Sub GetVerData()
        Dim sql As StringBuilder = New StringBuilder
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        con.Open()
        Dim cmd As SqlCommand

        sql.Append("SELECT ")
        ' Order table
        sql.Append("[order_id] ,[cell_num], [customer_id], [create_date], [plan_id] ,[esn], [serial_num] ,")
        sql.Append("[monthly_plan_id], [monthly_auto_renew], [cash_plan_id], [cash_auto_renew], [intl_amt], [intl_auto_renew], ")
        sql.Append("[intl_pin] ,[carrier_pin] ,[vendor_pin], ")
        sql.Append("[monitor] , [renew_charge_type], [order_closed], [statusmsg], ")
        ' MDN table
        sql.Append("[Status], CashBalance [Balance], CAST( [MonthExpDate] AS DATE ) AS [PlanExpDate], ")
        sql.Append("CAST( PlanExpDate AS DATE ) AS [MonthExpDate], ")
        sql.Append(" [MinutesinPlan], ")
        sql.Append(" case when cashperminute > 0 then ISNULL ( CONVERT(int,([CashBalance]/[CashPerMinute])) , 0 ) else 0 end AS [min_remain], ")
        sql.Append("MinutesUpdated [LastStatus], [Stacked], ")
        sql.Append("MinutesAvailable [TotalMinutes], ")
        ' VerRegister
        'sql.Append("CAST( PlanExpDate AS DATE ) AS [PlanExpDate], ")
        'sql.Append("reg.PlanName, ")
        ' Plans table
        sql.Append("[planname], ")
        ' IntlBalances table (in the ATCIntl database)
        '--------
        ' IntlBalances table (in the ATCIntl database)
        sql.Append("[BalAvailable] AS intl_bal ")
        sql.Append("FROM [orders] ")
        sql.Append("LEFT JOIN [ppc].[dbo].[VERMDN] ON [VERMDN].[MID] = [orders].[cell_num] ")
        sql.Append("LEFT JOIN Plans ON [orders].[plan_id] = [Plans].[planid] ")
        sql.Append("LEFT JOIN [ATCIntl].[dbo].[IntlBalances] ON orders.[intl_pin] = [IntlBalances].[IntlPin] ")
        'sql.Append("LEFT JOIN ( ")
        'sql.Append("SELECT TOP (1) * FROM VerRegister WHERE Effective IS NOT NULL AND Completed IS NULL ")
        'sql.Append("ORDER BY Effective DESC ) AS reg ")
        'sql.Append("ON orders.cell_num = reg.MID ")
        sql.Append("WHERE [order_id] = @order_id; ")

        cmd = New SqlCommand(sql.ToString(), con)

        cmd.Parameters.Add("@order_id", SqlDbType.Int).Value = varOrderID

        'Response.Write(sql.ToString())
        'Response.End()
        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()

            varCustomerId = reader.Item("customer_id")

            customer = New Customer(varCustomerId)

            varOrderCellNumber = ReplaceDBNull(reader.Item("cell_num"), "")
            varStatus = StrConv(ReplaceDBNull(reader.Item("Status"), ""), VbStrConv.ProperCase)
            varSignupDate = ReplaceDBNull(reader.Item("create_date"), Nothing)
            varBalance = ReplaceDBNull(reader.Item("Balance"), 0)
            varExpirationDate = ReplaceDBNull(reader.Item("MonthExpDate"), Nothing)
            varPlanExpirationDate = ReplaceDBNull(reader.Item("PlanExpDate"), Nothing)

            Try
                Dim lastUpdate As Date = ReplaceDBNull(reader.Item("LastStatus"), "")
                varLastUpdate = lastUpdate.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))
            Catch ex As InvalidCastException
                varLastUpdate = ""
            End Try

            varEsn = ReplaceDBNull(reader.Item("esn"), "")
            varIntlPin = ReplaceDBNull(reader.Item("intl_pin"), "")
            varSerialNumber = ReplaceDBNull(reader.Item("serial_num"), "")
            varVendorPin = ReplaceDBNull(reader.Item("vendor_pin"), "")
            varMonitor = ReplaceDBNull(reader.Item("monitor"), Session("UserMonitor"))
            varRenewalChargeType = ReplaceDBNull(reader.Item("renew_charge_type"), 0)
            varIsOrderClosed = ReplaceDBNull(reader.Item("order_closed"), False)
            varPlanID = ReplaceDBNull(reader.Item("plan_id"), 0)
            varPlanName = ReplaceDBNull(reader.Item("planname"), "")
            varVendorName = ReplaceDBNull(reader.Item("planname"), "")
            varRenewalMonthlyId = ReplaceDBNull(reader.Item("monthly_plan_id"), 0)
            varIsMonthlyRenew = ReplaceDBNull(reader.Item("monthly_auto_renew"), False)
            varRenewalCashId = ReplaceDBNull(reader.Item("cash_plan_id"), 0)
            varIsCashRenew = ReplaceDBNull(reader.Item("cash_auto_renew"), False)
            varRenewalIntl = FormatNumber(ReplaceDBNull(reader.Item("intl_amt"), 0), 2)
            varIsIntlRenew = ReplaceDBNull(reader.Item("intl_auto_renew"), False)
            varStackedPinCount = ReplaceDBNull(reader.Item("Stacked"), 0)
            varIntlBalance = ReplaceDBNull(reader.Item("intl_bal"), 0)
            varVerStatusMsg = ReplaceDBNull(reader.Item("statusmsg"), "")
            varTotalMinutes = ReplaceDBNull(reader.Item("TotalMinutes"), Nothing)
            If (reader("MinutesInPlan") Is DBNull.Value) Then
                ' Response.Write("minutes are null")
                varTotalMinutes = ReplaceDBNull(reader.Item("min_remain"), Nothing)


            End If

        End If

        reader.Close()

        '--------

        sql.Clear()
        'sql.Append("DECLARE @Start DATETIME;")
        'sql.Append("SELECT TOP 1 @Start = Effective FROM VerRegister WHERE MID = @CellNum ")
        'sql.Append("AND Effective IS NOT NULL ORDER BY Effective DESC; ")
        'sql.Append("IF @Start IS NULL BEGIN SELECT ( ")
        'sql.Append("SELECT SUM( CALLDURMIN ) FROM Verizon.dbo.CDR WHERE MID = @CellNum ")
        'sql.Append(") AS TotalMinutes END ELSE BEGIN SELECT ( ")
        'sql.Append("SELECT SUM( CALLDURMIN ) AS TotalMinutes FROM Verizon.dbo.CDR WHERE MID = @CellNum ")
        'sql.Append("AND CALLTIME BETWEEN @Start AND GETDATE() ) AS TotalMinutes END ")

        'cmd = New SqlCommand
        'cmd.Connection = 
        '   New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        'cmd.CommandType = CommandType.Text
        ' cmd.CommandText = sql.ToString
        ' cmd.Parameters.Add("@CellNum", SqlDbType.VarChar).Value = varOrderCellNumber

        'cmd.Connection.Open
        'reader = cmd.ExecuteReader
        'If reader.HasRows Then
        '   reader.Read()
        ''varIncomingMinutes
        ''varOutgoingMinutes
        'varTotalMinutes = ReplaceDBNull(reader.Item("TotalMinutes"), Nothing)
        'End If

        'reader.Close
        'cmd.Connection.Close

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

    Private Sub GetPPData()
        Dim sql As StringBuilder = New StringBuilder
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        con.Open()
        Dim cmd As SqlCommand

        sql.Append("SELECT ")
        ' Order table
        sql.Append("[order_id] ,[cell_num], [customer_id], [create_date], [plan_id] ,[esn], [serial_num] ,")
        sql.Append("[monthly_plan_id], [monthly_auto_renew], [cash_plan_id], [cash_auto_renew], [intl_amt], [intl_auto_renew], ")
        sql.Append("[intl_pin] ,[carrier_pin] ,[vendor_pin], ")
        sql.Append("[monitor] , [renew_charge_type], [order_closed], [statusmsg], ")
        'CONVERT(VARCHAR(10),  [ESNChecked], 101) as ESNChecked,  ")
        sql.Append(" [ESNChecked] as ESNChecked, ")
        ' MDN table
        sql.Append("[Status], [RatePlan], [Balance], [ExpDate], [PlanExpDate], [lastModified], [PINCount], ")
        ' Plans table
        sql.Append("[planname], [kosher], ")
        ' IntlBalances table (in the ATCIntl database)
        sql.Append("[BalAvailable] AS intl_bal, [PlanBalanceDetailsMin] ")
        sql.Append("FROM [orders] ")
        sql.Append("LEFT JOIN [ppc].[dbo].[MDN] ON [MDN].[PhoneNumber] = [orders].[cell_num] ")
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
       
            Try
                Dim lastUpdate As Date = ReplaceDBNull(reader.Item("lastModified"), "")
                varLastUpdate = lastUpdate.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))
            Catch ex As InvalidCastException
                varLastUpdate = ""
            End Try


            varEsn = ReplaceDBNull(reader.Item("esn"), "")

            varIntlPin = ReplaceDBNull(reader.Item("intl_pin"), "")

            varSerialNumber = ReplaceDBNull(reader.Item("serial_num"), "")

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

            varVerStatusMsg = ReplaceDBNull(reader.Item("statusmsg"), "")

            varTotalMinutes = ReplaceDBNull(reader.Item("PlanBalanceDetailsMin"), "")

            varLastValidated = ReplaceDBNull(reader.Item("ESNChecked"), "")

        End If
        cmd = New SqlCommand("IsValidESN", con)
        cmd.CommandType = Data.CommandType.StoredProcedure

        cmd.Parameters.Add("@cell_num", SqlDbType.VarChar).Value = varOrderCellNumber


        reader.Close()
        reader = cmd.ExecuteReader

        If reader.HasRows Then
            varValidESN = False

        Else
            varValidESN = True
        End If

        'cmd = New SqlCommand("noCallSummary", con)
        'cmd.CommandType = Data.CommandType.StoredProcedure

        ' cmd.Parameters.Add("@pn", SqlDbType.VarChar).Value = varOrderCellNumber

        ' reader.Close()
        ' reader = cmd.ExecuteReader

        ' If reader.HasRows Then
        ''reader.Read()
        ' varIncomingMinutes = ReplaceDBNull(reader.Item("Incoming minutes"), 0)
        ' varOutgoingMinutes = ReplaceDBNull(reader.Item("Outgoing minutes"), 0)
        ' varTotalMinutes = ReplaceDBNull(reader.Item("Total Minutes"), 0)
        'End If

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

    Private Sub GetATData()
        Dim sql As StringBuilder = New StringBuilder
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        con.Open()
        Dim cmd As SqlCommand

        sql.Append("SELECT ")
        ' Order table
        sql.Append("[order_id] ,[cell_num], [customer_id], [create_date], [plan_id] ,[esn], [serial_num] ,")
        sql.Append("[monthly_plan_id], [monthly_auto_renew], [cash_plan_id], [cash_auto_renew], [intl_amt], [intl_auto_renew], ")
        sql.Append("[intl_pin] ,[carrier_pin] ,[vendor_pin], ")
        sql.Append("[monitor] , [renew_charge_type], [order_closed], [statusmsg], ")
        'CONVERT(VARCHAR(10),  [ESNChecked], 101) as ESNChecked,  ")
        'sql.Append(" [ESNChecked] as ESNChecked, ")
        ' MDN table
        sql.Append("[Status], [Verify], [VerifyDate], [RatePlan], [Balance], convert(date, [ExpDate]) ExpDate, [PlanExpDate], [lastModified], [PINCount], ")
        ' Plans table
        sql.Append("[planname], [kosher], ")
        ' IntlBalances table (in the ATCIntl database)
        sql.Append("[BalAvailable] AS intl_bal, [PlanBalanceDetailsMin] ")
        sql.Append("FROM [orders] ")
        sql.Append("LEFT JOIN [ppc].[dbo].[ATC_MDN] ON [ATC_MDN].[PhoneNumber] = [orders].[cell_num] ")
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


            Try
                Dim lastUpdate As Date = ReplaceDBNull(reader.Item("lastModified"), "")
                varLastUpdate = lastUpdate.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))
            Catch ex As InvalidCastException
                varLastUpdate = ""
            End Try


            varEsn = ReplaceDBNull(reader.Item("esn"), "")

            varIntlPin = ReplaceDBNull(reader.Item("intl_pin"), "")

            varSerialNumber = ReplaceDBNull(reader.Item("serial_num"), "")

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

            varVerStatusMsg = ReplaceDBNull(reader.Item("statusmsg"), "")

            varTotalMinutes = ReplaceDBNull(reader.Item("PlanBalanceDetailsMin"), "")

            varLastValidated = ReplaceDBNull(reader.Item("VerifyDate"), "")
            varATCVerify = ReplaceDBNull(reader.Item("Verify"), "")

        End If

        'Dim statusmsg As String = ""
        'varValidESN = ATCPin.ATCEsn(varOrderCellNumber, statusmsg)
        'If statusmsg <> "" Then
        '    orderErrorMessage.Style("color") = "red"
        '    orderErrorMessage.InnerText = statusmsg
        'End If
        'cmd = New SqlCommand("IsValidESN", con)
        'cmd.CommandType = Data.CommandType.StoredProcedure
        'cmd.Parameters.Add("@cell_num", SqlDbType.VarChar).Value = varOrderCellNumber
        'reader.Close()
        'reader = cmd.ExecuteReader
        'If reader.HasRows Then
        '    varValidESN = False
        'Else
        '    varValidESN = True
        'End If


        'cmd = New SqlCommand("noCallSummary", con)
        'cmd.CommandType = Data.CommandType.StoredProcedure

        ' cmd.Parameters.Add("@pn", SqlDbType.VarChar).Value = varOrderCellNumber

        ' reader.Close()
        ' reader = cmd.ExecuteReader

        ' If reader.HasRows Then
        ''reader.Read()
        ' varIncomingMinutes = ReplaceDBNull(reader.Item("Incoming minutes"), 0)
        ' varOutgoingMinutes = ReplaceDBNull(reader.Item("Outgoing minutes"), 0)
        ' varTotalMinutes = ReplaceDBNull(reader.Item("Total Minutes"), 0)
        'End If

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
        showCalendar1.Visible = False
        showCalendar2.Visible = False

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
            _callBackResult &= "<creditUsed>" & (GetAgentCreditTotal(userName) - GetAgentCommissions(userName)) & "</creditUsed>"
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

        Dim sql As String = "SELECT sum([total]) AS total FROM [authtrans] WHERE [agent] = @Agent AND charged = 1 AND trans_type IN ( 2, 3 ); "

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

    Private Function GetAgentCommissions(Optional ByVal userName As String = "") As Decimal
        Dim tot As Decimal = Nothing
        Dim cmd As New SqlCommand
        cmd.Connection =
             New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT SUM(CommissionAmount) AS total FROM Commissions WHERE Agent = @Agent; "
        If userName = "" Then
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text
        Else
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = userName
        End If

        cmd.Connection.Open()

        Using reader As SqlDataReader = cmd.ExecuteReader
            If reader.Read Then
                tot = Decimal.Parse(If(IsDBNull(reader.Item("total")), 0, reader.Item("total")))
            End If
        End Using

        cmd.Connection.Close()
        Return tot
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

        balanceLbl.Text = FormatCurrency(varBalance, 2)
        inltBallanceLbl.Text = FormatCurrency(varIntlBalance, 2)

        expirationDateLbl.Text = varExpirationDate
        planExpirationDateLbl.Text = varPlanExpirationDate
        pinStackedStatusLbl.Text = varStackedPinCount

        signupDateBarLbl.Text = varSignupDate.ToString("d", CultureInfo.CreateSpecificCulture("en-US"))

        If varIsMonthlyRenew Then 'FK added this check on 5/1/14 because with all talk, the monthly plan id gets switched on every payment, even if there is no renewal.  So this check makes sure that the price for that monthly_plan_id is shown in the infobar by renewals only if there really is a monthly-renewal set up
            renewalMonthLbl.Text = FormatCurrency(GetPlanCost(varRenewalMonthlyId), 2)
        Else
            renewalMonthLbl.Text = FormatCurrency(0, 2)
        End If
        renewalCashLbl.Text = FormatCurrency(GetPlanCost(varRenewalCashId), 2)
        renewIntlLbl.Text = FormatCurrency(varRenewalIntl, 2)

        vendorNameLbl.Text = varVendorName
        'incomingMinutesLbl.Text = varIncomingMinutes
        'outgoingMinutesLbl.Text = varOutgoingMinutes
        totalMinutesLbl.Text = varTotalMinutes
        If varCarrierName.ToLower = "page plus" Then 'Or varCarrierName.ToLower = "all talk" 
            esnValidLbl.Visible = True
            esnLbl.Visible = True
            esnLbl.Visible = True
            esnLkBn.Visible = True

            If varValidESN = False Then
                esnLbl.Text = "Not Validated" 'If Not varCarrierName.ToLower = "all talk" Then 
                esnLbl.ForeColor = Drawing.Color.Red
                LastValidateLbl.Visible = True
                LastValidateDateLbl.Visible = True
                LastValidateDateLbl.Text = varLastValidated

            Else
                esnLbl.Text = "Valid"
                LastValidateLbl.Visible = True
                LastValidateDateLbl.Visible = True
                LastValidateDateLbl.Text = varLastValidated
            End If

        End If

        If varCarrierName.ToLower = "all talk" Then
            '    vendorNameLbl.Visible = False
            '    lblVendorName.Visible = False

            esnValidLbl.Visible = True
            esnValidLbl.Text = "Verify"
            esnLbl.Visible = True
            esnLbl.Text = varATCVerify
            esnLkBn.Visible = True
            LastValidateLbl.Visible = True
            LastValidateDateLbl.Visible = True
            LastValidateDateLbl.Text = varLastValidated

            If varATCVerify = "Not Validated" Then
                esnLbl.ForeColor = Drawing.Color.Red
            End If
        End If
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

            orderErrorMessage.Style("color") = "black"
            orderErrorMessage.InnerText = varVerStatusMsg

        End If

        SetCellNum( varOrderCellNumber )

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
        Dim creditUsed = GetAgentCreditTotal() - GetAgentCommissions
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
        If Not _isNewOrder Then
            If varCarrierName.ToLower = "verizon" Then

                GetVerizonCdr()
            ElseIf varCarrierName.ToLower = "page plus" Then
                ' GetPPCdr()
            End If
        End If

    End Sub

    Private Sub GetPPCdr()

        ' Get values that are used by the stored procedure to determine which cycle to display.
        Dim fromIndex As Integer = Convert.ToInt32(callDetailFromIndex.Value)
        Dim endIndex As Integer = Convert.ToInt32(callDetailEndIndex.Value)


        'Dim startdate As Integer = 0
        'Dim enddate As Integer = 0
        'Dim comm As SqlCommand = New SqlCommand
        'Dim strsql = ("SELECT ROW_NUMBER() OVER ( ORDER BY CallDate DESC ) AS RowNumber,  CallDate ")
        'strsql = strsql + ("FROM [PPCData].[dbo].[CDR] c JOIN [MDN] mdn ")
        'strsql = strsql + ("ON c.PhoneAccountId = MDN.phoneAccountId AND  c.AccountName = MDN.Account")
        'strsql = strsql + ("WHERE MDN.PhoneNumber = @PhoneNumber'  AND")
        'strsql = strsql + ("c.[Description] = 'Credit/Replenishment' AND  CallDate=")
        'strsql = strsql + ("(SELECT CONVERT(DATETIME," + "'" + startDate + "'" + ", 121))")
        'strsql = strsql + ("GROUP BY CallDate")
        'comm.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        'comm.Parameters.Add("@phone_number", SqlDbType.VarChar).Value = varOrderCellNumber
        'comm.CommandText = strsql
        'comm.Connection.Open()
        'Dim reader As SqlDataReader = comm.ExecuteReader
        'If reader.HasRows Then
        '    reader.Read()
        '    startdate = ReplaceDBNull(reader.Item("RowNumber"), 0)
        'End If
        'reader.Close()
        'comm.Connection.Close()

        ''Response.Write(startdate)
        'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & startdate & "');</script>")
        ''For end date

        'Dim strsql2 = ("SELECT ROW_NUMBER() OVER ( ORDER BY CallDate DESC ) AS RowNumber,  CallDate ")
        'strsql2 = strsql2 + ("FROM [PPCData].[dbo].[CDR] c JOIN [MDN] mdn ")
        'strsql2 = strsql2 + ("ON c.PhoneAccountId = MDN.phoneAccountId AND  c.AccountName = MDN.Account")
        'strsql2 = strsql2 + ("WHERE MDN.PhoneNumber = @PhoneNumber'  AND")
        'strsql2 = strsql2 + ("c.[Description] = 'Credit/Replenishment' AND  CallDate=")
        'strsql2 = strsql2 + ("(SELECT CONVERT(DATETIME," + "'" + endDate + "'" + ", 121))")
        'strsql2 = strsql2 + ("GROUP BY CallDate")
        'comm.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        'comm.Parameters.Add("@phone_number", SqlDbType.VarChar).Value = varOrderCellNumber
        'comm.CommandText = strsql2
        'comm.Connection.Open()
        'reader = comm.ExecuteReader
        'If reader.HasRows Then
        '    reader.Read()
        '    enddate = ReplaceDBNull(reader.Item("RowNumber"), 0)
        'End If
        'reader.Close()
        'comm.Connection.Close()

        '' Response.Write(enddate)
        'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & enddate & "');</script>")




        ' This is the number of cycles there are for this order.
        Dim cdIntervals As Integer = Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value)

        Dim cmd As SqlCommand = New SqlCommand
        Dim dataAdapter As SqlDataAdapter
        Dim dt As New DataTable

        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add("@phone_number", SqlDbType.VarChar).Value = varOrderCellNumber
        cmd.Parameters.Add("@from_index", SqlDbType.Int).Value = fromIndex
        cmd.Parameters.Add("@end_index", SqlDbType.Int).Value = endIndex
        'cmd.Parameters.Add("@from_index", SqlDbType.Int).Value = startdate
        'cmd.Parameters.Add("@end_index", SqlDbType.Int).Value = enddate


        cmd.CommandText = "CallDetailGridviewData"

        cmd.Connection.Open()

        dataAdapter = New SqlDataAdapter(cmd)

        dataAdapter.Fill(dt)

        cmd.Connection.Close()

        orderCallDetails.DataSource = dt
        orderCallDetails.DataBind()

        ' Clear parameters to reuse cmd for a new query
        cmd.Parameters.Clear()

        cmd.Connection.Open()
        cmd.CommandType = Data.CommandType.StoredProcedure
        cmd.CommandText = "CallSummary"

        cmd.Parameters.Add("@pn", SqlDbType.VarChar).Value = varOrderCellNumber
        cmd.Parameters.Add("@from_index", SqlDbType.Int).Value = fromIndex
        cmd.Parameters.Add("@end_index", SqlDbType.Int).Value = endIndex
        'cmd.Parameters.Add("@from_index", SqlDbType.Int).Value = startdate
        'cmd.Parameters.Add("@end_index", SqlDbType.Int).Value = enddate

        Dim reader As SqlDataReader = cmd.ExecuteReader


        If reader.HasRows Then
            reader.Read()

            inMinutesForTab.Text = ReplaceDBNull(reader.Item("Incoming minutes"), 0)
            outMinutesForTab.Text = ReplaceDBNull(reader.Item("Outgoing minutes"), 0)
            totalMinutesForTab.Text = ReplaceDBNull(reader.Item("Total Minutes"), 0)

            callDetailFromLbl.Text = ReplaceDBNull(reader.Item("from_date"), varSignupDate)
            'If callDetailFromLbl.Text = "1/1/1900" Then
            '    callDetailFromLbl.Text = varSignupDate
            '    ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('helo');</script>")
            'End If

            callDetailToLbl.Text = ReplaceDBNull(reader.Item("end_date"), Now)
        End If

        reader.Close()

        cmd.Connection.Close()

        ' End index is zero if there are no future cycles so if it is zero disable 
        ' the next cycle button.
        If endIndex > 0 Then
            nextCallDetail.Enabled = True
        Else
            'nextCallDetail.Enabled = False
        End If

        ' Call detail intervals (cdIntervals) are the number of cycles for this phone number
        ' so disable previous cycle button if there are no previous cycles. 
        If fromIndex >= cdIntervals Then
            prevCallDetails.Enabled = False
        Else
            prevCallDetails.Enabled = True
        End If

    End Sub
   


    Public Sub GetVerizonCdr()
       


        Dim cmd As New SqlCommand
        Dim sql As StringBuilder = New StringBuilder
        Dim adapter As SqlDataAdapter
        Dim ds As New DataSet


        'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & useCalendar.Value & "');</script>")

        startDate = txtFrom.Value
        'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & txtTo.Text & "');</script>")
        endDate = txtTo.Value
        

        ' Set variables

        sql.Append("DECLARE @Start DATETIME, @End DATETIME; ")

        ' Get the dates
        ''sql.Append("SELECT IDENTITY( INT, 1, 1 ) AS [Offset], Effective INTO #dates FROM ppc.dbo.VerRegister ")
        ''sql.Append("WHERE MID = @CellNum AND Effective IS NOT NULL ORDER BY Effective DESC; ")

        ''sql.Append("SET @Start = COALESCE( ( SELECT Effective FROM #dates WHERE Offset = @sIndex ), ")
        ''sql.Append("( SELECT MIN(CALLTIME) FROM CDR WHERE MID = @CellNum ) ); ")

        ''sql.Append("SET @End = ( COALESCE( ( SELECT Effective FROM #dates WHERE Offset = @eIndex ), GETDATE() ) ); ")
        ''sql.Append("DROP TABLE #dates; ")



        'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & useCalendar.Value & "');</script>")
        
        If useCalendar.Value = "true" Then

            'prevCallDetails.Text = "true"
            sql.Append("SET @Start =  (SELECT CONVERT(DATETIME," + "'" + startDate + "'" + ", 121))   ")
            'sql.Append("SET @End =  (SELECT CONVERT(DATETIME," + "'" + endDate + "'" + ", 121))   ")
            'sql.Append("SET @End = (SELECT CONVERT (datetime, ( CONVERT(DATETIME, " + "'" + startDate + "'" + ", 121) + convert(datetime, '23:59:59', 8)), 121)) ")
            sql.Append("SET @End = (SELECT CONVERT (datetime, ( CONVERT(DATETIME, " + "'" + endDate + "'" + ", 121) + convert(datetime, '23:59:59', 8)), 121)) ")


            'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & txtFrom.Value & "');</script>")
            'If txtFrom.Value = "" Then
            '    sql.Append("SET @Start = dateadd(mm,@sIndex,(select create_date from ppc.dbo.orders where cell_num = @cellnum))") 
            '    sql.Append("SET @End = dateadd(mm,@eIndex,(select create_date from ppc.dbo.orders where cell_num = @cellnum))") 
        End If

        If useCalendar.Value = "false" Or useCalendar.Value = "" Then
            ''prevCallDetails.Text = "false"
            'sql.Append("SET @Start = dateadd(mm,@sIndex,(select create_date from ppc.dbo.orders where cell_num = @cellnum))") 
            'sql.Append("SET @End = dateadd(mm,@eIndex,(select create_date from ppc.dbo.orders where cell_num = @cellnum))") 
            ''sql.Append("SET @Start = dateadd(mm,@sIndex,  (SELECT CONVERT(DATETIME," + "'" + startDate + "'" + ", 121)))") 
            ''sql.Append("SET @End = dateadd(mm,@eIndex, (SELECT CONVERT(DATETIME," + "'" + endDate + "'" + ", 121)))") 

            'sql.Append(" SELECT 1 as [offset], CREATE_DATE  AS [mydate] into #dt1 FROM ORDERS WHERE cell_num = @CellNum")
            'sql.Append(" SELECT IDENTITY( INT, 2, 1 ) AS [Offset], ISNULL(month_processed,cash_processed)   AS [mydate] into #dt2")
            'sql.Append(" FROM ppc.dbo.authtrans")
            ' sql.Append(" WHERE cell_num = @cellnum AND (month_processed is not null or cash_processed is not null)")
            'sql.Append(" order by ISNULL(month_processed,cash_processed)")
            'sql.Append(" Insert into  #dt2 (mydate) VALUES (GETDATE());  ")
            'sql.Append(" select * into #dates from #dt1 union select * from #dt2 order by offset;")


            'OK PREV
            sql.Append("SELECT IDENTITY( INT, 1, 1 ) AS [Offset], ISNULL(month_processed,cash_processed)  As [mydate] INTO #dates FROM ppc.dbo.authtrans ")
            sql.Append("WHERE cell_num = @CellNum AND (month_processed is not null or cash_processed is not null) order by ISNULL(month_processed,cash_processed) ; ")
            sql.Append("Insert into   #dates (mydate) VALUES (GETDATE());  ")

            sql.Append("SET @Start = ( SELECT mydate FROM #dates WHERE Offset =  @sIndex ); ")


            sql.Append("SET @End = ( SELECT mydate FROM #dates WHERE Offset =  @eIndex ); ")
            'sql.Append("SET @Start = dateadd(mm,@sIndex,(SELECT mydate FROM #dates WHERE Offset =  @sIndex ))")
            'sql.Append("SET @End = dateadd(mm,@eIndex,(SELECT mydate FROM #dates WHERE Offset =  @sIndex))")
            sql.Append(" DROP TABLE #dates; ")
            'sql.Append(" drop table #dt1 drop table #dt2 ")


        End If




        ''Full Version

        ''sql.Append("SELECT [RECORD ID], [NET ELEM NO], [CALL DATA RCD DT], [SWCH TYPE IND], [AUTO STRCTR CD] ")
        ''sql.Append(",[SWCH NO], [MID], [MIN], [DLD DGT NO], [OPLSD DGT NO], [EQPMT SRL NO/MEID] ")
        ''sql.Append(",cdi.[LookupVal] AS [CALL DIRN IND], rsi.[LookupVal] AS [ROMR STAT IND] ")
        ''sql.Append(",[SZR DT TM], [SZR DURTN CNT], [ANSW DT TM] , [ANSW DURTN CNT], [INIT CELL NO] ")
        ''sql.Append(",[FINL CELL NO], [TRK MBR ID], ci.[LookupVal] AS [CCI IND], [MOTO CXR ID] ")
        ''sql.Append(",[MOTO CALL TYPE CD], [MOTO MIDN ROLL IND], actc.[LookupVal] AS [AUTO CALL TYPE CD] ")
        ''sql.Append(",[AUTO CPN NO], [AUTO OVS IND], [AUTO SVC FEAT CD], [AUTO CXR PRFX CD], [ANSW STAT IND] ")
        ''sql.Append(",sfc.[LookupVal] AS [SVC FEAT CD], [NORT SVC FEAT CD], [SIDBID NO] ")
        ''sql.Append(",tc.[LookupVal] AS [TERM CD], [CALL DLVRY IND], [FEAT SETUP IND], [THREE WAY CALL IND] ")
        ''sql.Append(",[CALL WAIT IND], [BUSY XFER IND], [NO ANSW XFER IND], [CALL FWD IND], [NORT CXR ID] ")
        ''sql.Append(",[WORLD NO], [NRTRFileId], [CALLDURMIN], [CALLTIME] ")


        'Abridged version

        'sql.Append("SELECT [MID], [MIN], [DLD DGT NO], [OPLSD DGT NO] ")
        'sql.Append(", cdi.[LookupVal] AS [CALL DIRN IND], rsi.[LookupVal] AS [ROMR STAT IND] ")
        'sql.Append(", [CALLDURMIN], [CALLTIME] ")


        'sql.Append("FROM [CDR] vcdr ")
        'sql.Append("LEFT OUTER JOIN [LT_CALL_DIRN_IND] cdi ON cdi.[LookupNum] = vcdr.[CALL DIRN IND] ")
        'sql.Append("LEFT OUTER JOIN [LT_ROMR_STAT_IND] rsi ON rsi.[LookupNum] = vcdr.[CALL DIRN IND] ")
        'sql.Append("LEFT OUTER JOIN [LT_CCI_IND] ci ON ci.[LookupNum] = vcdr.[CCI IND] ")
        'sql.Append("LEFT OUTER JOIN [LT_AUTO_CALL_TYPE_CD] actc ON actc.[LookupNum] = vcdr.[AUTO CALL TYPE CD] ")
        'sql.Append("LEFT OUTER JOIN [LT_SVC_FEAT_CD] sfc ON sfc.[LookupNum] = vcdr.[SVC FEAT CD] ")
        'sql.Append("LEFT OUTER JOIN [LT_TERM_CD] tc ON tc.[LookupNum] = vcdr.[TERM CD] ")
        'sql.Append("WHERE [MID] = @CellNum  and vcdr.[CALL DIRN IND] != '5'")
        'sql.Append("AND [CALLTIME] BETWEEN @Start AND @End ")
        'sql.Append("ORDER BY [SEQID] DESC; ")

        sql.Append("SELECT [RECORD ID][REC ID], [BILLING NUMBER][MID], [CALLTIME] ,[FROM CITY]  + ' ' + [FROM STATE] [ORIGIN], [OTHER PARTY NUMBER][CALLING NUM]")
        sql.Append(", [TO CITY]  + ' ' + [TO STATE] [DEST],[TOTAL SECONDS OF CALL] [DUR],[CALLDURMIN][MIN] ")
        sql.Append("FROM [Verizon].[dbo].[CDR_NEW] vcdr ")
        sql.Append("WHERE [BILLING NUMBER] = @CellNum ")
        sql.Append("AND [CALLTIME] BETWEEN @Start AND @End ")
        sql.Append("ORDER BY [SEQID] DESC ")

        ' Get cycle dates and total minutes
        sql.Append("SELECT @Start AS Start, @End AS [End], SUM( CALLDURMIN ) AS TotalMinutes FROM CDR_NEW ")
        ' sql.Append("SELECT @Start AS Start, @End AS [End],(SELECT CONVERT(DATETIME, @Start, 121)) AS Start2, (SELECT CONVERT(DATETIME, @End, 121)) AS [End2], SUM( CALLDURMIN ) AS TotalMinutes FROM CDR ")
        sql.Append("WHERE [BILLING NUMBER] = @CellNum  AND [CALLTIME] BETWEEN @Start AND @End; ")

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("verizonConnectionString").ConnectionString)

        cmd.Parameters.Add("@CellNum", SqlDbType.VarChar).Value = varOrderCellNumber
        'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & varCarrierName & "');</script>")
        If varCarrierName.ToLower = "verizon" Then
            cmd.Parameters.Add("@sIndex", SqlDbType.Int).Value = (Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value - callDetailFromIndex.Value))
            cmd.Parameters.Add("@eIndex", SqlDbType.Int).Value = (Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value - callDetailEndIndex.Value))
            'cmd.Parameters.Add("@sIndex", SqlDbType.Int).Value = (Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value - callDetailFromIndex.Value)) - 1
            'cmd.Parameters.Add("@eIndex", SqlDbType.Int).Value = (Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value - callDetailEndIndex.Value)) - 1


        End If



        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.Connection.Open()

        adapter = New SqlDataAdapter(cmd)

        adapter.Fill(ds)

        cmd.Connection.Close()

        If ds.Tables.Count > 0 Then
            ds.Tables(0).TableName = "CDR_NEW"
            ds.Tables(1).TableName = "CycleInfo"

            orderCallDetails.DataSource = ds.Tables("CDR_NEW")
            orderCallDetails.DataBind()

            callDetailFromLbl.Text = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("Start"), varSignupDate)

            callDetailToLbl.Text = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("End"), "")

            totalMinutesForTab.Text = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("TotalMinutes"), 0)
            If useCalendar.Value = "false" Then
                ' ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & useCalendar.Value & "');</script>")
                'txtFrom.Text = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("Start2"), varSignupDate)
                txtFrom.Value = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("Start"), varSignupDate)
                'txtTo.Text = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("End2"), "")
                txtTo.Value = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("End"), "")
            End If
            If useCalendar.Value = "" Then
                txtFrom.Value = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("Start"), varSignupDate)
                txtTo.Value = ReplaceDBNull(ds.Tables("CycleInfo").Rows(0).Item("End"), "")
                useCalendar.Value = "true"
            End If
        End If
        'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & varCarrierName & "');</script>")
        If varCarrierName.ToLower = "verizon" Then
            ' Get values that are used by the stored procedure to determine which cycle to display.
            Dim fromIndex As Integer = Convert.ToInt32(callDetailFromIndex.Value)
            Dim endIndex As Integer = Convert.ToInt32(callDetailEndIndex.Value)
            'This is the number of cycles there are for this order.

            Dim cdIntervals As Integer = Convert.ToInt32(hdnNumOfCallDetailsIntervals.Value)

            ' End index is zero if there are no future cycles so if it is zero disable 
            '' the next cycle button.
            If endIndex > 0 Then
                nextCallDetail.Enabled = True
            Else
                nextCallDetail.Enabled = False
            End If

            '' Call detail intervals (cdIntervals) are the number of cycles for this phone number
            '' so disable previous cycle button if there are no previous cycles. 
            If fromIndex >= cdIntervals Then
                prevCallDetails.Enabled = False
            Else
                prevCallDetails.Enabled = True
            End If
        End If

        'If callDetailFromLbl.Text = "1/1/1900" Then
        '    'callDetailFromLbl.Text = varSignupDate
        '    txtFrom.Value = varSignupDate
        '    ' txtTo.Value = varSignupDate
        '    'ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('hello');</script>")
        'End If
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

    Private Sub BindActivityGv()
        
        If IsNothing(activityDataSource.SelectParameters.Item("CellNum")) Then
            activityDataSource.SelectParameters.Add(New Parameter("CellNum", DbType.String))
        End If
        activityDataSource.SelectParameters.Item("CellNum").DefaultValue = varOrderCellNumber
    
        activityDataSource.SelectCommandType = SqlDataSourceCommandType.StoredProcedure
        activityDataSource.SelectCommand = "GetOrderActivity"
        activityDataSource.CancelSelectOnNullParameter = False

        activityGv.DataBind()
    
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



        'if carrier != all talk then do the following, otherwise, cash Or monthly renewal both get saved in monthly_plan_id

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



    Private Function GetVerInfo() As String
        Dim cmd As New SqlCommand
        Dim reader As SqlDataReader
        Dim data As String = ""
        Dim convertStatus As String = ""

        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT cell_num, [status], statusmsg, carrier_name FROM orders AS o LEFT JOIN VERMDN AS vMdn "
        cmd.CommandText &= "ON o.cell_num = vMdn.MID WHERE order_id = @OrderId; "
        cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID

        cmd.Connection.Open()

        Try

            reader = cmd.ExecuteReader()
            reader.Read()

            ' For orders converted from PP to Ver, check when the conversion is successful
            If carrierName.SelectedValue.ToLower = "page plus" _
                And reader.Item("carrier_name").ToString.ToLower = "verizon" Then
                convertStatus = "ChangedToVerizon"
            End If

            data &= "{" & DoubleQuoteString("cell") & ": " & DoubleQuoteString(reader.Item("cell_num").ToString)
            data &= ", " & DoubleQuoteString("msg") & ": " & DoubleQuoteString(reader.Item("statusmsg").ToString)
            data &= ", " & DoubleQuoteString("mdnStatus") & ": " & DoubleQuoteString(reader.Item("status").ToString)
            data &= ", " & DoubleQuoteString("convertStatus") & ": " & DoubleQuoteString(convertStatus) & "}"

        Catch ex As Exception

        End Try

        cmd.Connection.Close()

        Return data

    End Function



    ' Handle user events
    Protected Sub SaveOrderBtnClick(ByVal s As Object, ByVal e As CommandEventArgs)

        If e.CommandName = "Save" Then
            If SaveOrder() Then
                Response.Redirect("~/Order.aspx?oid=" & StringEncode(varOrderID))
            End If
        ElseIf e.CommandName = "Update" Then
            UpdateOrder()
            ClearInvoiceFields()
        End If

    End Sub

    Protected Sub submitVerReqBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles submitVerReqBtn.Click

        If varCarrierName.ToLower <> "verizon" Then
            Exit Sub
        End If

        Dim cmd As New SqlCommand
        Dim sql As New StringBuilder
        Dim esnChanged As Boolean = serialNumber.Text <> varSerialNumber
        Dim statusMsg As String = Nothing

        If esnChanged Then
            statusMsg = SubmitChangeVerEsn()
        Else
            statusMsg = SubmitVerReq()
        End If

        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "UPDATE orders SET statusmsg = @Msg WHERE order_id = @OrderId; "
        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID
        cmd.Parameters.Add("@Msg", SqlDbType.VarChar).Value = statusMsg

        cmd.Connection.Open()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

        ReloadOrder()

    End Sub
    

    Protected Sub executeAdminTasks_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles executeAdminTasks.Click
        Dim reload As Boolean = True
        Dim cmd As New SqlCommand
        Dim statusMsg As String = Nothing
        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandTimeout = 150

        If adminTasksRadiolist.SelectedValue.ToLower = "convertat" Then 'convert from page plus to all talk
            cmd.Connection.Open()
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
            cmd.CommandText = "ATC_ConvertAT"
            cmd.ExecuteNonQuery()

            Dim statusmsg2 As String = ""
            'ATClass.ATCPinAssign.ATCStatus(varOrderCellNumber, statusmsg2)
            ATCPin.ATCStatus(varOrderCellNumber, statusmsg2)
            If statusmsg2 <> "" Then
                orderErrorMessage.Style("color") = "red"
                orderErrorMessage.InnerText = statusmsg2
                'set error label equal to statusmsg2
            End If
        ElseIf adminTasksRadiolist.SelectedValue.ToLower = "disconnect" Then 'disconnect verizon
            cmd.Connection.Open()
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.Add("@MDN", SqlDbType.VarChar).Value = varOrderCellNumber
            If varCarrierName.ToLower = "verizon" Then
                cmd.CommandText = "VER_Disconnect"
            End If
            cmd.ExecuteNonQuery()
        ElseIf adminTasksRadiolist.SelectedValue.ToLower = "convertpp" Then 'convert from verizon / all talk to pp
            cmd.Connection.Open()
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
            If varCarrierName.ToLower <> "verizon" Then
                cmd.CommandText = "VER_ConvertPP"
            Else
                cmd.CommandText = "ATC_ConvertPP"
            End If
            cmd.ExecuteNonQuery()
            'cmd.Connection.Close()
        ElseIf adminTasksRadiolist.SelectedValue = "changeMDN" Then 'change Verizon MDN (not used)
            If varCarrierName.ToLower = "verizon" Then
                cmd.Connection.Open()
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
                cmd.Parameters.Add("@MDN", SqlDbType.VarChar).Value = varOrderCellNumber
                cmd.CommandText = "VER_ChangeMDN"
                cmd.ExecuteNonQuery()
            End If
       
        ElseIf adminTasksRadiolist.SelectedValue.ToLower = "updatestack" Then 'update stack
            cmd.Connection.Open()
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = varOrderCellNumber
            If varCarrierName.ToLower <> "verizon" Then
                cmd.CommandText = "ATC_ReleaseStack"
            Else
                cmd.CommandText = "Ver_ReleaseStack"
            End If
            cmd.ExecuteNonQuery()
        ElseIf adminTasksRadiolist.SelectedValue.ToLower = "removestack" Then 'subtract one from stack
            ATCPin.subtractFromStack(varOrderCellNumber, statusMsg)
        ElseIf adminTasksRadiolist.SelectedValue.ToLower = "atprovision" Then
            ATCPin.ConvertAT(varOrderCellNumber, varEsn, varOrderID, statusMsg) 'as per email from Mr. Greenberg on 4.3.2014
        Else
            Dim validReq As Boolean = True
            Dim task As String = adminTasksRadiolist.SelectedValue.ToLower

            'if admin task is to convert to verizon, must check that customer has all required info such as address, city, state, zip
            If task = "convertver" Then
                'if required fields are missing, set validreq = false
                'If customer.Address = "" Or customer.City = "" Or customer.State = "" Or customer.Zip = "" Then
                'If customer.Address = "" Or customer.City = "" Or customer.State = "" Or customer.Zip = "" Then
                'adminErrorMsg.InnerHtml = "Please update customer information to include first name, address, city, state, and zip."
                'validReq = False
                If customer.Address = "" Or customer.City = "" Or customer.State = "" Or customer.Zip = "" Or customer.FirstName = "" Then
                    adminErrorMsg.InnerHtml = "Please update customer information to include first name, address, city, state, and zip."
                    validReq = False
                ElseIf customer.State.Length > 2 Then
                    adminErrorMsg.InnerHtml = "Please use the 2 letter abbreviation for the State."
                    validReq = False
                ElseIf customer.Address.Length > 1 Then
                    For i = 0 To customer.Address.Length - 1
                        If Char.IsSymbol(customer.Address.Chars(i)) Then
                            adminErrorMsg.InnerHtml = "Please remove any special charecters from the customer's address."
                            validReq = False
                            Exit For
                        End If
                    Next

                    cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
                    cmd.Parameters.Add("@ESN", SqlDbType.VarChar).Value = varEsn
                    cmd.Parameters.Add("@MDN", SqlDbType.VarChar).Value = varOrderCellNumber
                    cmd.CommandText = "VerTransfer"
                    statusMsg = "Submitted Convert Request"
                End If

            ElseIf task = "newver" Then 'Port to New Verizon MDN (not used)
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
                cmd.Parameters.Add("@ESN", SqlDbType.VarChar).Value = varEsn
                cmd.Parameters.Add("@NPAZIP", SqlDbType.VarChar).Value = varOrderCellNumber.Substring(0, 6)
                cmd.CommandText = "VerNewService"
                statusMsg = "Submitted new Verizon MDN Request"

                'If task = "suspend" Then
                'md.CommandText = "VerSuspend"
                'statusMsg = "Submitted Suspend Request"
                'ElseIf task = "restore" Then
                ' cmd.CommandText = "VerRestore"
                ' statusMsg = "Submitted Restore Request"
                ' Else
            Else
                adminErrorMsg.InnerHtml = "Unrecognized Request :" + adminTasksRadiolist.SelectedValue
                ValidReq = False
            End If

            If validReq Then
                cmd.Connection.Open()
                cmd.CommandType = CommandType.StoredProcedure
                cmd.ExecuteNonQuery()

                cmd.CommandType = CommandType.Text
                cmd.CommandText = "UPDATE orders SET statusmsg = @Msg WHERE order_id = @OrderId; "
                cmd.Parameters.Add("@Msg", SqlDbType.VarChar).Value = statusMsg

                cmd.ExecuteNonQuery()
                cmd.Connection.Close()

                'to check results
                'reload = False
                'adminErrorMsg.InnerHtml = "Passed the check."
            Else 'otherwise, if the customer does not have all required fields, then display error message
                reload = False
            End If
        End If

        If reload Then
            TabContainer1.ActiveTabIndex = 0
            ReloadOrder()
        End If


    End Sub

   
    Protected Sub payButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles payButton.Click

        If carrierName.SelectedValue.ToLower = "all talk" Then
            Dim statusmsg As String = ""
            Dim valid As String = ""
            ATCPin.ATCVerify(varOrderCellNumber, statusmsg, "", valid)

            If valid = "Valid" Then
                ChargeOrder()
            Else
                'Response.Write("didn't verify")
                'Response.End()
                authNoteLbl.Text = "Transaction canceled. Phone number did not verify with Verizon. " & statusmsg
                authNoteLbl.ForeColor = Drawing.Color.Red
            End If
        Else
            If varStatus = "Porting" Or Not carrierName.SelectedValue.ToLower = varCarrierName.ToLower Then
                authNoteLbl.Text = "Transaction canceled. Porting in Progress"
                authNoteLbl.ForeColor = Drawing.Color.Red
            Else
                ChargeOrder()
            End If
        End If
    End Sub

    Protected Sub CancelOrder(ByVal s As Object, ByVal e As CommandEventArgs)
        Response.Redirect("~/SearchOrder.aspx")
    End Sub


    Protected Sub ClearStatusMsg(ByVal s As Object, ByVal e As CommandEventArgs)

        Dim sql As StringBuilder = New StringBuilder
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.Text

        sql.Append("update orders set statusmsg = '' WHERE [order_id] = ")
        sql.Append(varOrderID)

        cmd.Connection.Open()
        cmd.CommandText = sql.ToString()
    
        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

        ReloadOrder()

    End Sub

    Protected Sub saveCustomerBillingInfoBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles saveCustomerBillingInfoBtn.Click

        SaveCustomerBillingInfo()

        Dim noChargeRunCharge As New CreditCardCharge

        noChargeRunCharge.OrderId = varOrderID
        noChargeRunCharge.CellNumber = varOrderCellNumber

        noChargeRunCharge.CCNumber = creditCardNumber.Text
        noChargeRunCharge.CCExpiration = GetCCExpDate()
        noChargeRunCharge.CCCode = creditCardCode.Text

        noChargeRunCharge.User = Membership.GetUser.UserName
        noChargeRunCharge.Agent = salesRepDropdown.SelectedItem.Text

        noChargeRunCharge.RunCharge()

        ReloadOrder()

    End Sub

    Protected Sub prevCallDetails_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles prevCallDetails.Click
        useCalendar.Value = "false"




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
        useCalendar.Value = "true"
       
    End Sub

    Protected Sub nextCallDetail_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles nextCallDetail.Click
        useCalendar.Value = "false"

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
        useCalendar.Value = "true"
    End Sub

    Protected Sub SaveOrderNote(ByVal s As Object, ByVal e As CommandEventArgs)

        ClearInvoiceFields()

        Dim sql As StringBuilder = New StringBuilder
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.Text

        sql.Append("IF EXISTS(SELECT TOP 1 [order_id] FROM [order_notes] WHERE [order_id] = @OrderId) ")
        sql.Append("BEGIN UPDATE [order_notes] SET [note] = @Note WHERE [order_id] = @OrderId END ")
        sql.Append("ELSE BEGIN INSERT INTO [order_notes] ([order_id], [create_date], [note]) VALUES ")
        sql.Append("(@OrderId, getdate(), @Note) END ")

        cmd.Connection.Open()
        cmd.CommandText = sql.ToString()
        cmd.Parameters.Add("OrderId", SqlDbType.Int).Value = varOrderID
        cmd.Parameters.Add("Note", SqlDbType.VarChar).Value = orderNoteTextArea.InnerText.Trim

        Dim rowAffected As Integer = cmd.ExecuteNonQuery()

        If rowAffected > 0 Then
            varOrderNote = orderNoteTextArea.InnerText
            SetSaveNoteBtnProps()   ' Has a dependency on varOrderNote
        End If

        cmd.Connection.Close()

    End Sub

    Protected Sub downloadCdrBtn_Click()

        Dim xlExporter As DataTableToExcel = New DataTableToExcel

        BindCallDetailGridview()

        xlExporter.DataTable = orderCallDetails.DataSource()

        xlExporter.FileName = "CDR Report"
        xlExporter.Export()

        Exit Sub

    End Sub

    Protected Sub refreshIntlCallGV_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles refreshIntlCallGV.Click
        BindIntlCallsGridView()
    End Sub

    Protected Sub intlCallsGridView_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles intlCallsGridView.PageIndexChanging
        intlCallsGridView.PageIndex = e.NewPageIndex
        BindIntlCallsGridView()
    End Sub

    Protected Sub orderCallDetails_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles orderCallDetails.PageIndexChanging
        orderCallDetails.PageIndex = e.NewPageIndex
        BindCallDetailGridview()
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
    Private Sub ToggleCalendar()
        Response.Write("hello")
        Response.Write(TabContainer1.ActiveTabIndex)
        Response.Write(varCarrierName)
        ClientScript.RegisterStartupScript(Me.GetType(), "", "<script>alert('" & varCarrierName & "');</script>")
        'If varCarrierName.ToLower = "verizon" Then
        '    Response.Write("verizon")
        'ElseIf varCarrierName.ToLower = "page plus" Then
        '    ' GetPPCdr()
        '    Response.Write("pageplus")
        'End If
    End Sub

    Protected Sub activityGv_PageIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles activityGv.PageIndexChanged
        BindActivityGv()
    End Sub

    Protected Sub equipmentGridview_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles equipmentGridview.PageIndexChanging
        equipmentGridview.PageIndex = e.NewPageIndex
        BindEquipmentGridview()
    End Sub



    ' Charge functions
    Private Function CreditCardCharge() As Charge

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

        Return ccCharge

    End Function

    Private Function AgentAccountCharge() As Charge

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

        Return agentCharge

    End Function



    ' Helper functions
    Private Function StringDecode(ByVal value As String) As String
        Try
            Return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value))
        Catch ex As ArgumentNullException
            Return "0"
        Catch ex As FormatException
            ' This exception is caused by changing the URL, so send user to the SearchOrder page.
            Response.Redirect("~/SearchOrder.aspx")
            Return Nothing
        End Try
    End Function

    Function StringEncode(ByVal value As String) As String
        Return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))
    End Function

    ' When we avoid a resubmit on page refresh,
    ' we redirect the page to itself.
    ' Also, we maintain the current tab with the QueryString because were losing the viewstate
    Private Sub ReloadOrder()
        Dim str As String = "tab=" & TabContainer1.ActiveTabIndex.ToString
            str &= "&paymentMethod=" & selectPaymentMethod.SelectedIndex

            Session("_RedirectSaveState") = str
            Response.Redirect("~/Order.aspx?oid=" & StringEncode(varOrderID))
    End Sub

    Private Sub SetupNumberOfCallDetailsIntervals()
        If varCarrierName.ToLower = "verizon" Then
            GetVerCDRIntervals()
        ElseIf varCarrierName.ToLower = "page plus" Then
            For Each tab As AjaxControlToolkit.TabPanel In TabContainer1.Tabs()
                If tab.ID = "TabPanel6" Then   ' CallDetail tab
                    tab.Visible = False
                End If
            Next
        End If
    End Sub

    Private Sub GetPPCDRIntervals()
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

    Private Sub GetVerCDRIntervals()

        Dim cmd As New SqlCommand
        Dim sql As New StringBuilder

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        cmd.Connection.Open()

        cmd.CommandType = Data.CommandType.Text
        'cmd.CommandText = "SELECT COUNT(*) AS [Intevals] FROM VerRegister WHERE MID = @CellNum AND Effective IS NOT NULL; "
        cmd.CommandText = " SELECT COUNT(*) AS [Intevals] from authtrans where cell_num = @CellNum and(month_processed is not null or cash_processed is not null);"
        ' cmd.CommandText = "select DATEDIFF(mm,create_date,GETDATE())+ 1 AS [Intevals] from orders where cell_num =  @CellNum; "
        ' cmd.CommandText = " select  top 1 DATEDIFF(mm,ISNULL(month_processed,cash_processed) ,GETDATE())+ 1 AS [Intevals] from authtrans where cell_num = @CellNum and(month_processed is not null or cash_processed is not null) order by ISNULL(month_processed,cash_processed) ; "

        ' cmd.CommandText = "select top 1 DATEDIFF(mm, (CONVERT(DATETIME," + "'" + startDate + "'" + ", 121)) ,(CONVERT(DATETIME," + "'" + endDate + "'" + ", 121))) AS [Intevals] from Verizon.dbo.CDR where MID =  @CellNum; "

        cmd.Parameters.Add("@CellNum", SqlDbType.VarChar).Value = varOrderCellNumber

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            If reader.Item("Intevals") IsNot DBNull.Value Then
                hdnNumOfCallDetailsIntervals.Value = reader.Item("Intevals") + 1 '
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

    Private Function SaveOrder() As Boolean
        Return CreateOrder()
    End Function

    Private Function CreateOrder() As Boolean

        Dim cmd As New SqlCommand
        Dim sql As StringBuilder = New StringBuilder
        Dim success As Boolean = False

        cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        ' Insert into customer
        sql.Append("INSERT INTO [customers] ([initial_agent], [fname], [lname], [prefix], [address], ")
        sql.Append("[city], [state], [phone], [zip], [email], [cus_pin]) ")
        sql.Append("VALUES (@Agent, @fname, @lname, @prefix, @address, @city, @state, @phone, @zip, @email, @customer_pin); ")

        ' Insert into orders
        sql.Append("INSERT INTO [orders] ([customer_id], [update_date], [cell_num], [serial_num], [esn],  ")
        sql.Append("[intl_pin], [carrier_name], [vendor_pin], [monitor] ) ")
        sql.Append("OUTPUT inserted.order_id ")
        sql.Append("VALUES (SCOPE_IDENTITY(), getdate(), @cell_number, @serial_number, @esn, ") ' Insert customer_id into the orders table
        sql.Append("@intl_pin, @carrier_name, @vendor_pin, @monitor ); ")

        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value =
            If(Session("UserLevel") = 3, Membership.GetUser.UserName, initialAgentDropdown.SelectedItem.Text)
        cmd.Parameters.Add("@prefix", SqlDbType.VarChar).Value = StrOrDBNull(prefix.Text)
        cmd.Parameters.Add("@fname", SqlDbType.VarChar).Value = StrOrDBNull(fname.Text)
        cmd.Parameters.Add("@lname", SqlDbType.VarChar).Value = StrOrDBNull(lname.Text)
        cmd.Parameters.Add("@address", SqlDbType.VarChar).Value = StrOrDBNull(address.Text)
        cmd.Parameters.Add("@city", SqlDbType.VarChar).Value = StrOrDBNull(city.Text)
        cmd.Parameters.Add("@state", SqlDbType.VarChar).Value = StrOrDBNull(state.Text)
        cmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = StrOrDBNull(phone.Text)
        cmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = StrOrDBNull(zip.Text)
        cmd.Parameters.Add("@email", SqlDbType.VarChar).Value = StrOrDBNull(email.Text)
        cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = StrOrDBNull(
            If(carrierName.SelectedValue.ToLower = "page plus" Or carrierName.SelectedValue.ToLower = "all talk", cell_number.Text, "")
        )
        cmd.Parameters.Add("@esn", SqlDbType.VarChar).Value = StrOrDBNull(hiddenESN.Value)
        cmd.Parameters.Add("@intl_pin", SqlDbType.VarChar).Value = StrOrDBNull(hiddenIntlPin.Value)
        cmd.Parameters.Add("@serial_number", SqlDbType.VarChar).Value = StrOrDBNull(serialNumber.Text)
        cmd.Parameters.Add("@carrier_name", SqlDbType.VarChar).Value = carrierName.SelectedItem.Text

        If carrierName.SelectedValue.ToLower = "page plus" Then
            cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = cell_number.Text.Substring(cell_number.Text.Length - 4)
        Else
            cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = DBNull.Value
        End If

        cmd.Parameters.Add("@customer_pin", SqlDbType.VarChar).Value = StrOrDBNull(customerPin.Text)
        If carrierName.SelectedValue.ToLower = "verizon" Then
            cmd.Parameters.Add("@monitor", SqlDbType.Bit).Value = DBNull.Value
        Else
            cmd.Parameters.Add("@monitor", SqlDbType.Bit).Value = monitor.Checked
        End If

        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.Connection.Open()
        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()
            varOrderID = reader.Item("order_id")
            success = True
        End If
        cmd.Connection.Close()


        If carrierName.SelectedValue.ToLower = "all talk" Then
            Dim con As SqlConnection =
         New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

            Dim cmd2 As SqlCommand = New SqlCommand("GetCallDetailIntervals", con)
            cmd2.Connection.Open()

            cmd2.CommandType = Data.CommandType.StoredProcedure

            cmd2.Parameters.Add("MID", SqlDbType.VarChar).Value = cell_number.Text
            cmd2.CommandText = "ATC_MDNNew"
            cmd2.ExecuteNonQuery()
            cmd2.Connection.Close()

        End If



        Return success

    End Function

    Private Function SubmitVerReq() As String

        Dim cmd As New SqlCommand
        Dim statusMsg As String = Nothing

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Connection.Open()

        If newNumberRadioType.SelectedValue.ToLower = "new" Then
            cmd.CommandType = CommandType.StoredProcedure
            cmd.CommandText = "VerNewService"
            cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
            cmd.Parameters.Add("@ESN", SqlDbType.VarChar).Value = hiddenESN.Value
            cmd.Parameters.Add("@NpaZip", SqlDbType.VarChar).Value =
                If(createZipNPARadioList.SelectedValue = "createPhoneZip", createPhoneZip.Text,
                    String.Concat(createPhoneNPA.Text, createPhoneNXX.Text))
            statusMsg = "Submitted New Service Request"
        ElseIf newNumberRadioType.SelectedValue.ToLower = "port" Then
            cmd.CommandText = "VerNewServicePort"
            cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
            cmd.Parameters.Add("@MDN", SqlDbType.VarChar).Value = portPhone.Text.Trim
            cmd.Parameters.Add("@PORTPASSWD", SqlDbType.VarChar).Value = portPwFld.Text.Trim
            cmd.Parameters.Add("@ESN", SqlDbType.VarChar).Value = hiddenESN.Value
            cmd.Parameters.Add("@PORTTYPE", SqlDbType.VarChar).Value = phoneOriginTypeRadio.SelectedValue
            cmd.Parameters.Add("@PORTACCT", SqlDbType.VarChar).Value = carrierCodeFld.Text
            statusMsg = "Submitted New Service Port Request"
        End If

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

        Return statusMsg

    End Function

    Private Function SubmitChangeVerEsn() As String
        Dim cmd As New SqlCommand

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Connection.Open()

        cmd.CommandText = "VerChangeESN"
        cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = varOrderID
        cmd.Parameters.Add("@MDN", SqlDbType.VarChar).Value = varOrderCellNumber
        cmd.Parameters.Add("@NEWESN", SqlDbType.VarChar).Value = hiddenESN.Value

        cmd.ExecuteNonQuery()

        confirmSerialNumber.Text = ""
        esn.Text = hiddenESN.Value

        cmd.Connection.Close()

        Return "Submitted Change ESN Request"

    End Function
    Protected Sub orderErrorMessage_Load(sender As Object, e As System.EventArgs) Handles orderErrorMessage.Load
        'to keep message of whether esn was changed or not when user updated esn
        If Not IsPostBack Then
            If Not IsNothing(Session("esnError")) Then
                orderErrorMessage.Style("color") = "red"
                orderErrorMessage.InnerText = CType(Session("esnError"), String)
                Session.Remove("esnError")
            End If
        End If
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
        sql.Append("[cell_num] = @cell_number, ")

        If carrierName.SelectedValue.ToLower = "page plus" Then
            sql.Append("[serial_num] = @serial_number, [esn] = @esn,  ")
        ElseIf carrierName.SelectedValue.ToLower = "all talk" Then
            'Response.Write("OldESN: " & varEsn & " NewESN: " & hiddenESN.Value)
            'Response.End()
            If varEsn <> hiddenESN.Value Then
                'If ATClass.ATCPinAssign.ATCChangeEsn(varOrderCellNumber, varEsn, hiddenESN.Value) Then
                If ATCPin.ATCChangeEsn(varOrderCellNumber, varEsn, hiddenESN.Value) Then
                    sql.Append("[serial_num] = @serial_number, [esn] = @esn,  ")
                    Session("esnError") = "ESN changed successfully." 'message saved in session variable, to be able to retrieve it after order is reloaded
                Else
                    Session("esnError") = "ESN Change failed on vendor side."
                End If
            End If
        ElseIf carrierName.SelectedValue.ToLower = "verizon" Then
            If (serialNumber.Text = varSerialNumber) Then
                sql.Append("[serial_num] = @serial_number, [esn] = @esn,  ")
            End If
        End If

        sql.Append("[intl_pin] = @intl_pin, [carrier_name] = @carrier_name, ")
        sql.Append("[vendor_pin] = @vendor_pin, [monitor] = @monitor ")
        sql.Append("WHERE [order_id] = @OrderID ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)

        If Session("UserLevel") = 1 Or Session("UserLevel") = 2 Then
            cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = initialAgentDropdown.SelectedItem.Text
        End If

        cmd.Parameters.Add("@customer_id", SqlDbType.Int).Value = varCustomerId
        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID

        cmd.Parameters.Add("@prefix", SqlDbType.VarChar).Value = StrOrDBNull(prefix.Text)
        cmd.Parameters.Add("@fname", SqlDbType.VarChar).Value = StrOrDBNull(fname.Text)
        cmd.Parameters.Add("@lname", SqlDbType.VarChar).Value = StrOrDBNull(lname.Text)
        cmd.Parameters.Add("@address", SqlDbType.VarChar).Value = StrOrDBNull(address.Text)
        cmd.Parameters.Add("@city", SqlDbType.VarChar).Value = StrOrDBNull(city.Text)
        cmd.Parameters.Add("@state", SqlDbType.VarChar).Value = StrOrDBNull(state.Text)
        cmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = StrOrDBNull(phone.Text)
        cmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = StrOrDBNull(zip.Text)
        cmd.Parameters.Add("@email", SqlDbType.VarChar).Value = StrOrDBNull(email.Text)
        cmd.Parameters.Add("@cell_number", SqlDbType.VarChar).Value = StrOrDBNull(cell_number.Text)

        cmd.Parameters.Add("@serial_number", SqlDbType.VarChar).Value = StrOrDBNull(serialNumber.Text)
        cmd.Parameters.Add("@esn", SqlDbType.VarChar).Value = StrOrDBNull(hiddenESN.Value)

        cmd.Parameters.Add("@intl_pin", SqlDbType.VarChar).Value = StrOrDBNull(hiddenIntlPin.Value)
        cmd.Parameters.Add("@carrier_name", SqlDbType.VarChar).Value = carrierName.SelectedItem.Text

        If carrierName.SelectedValue.ToLower = "page plus" Then
            cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = vendorPin.Text
        Else
            cmd.Parameters.Add("@vendor_pin", SqlDbType.VarChar).Value = DBNull.Value
        End If

        cmd.Parameters.Add("@customer_pin", SqlDbType.VarChar).Value = StrOrDBNull(customerPin.Text)
        If carrierName.SelectedValue.ToLower = "verizon" Then
            cmd.Parameters.Add("@monitor", SqlDbType.Bit).Value = DBNull.Value
        Else
            cmd.Parameters.Add("@monitor", SqlDbType.Bit).Value = monitor.Checked
        End If

        cmd.Connection.Open()

        Dim rowsAffected As Integer = cmd.ExecuteNonQuery

        cmd.Connection.Close()

        If rowsAffected > 0 Then
            ReloadOrder()
        End If

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
        cmd.Parameters.Add("cellnum", SqlDbType.VarChar).Value = varOrderCellNumber
        cmd.Parameters.Add("amount", SqlDbType.Money).Value = intlBalanceDropdown.SelectedValue

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

        Dim cmd As SqlCommand = Nothing
        Dim sql As New StringBuilder

        ' Update phone (order)
        If carrierName.SelectedValue.ToLower = "all talk" And monthlyPlanDropdown.SelectedIndex > 0 Then
            'set monthly_plan_id to plan id so that when user goes into renewals, the monthly_renewal_dropdown should be populated to the users last payment plan type
            sql.Append("UPDATE [orders] SET [update_date] = getdate(), ")
            sql.Append("[plan_id] = @PlanId, [monthly_plan_id] = @PlanId ")
            sql.Append("WHERE [order_id] = @OrderID")
        Else
            sql.Append("UPDATE [orders] SET [update_date] = getdate(), ")
            sql.Append("[plan_id] = @PlanId ")
            sql.Append("WHERE [order_id] = @OrderID")
        End If
        

        cmd = New SqlCommand(sql.ToString, connection)

        ' Order ID for updating the order table
        cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = varOrderID
        cmd.Connection.Open()

        ' Get plan id
        'Dim planId As Integer = 0 DL
        Dim PlanId = varPlanID
        Dim monthlyId = monthlyPlanDropdown.SelectedValue
        Dim cashId = cashBalanceDropdown.SelectedValue

        ' If varCarrierName.ToLower = "verizon" Then
        'Dim strMsg As String = "Planid=" + PlanId.ToString() + "MothlyId= " + monthlyId.ToString() + ",CashId=" + cashId.ToString()
        'Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "alert", "alert('" + strMsg + "')", True)
        'Response.Write(strMsg)
        ' Response.End()
        'End If '

        If monthlyId > 0 Then PlanId = monthlyId
        'ElseIf cashId > 0 Then DL
        '   planId = cashId
        If PlanId = 0 And cashId > 0 Then PlanId = cashId

        If PlanId > 0 Then
            cmd.Parameters.Add("@PlanId", SqlDbType.Int).Value = PlanId
        Else
            cmd.Parameters.Add("@PlanId", SqlDbType.Int).Value = DBNull.Value
        End If

        cmd.ExecuteNonQuery()
        connection.Close()

        ' Deprecated DL Important: This relies on the plan_id being updated.
        ' Only call this after the above code.
        'Moved below 10/21/14
        'If varCarrierName.ToLower = "verizon" _
         '       And (monthlyPlanDropdown.SelectedValue > 0 Or cashBalanceDropdown.SelectedValue > 0) Then
        'UpdateVerMdn()
        'End If

        Dim charge As Charge = Nothing
        If selectPaymentMethod.Items(0).Selected Then   ' Selected method of payment: Credit card
            ' Charge the credit card
            charge = CreditCardCharge()
        ElseIf selectPaymentMethod.Items(1).Selected Then   ' Selected method of payment: Agent account
            ' Charge the agent account
            charge = AgentAccountCharge()
        End If

        If Not charge.hasCharged Then
            ' Reset the itemized cost labels.
            SetInvoiceAmounts()

            ' Register functions that get the invoice total and sets the pay button to enabled.
            Dim strScript As New StringBuilder
            strScript.Append("<SCRIPT> calculateTotal();")
            strScript.Append("TogglePayButton();</SCRIPT>")
            ClientScript.RegisterStartupScript(Me.GetType(), "recalculateInvoice", strScript.ToString)

            ' If there is an error, set the color to brown.
            authNoteLbl.Text = charge.AuthMessage
            authNoteLbl.ForeColor = Drawing.Color.Brown

            BindActivityGv()

        Else
            If intlBalanceDropdown.SelectedIndex > 0 And Not String.IsNullOrEmpty(varOrderCellNumber) Then
                PayIntlPin()
            End If

            'If carrierName.SelectedValue.ToLower = "all talk" And (monthlyPlanDropdown.SelectedIndex > 0 Or cashBalanceDropdown.SelectedIndex > 0) Then
            If carrierName.SelectedValue.ToLower = "all talk" And monthlyPlanDropdown.SelectedIndex > 0 Then
                'if charge went through, update stack to pincount + 1
                'Dim sqlcmd As New SqlCommand
                'sqlcmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)
                'sqlcmd.CommandText = "Update atc_mdn set pincount = isnull([pincount], 0) + 1 where phonenumber = @phonenum"
                'sqlcmd.Parameters.Add("phonenum", SqlDbType.VarChar).Value = varOrderCellNumber
                'sqlcmd.Connection.Open()
                'sqlcmd.ExecuteNonQuery()
                'sqlcmd.Connection.Close()

                Dim statusMsg As String = ""
                Dim pinType As String = ""
                'If monthlyPlanDropdown.SelectedIndex > 0 Then
                pinType = monthlyPlanDropdown.SelectedItem.ToString
                'ElseIf cashBalanceDropdown.SelectedIndex > 0 Then
                '   pinType = cashBalanceDropdown.SelectedItem.ToString
                'End If


                'In assignATCPin, i am sending in monthlyplandropdown.selecteditem as plantype. monthlyplandropdown is populated from the plans table, 
                'and its value is now gonna be used as pintype to assign pin in pins table (this happens in function assignATCPin0.  So the pintype column 
                'in atc_pins table, and the planname column in plans table have to match values

                ATCPin.AssignATCPin(varOrderCellNumber, pinType, charge.TransactionId, statusMsg)
                'ATCPin.ATCStatus(varOrderCellNumber, statusmsg2)
                'ATClass.ATCPinAssign.AssignATCPin(varOrderCellNumber, monthlyPlanDropdown.SelectedItem.ToString, statusMsg)

                If statusMsg <> "" Then
                    Session("statusmsg") = statusMsg
                End If

            End If

            'Moved her to ensure paymeny went thru : 10/21/14
            If varCarrierName.ToLower = "verizon" _
               And (monthlyPlanDropdown.SelectedValue > 0 Or cashBalanceDropdown.SelectedValue > 0) Then
                UpdateVerMdn()
            End If

            Session("TransMsg") = charge.AuthMessage
            SaveAuthTransItems(charge)
            InsertCommission(charge.TransactionId)
            'Response.Write(charge.TransactionId)
            'Response.End()
            ReloadOrder()
        End If

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

    Private Sub UpdateVerMdn()
        Dim cmd As New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        cmd.CommandType = CommandType.StoredProcedure

        'cmd.CommandText = "VerPay"  10/21/14 
        cmd.CommandText = "VerNewPayment"
        cmd.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = varOrderCellNumber
        cmd.Parameters.Add("@monthplanid", SqlDbType.VarChar).Value = monthlyPlanDropdown.SelectedValue
        cmd.Parameters.Add("@cashplanid", SqlDbType.VarChar).Value = cashBalanceDropdown.SelectedValue

        cmd.Connection.Open()
3:
        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

    End Sub

    Private Sub DisableOrder()
        TabContainer1.Enabled = False
        closeOrderButton.ToolTip = "Set order to be enabled."
        closeOrderButton.Text = "Unlock Order"
        closeOrderButton.CommandName = "open"
    End Sub

    Private Sub EnableOrder()
        TabContainer1.Enabled = True
        closeOrderButton.ToolTip = "Set order to be disabled."
        closeOrderButton.Text = "Lock Order"
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
    'Private Sub ToggleCalendar()
    '    If TabContainer1.ActiveTabIndex = 5 Then
    '        showCalendar1.Style("display") = "none"
    '    End If

    'End Sub

    Private Function ReplaceDBNull(ByVal val As Object, ByVal replace As Object) As System.Object
        If val Is DBNull.Value Then
            Return replace
        Else
            Return val
        End If
    End Function

    Private Function StrOrDBNull(ByVal str As String) As Object
        If String.IsNullOrEmpty(str.Trim) Then
            Return DBNull.Value
        Else
            Return str.Trim
        End If
    End Function

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

    Private Sub SetCellNum(ByVal cell As String)
        Dim carrier As String = If(_isNewOrder, carrierName.SelectedValue, varCarrierName)
        cell_number.Text = cell
        If carrier.ToLower = "verizon" Then
            verCellLbl.Text = cell
        End If
        If carrier.ToLower = "all talk" Then
            cell_number.ReadOnly = True
        End If
    End Sub

    Private Function DoubleQuoteString(ByVal str As String) As String
        Return ControlChars.Quote & str & ControlChars.Quote
    End Function

    Private Sub InsertCommission(ByVal transId As Integer)
        Dim cmd As New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "InsertTransCommission"
        cmd.Parameters.Add("TransId", SqlDbType.Int).Value = transId

        cmd.Connection.Open()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()
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

        If getAction = "isCellNumberUnique" Then
            Dim args As String = eventArgument.Substring(InStr(eventArgument, ":"))
            IsCellNumberUnique(args)
        End If

        If getAction = "refreshVerStatusMsg" Then
            _callBackResult = GetVerInfo()
        End If

        If getAction = "getEncodedOid" Then
            _callBackResult = StringEncode(varOrderID)
        End If
       

    End Sub

    Public Function GetCallbackResult() As String _
     Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function

    Protected Sub esnLkBn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles esnLkBn.Click
        esnLkBn.Visible = False
        If varCarrierName.ToLower = "page plus" Then
            Dim cmd As New SqlCommand
            cmd.Connection =
                New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

            cmd.CommandType = CommandType.StoredProcedure
            cmd.CommandText = "run_esn_query"
            cmd.CommandTimeout = 150

            cmd.Parameters.Add("@esn", SqlDbType.VarChar).Value = varEsn

            cmd.Connection.Open()

            cmd.ExecuteNonQuery()

            cmd.Connection.Close()
            GetPPData()
            
               

            'SetOrderInfoBar()
        ElseIf varCarrierName.ToLower = "all talk" Then
            Dim statusmsg As String = ""
            'varATCVerify = ATClass.ATCPinAssign.ATCVerify(varOrderCellNumber, statusmsg, varLastValidated)
            'esnValidLbl.Visible = True
            'esnLbl.Visible = True
            'esnLbl.Visible = True
            'esnLkBn.Visible = True
            'If varATCVerify = "Not Then
            '    esnLbl.Text = "Not Validated" 'If Not varCarrierName.ToLower = "all talk" Then 
            '    esnLbl.ForeColor = Drawing.Color.Red
            '    LastValidateLbl.Visible = True
            '    LastValidateDateLbl.Visible = True
            '    LastValidateDateLbl.Text = varLastValidated

            'Else
            '    esnLbl.Text = "Valid"
            '    esnLbl.ForeColor = Drawing.Color.Black
            '    LastValidateLbl.Visible = True
            '    LastValidateDateLbl.Visible = True
            '    LastValidateDateLbl.Text = varLastValidated
            'End If
            ATCPin.ATCVerify(varOrderCellNumber, statusmsg, varLastValidated, "")
            'ATClass.ATCPinAssign.ATCVerify(varOrderCellNumber, statusmsg, varLastValidated)
            If statusmsg <> "" Then
                toggleOrderErrorMsg.Style("color") = "red"
                toggleOrderErrorMsg.InnerText = statusmsg
            End If
            GetATData()
           
        End If



        SetOrderInfoBar()
        esnLkBn.Visible = True

    End Sub

    Protected Sub statusLkBn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles statusLkBn.Click
        statusLkBn.Visible = False

        If varCarrierName.ToLower = "page plus" Then
            Dim cmd As New SqlCommand
            cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings(_ppcConStr).ConnectionString)

            cmd.CommandType = CommandType.StoredProcedure


            cmd.CommandText = "run_mdn_query"
            cmd.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = varOrderCellNumber


            cmd.CommandTimeout = 150
            cmd.Connection.Open()

            cmd.ExecuteNonQuery()

            cmd.Connection.Close()
            GetPPData()
        ElseIf varCarrierName.ToLower = "verizon" Then
            GetVerData()
        ElseIf varCarrierName.ToLower = "all talk" Then
            Dim statusmsg As String = ""
            ATCPin.ATCStatus(varOrderCellNumber, statusmsg)
            'ATClass.ATCPinAssign.ATCStatus(varOrderCellNumber, statusmsg)
            If statusmsg <> "" Then
                toggleOrderErrorMsg.Style("color") = "red"
                toggleOrderErrorMsg.InnerText = statusmsg
            End If
            'Response.Write(statusmsg)
            'Response.End()

            GetATData()
        End If

        SetOrderInfoBar()
        statusLkBn.Visible = True


    End Sub

    
End Class
