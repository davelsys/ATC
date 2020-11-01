Imports System.Globalization

Partial Class Agents
    Inherits System.Web.UI.Page

    Dim userLevel As Integer = 0
    Dim _ppcConStr As String = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        If Session.Item("UserLevel") Is Nothing Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        ' This needs to be set before anything else is called.
        userLevel = Session.Item("UserLevel")

        If Not IsPostBack Then
            PopulateSalesRepDropdown()
            PopulateCCExpDropdowns()
            PopulateAgentPaymentInfo()
            SetCreditLimitLabel()
            BindAuthtransGrid()
            BindAgentCreditsGV()
        End If

        BindCommissions()
        
        ' To prevent resubmits we redirect, however we lose ViewState so we maintain
        ' some states in the session as a serialized string
        If Not IsNothing(Session("_RedirectSaveState")) Then
            Dim parts() As String = CType(Session("_RedirectSaveState"), String).Split("&")
            creditAgentTabs.ActiveTabIndex = CType(CType(parts(0), String).Split("=")(1), Integer)
            ' This tells JavaScript not to change the tab indexes
            If Not ClientScript.IsClientScriptBlockRegistered("testBillingInfo") Then
                ClientScript.RegisterClientScriptBlock(Me.GetType, "testBillingInfo", 
                        "var testBillingInfo = false;", true)
            End If
            Session.Remove("_RedirectSaveState")
        End If

        ' Clear error messages
        agntPayError.InnerText = ""
        
    End Sub
    
    Protected Sub creditAgentTabs_Load(sender As Object, e As System.EventArgs) Handles creditAgentTabs.Load
        If CType(Session.Item("UserLevel"), Integer) <> 1 Then
            creditAgentTabs.FindControl("adminCreditAgent").Visible = False
        Else
            creditAgentTabs.FindControl("adminCreditAgent").Visible = True
        End If
    End Sub
    
    Protected Sub commissionsGVTab_Load(sender As Object, e As System.EventArgs) Handles commissionsGVTab.Load
        Dim cmd As New SqlCommand
        Dim show As Boolean = False

        cmd.Connection = New SqlConnection(_ppcConStr)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT ( CASE WHEN "
        cmd.CommandText &= "COALESCE( CommissionPlan, 'No Commission' ) = 'No Commission' THEN 'false' "
        cmd.CommandText &= " ELSE 'true' END ) AS HasCom "
        cmd.CommandText &= "FROM [ASPNETDB].[dbo].[aspnet_Users] WHERE UserName = @Agent; "
        cmd.Parameters.Add("Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedValue

        cmd.Connection.Open

        Using reader As SqlDataReader = cmd.ExecuteReader
            If reader.Read Then
                show = Boolean.Parse(reader.Item("HasCom").ToString)
                commissionsGVTab.Visible = show
                commissionAmountSpan.Visible = show
            End If
        End Using

        cmd.Connection.Close

    End Sub

    Private Sub PopulateSalesRepDropdown()

        Dim con As SqlConnection = New SqlConnection(_ppcConStr)

        con.Open()

        Dim cmd As SqlCommand

        Dim sql As String = "SELECT [UserName], [LoweredUserName] FROM [ASPNETDB].[dbo].[aspnet_Users] "
        cmd = New SqlCommand(sql, con)

        Using dtr = cmd.ExecuteReader
            salesRepDropdown.DataSource = dtr
            salesRepDropdown.DataTextField = "UserName"
            salesRepDropdown.DataValueField = "LoweredUserName"
            salesRepDropdown.DataBind()
        End Using

        Dim userName As String = Membership.GetUser().UserName
        Dim userIndex As Integer = salesRepDropdown.Items.IndexOf(salesRepDropdown.Items.FindByValue(userName))
        If userLevel <> 1 Then
            salesRepDropdown.Enabled = False
            salesRepDropdown.SelectedIndex = userIndex
        Else
            salesRepDropdown.SelectedIndex = 0
        End If

        con.Close()

    End Sub

    Private Sub PopulateCCExpDropdowns()
        ' Set up the year dropdown
        Dim year As Integer = Now.Year Mod 100
        For y = year To (year + 10)
            creditCardExpirationYear.Items.Add(y)
        Next
    End Sub

    Private Sub PopulateAgentPaymentInfo()

        Dim cmd As New SqlCommand
        Dim reader As SqlDataReader
        Dim agent As String = salesRepDropdown.SelectedValue
        Dim dateParts As String()
        Dim savedBillingInfo As String = "false"

        cmd.Connection = New SqlConnection(_ppcConStr)
        cmd.Connection.Open()

        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT * FROM AgentsCCInfo WHERE UserId = @UserId; "
        cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = New Guid(Membership.GetUser(agent).ProviderUserKey.ToString)

        reader = cmd.ExecuteReader
        If reader.HasRows Then
            reader.Read
            creditCardNumber.Text = If(IsDBNull(reader.Item("CCLastFour")), "", "************" & reader.Item("CCLastFour"))
            dateParts = Split(If(IsDBNull(reader.Item("CCExpirationDate")), "", reader.Item("CCExpirationDate")), "-")
            creditCardExpirationYear.SelectedIndex = 
                If(dateParts(0).Length > 0, 
                   creditCardExpirationYear.Items.IndexOf(creditCardExpirationYear.Items.FindByValue(dateParts(0) Mod 100) ),
                   0 )
            creditCardExpirationMonth.SelectedIndex =
                If( dateParts.Length > 1,
                   creditCardExpirationMonth.Items.IndexOf(creditCardExpirationMonth.Items.FindByValue(dateParts(1))),
                   0)
            billFnameFld.Text   = If( IsDBNull(reader.Item("BillingFName")), "", reader.Item("BillingFName") )
            billLnameFld.Text   = If( IsDBNull(reader.Item("BillingLName")), "", reader.Item("BillingLName") )
            billAddressFld.Text = If( IsDBNull(reader.Item("BillingAddress")), "", reader.Item("BillingAddress") )
            billCityFld.Text    = If( IsDBNull(reader.Item("BillingCity")), "", reader.Item("BillingCity") )
            billStateFld.Text   = If( IsDBNull(reader.Item("BillingState")), "", reader.Item("BillingState") )
            billZipFld.Text     = If( IsDBNull(reader.Item("BillingZip")), "", reader.Item("BillingZip") )
            billPhoneFld.Text   = If( IsDBNull(reader.Item("BillingPhone")), "", reader.Item("BillingPhone") )
            billEmailFld.Text   = If( IsDBNull(reader.Item("BillingEmail")), "", reader.Item("BillingEmail") )

            savedBillingInfo = If( IsDBNull(reader.Item("BillingLName")), "false", "true" )
        End If
        
        agentPayAmount.Text = ""
        adminAgentPayFld.Text = ""

        hasBillingInfo.Value = savedBillingInfo

        cmd.Connection.Close()

    End Sub

    Private Sub SetCreditLimitLabel()

        Dim creditLimit = GetAgentCreditLimit()
        Dim totalCom = GetAgentCommissions
        Dim totalCharges = GetAgentTotalCharges()
        Dim available = ( ( creditLimit + totalCom ) - totalCharges )
        Dim balance = available - creditLimit

        creditLimitLbl.Text = FormatCurrency(creditLimit, 2)

        creditUsedLbl.Text = FormatCurrency(totalCharges, 2)
        totalChargesLbl.Text = FormatCurrency(totalCharges, 2)

        creditAvailableLbl.Text = FormatCurrency(available, 2)

        totalComLbl.Text = FormatCurrency(totalCom, 2)
        totalComTabLbl.Text = FormatCurrency(totalCom, 2)

        balanceLbl.Text = FormatCurrency(balance, 2)

    End Sub

    Private Function GetAgentCreditLimit(Optional ByVal userName As String = "") As Decimal
        Dim creditLimit As Decimal = 0
        Dim con As SqlConnection = New SqlConnection(_ppcConStr)
        Dim sql As String = "SELECT [CreditLimit] FROM [ASPNETDB].[dbo].[aspnet_Users] WHERE [UserName] = @UserName"

        con.Open()

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        If userName = "" Then
            cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text
        Else
            cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = userName
        End If


        Dim reader As SqlDataReader = cmd.ExecuteReader()

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

    Private Function GetAgentTotalCharges() As Decimal
        Dim con As SqlConnection = New SqlConnection(_ppcConStr)
        con.Open()

        Dim sql As String = "SELECT sum([total]) AS total FROM [authtrans] WHERE [agent] = @Agent AND charged = 1 AND trans_type IN ( 2, 3 ); "

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text

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

    Private Function GetAgentCommissions As Decimal
        Dim tot As Decimal = Nothing
        Dim cmd As New SqlCommand
        cmd.Connection = New SqlConnection(_ppcConStr)
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT SUM(CommissionAmount) AS total FROM Commissions WHERE Agent = @Agent; "
        cmd.Parameters.Add("Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedValue

        cmd.Connection.Open

        Using reader As SqlDataReader = cmd.ExecuteReader
            If reader.Read Then
                tot = Decimal.Parse( If(IsDBNull(reader.Item("total")), 0, reader.Item("total")) )
            End If
        End Using

        cmd.Connection.Close
        Return tot
    End Function

    Private Sub BindAuthtransGrid()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        Dim dt As New DataTable

        sql.Append("SELECT (CASE WHEN isnull([authtrans].[cell_num], '') = '' THEN ")
        sql.Append("[orders].[cell_num] ELSE [authtrans].[cell_num] END) AS [Cell Phone], ")
        sql.Append("(isnull([fname], '') + ' ' + [lname]) AS [Name], ")

        sql.Append("DATENAME(MM, [paydate]) + RIGHT(CONVERT(VARCHAR(12), [paydate], 107), 9) AS [Pay Date], ")

        sql.Append("CONVERT( VARCHAR(12), [total], 1 ) AS [Total]  FROM [authtrans] ")
        sql.Append("LEFT JOIN [orders] ON [authtrans].[orderid] = [orders].[order_id] ")
        sql.Append("LEFT JOIN [customers] ON [customers].[customer_id] = [orders].[customer_id] ")
        sql.Append("WHERE [agent] = @Agent AND charged = 1 AND ( trans_type = 2 OR trans_type = 3 ) ORDER BY paydate desc ")

        cmd.Connection = New SqlConnection(_ppcConStr)
        cmd.CommandType = SqlDataSourceCommandType.Text
        cmd.CommandText = sql.ToString
        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text

        cmd.Connection.Open()

        adapter.SelectCommand = cmd
        adapter.Fill(dt)

        For Each row As DataRow In dt.Rows
            row.Item("Total") = FormatCurrency(row.Item("Total"), 2)
        Next

        agentAuthtransGridview.DataSource = dt
        agentAuthtransGridview.DataBind()

        cmd.Connection.Close()
    End Sub

    Private Sub BindAgentCreditsGV()
        Dim cmd As New SqlCommand
        Dim sql As New StringBuilder

        sql.Append("SELECT agent, [user], paydate, (0 - total) AS total, authtransid, authmessage ")
        sql.Append("FROM authtrans WHERE agent = @Agent AND ( (trans_type = 3) ")
        sql.Append("OR (trans_type = 2 AND orderid IS NULL AND cell_num IS NULL AND total < 0) ) ")
        sql.Append("AND charged = 1 ORDER BY paydate DESC; ")

        cmd.Connection = New SqlConnection(_ppcConStr)
        cmd.Connection.Open()

        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedValue

        agentCreditTransGV.DataSource = cmd.ExecuteReader
        agentCreditTransGV.DataBind

        cmd.Connection.Close()
    End Sub

    Private Sub BindCommissions()
        Dim sql As New StringBuilder
        sql.Append("SELECT a.cell_num AS Cell, TransDate AS [Date], CommissionItem AS Item, ")
        sql.Append("'$' + CAST( CommissionAmount AS VARCHAR(12) ) AS Amount ")
        sql.Append("FROM Commissions co JOIN authtrans a ON co.TransId = a.transid ")
        sql.Append("JOIN orders o ON a.orderid = o.order_id ")
        sql.Append("WHERE co.Agent = @Agent ORDER BY TransDate DESC; ")

        agentCommissionsDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        agentCommissionsDataSource.SelectCommand = sql.ToString
        agentCommissionsDataSource.CancelSelectOnNullParameter = False

        agentCommissionsGV.DataBind()
    End Sub

    Protected Sub downloadAgentsBtn_ServerClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles downloadAgentsBtn.ServerClick

        Dim xlExporter As DataTableToExcel = New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        BindAuthtransGrid()

        xlExporter.DataTable = agentAuthtransGridview.DataSource()

        xlExporter.FileName = "Agent Charges"

        xlExporter.Export()

    End Sub
    
    Protected Sub downloadCommissionsBtn_ServerClick(sender As Object, e As System.EventArgs) Handles downloadCommissionsBtn.ServerClick
        
        Dim xlExporter As New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        agentCommissionsGV.DataBind

        xlExporter.DataTable = 
            CType(agentCommissionsDataSource.Select(DataSourceSelectArguments.Empty), DataView).ToTable

        xlExporter.FileName = salesRepDropdown.SelectedItem.Text & " Commissions"

        xlExporter.Export()

    End Sub

    Protected Sub agentAuthtransGridview_DataBound(sender As Object, e As System.EventArgs) Handles agentAuthtransGridview.DataBound
        If agentAuthtransGridview.Rows.Count = 0 Then
            gvHasRowsDiv.Visible = False
        Else
            gvHasRowsDiv.Visible = True
        End If
    End Sub

    Protected Sub agentCommissionsGV_DataBound(sender As Object, e As System.EventArgs) Handles agentCommissionsGV.DataBound
        If agentCommissionsGV.Rows.Count = 0 Then
            commissionsBtnDiv.Visible = False
        Else
            commissionsBtnDiv.Visible = True
        End If
    End Sub

    Protected Sub agentAuthtransGridview_PageIndexChanging(sender As Object, e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles agentAuthtransGridview.PageIndexChanging
        agentAuthtransGridview.PageIndex = e.NewPageIndex
        BindAuthtransGrid()
    End Sub

    Protected Sub salesRepDropdown_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles salesRepDropdown.SelectedIndexChanged
        agentCommissionsGV.PageIndex = 0
        agentAuthtransGridview.PageIndex = 0
        BindAuthtransGrid()
        BindAgentCreditsGV()
        PopulateAgentPaymentInfo()
        SetCreditLimitLabel()
    End Sub

    Protected Sub agentPaymentBtn_Click(sender As Object, e As System.EventArgs) Handles agentPaymentBtn.Click

        ' First save CC information.
        Dim cmd As New SqlCommand
        Dim sql As New StringBuilder
        Dim agent As String = salesRepDropdown.SelectedValue
        Dim ccNum As String = creditCardNumber.Text.Trim()
        Dim ccExpDate As String = 
            "20" & creditCardExpirationYear.SelectedValue & "-" & creditCardExpirationMonth.SelectedValue

        sql.Append("UPDATE AgentsCCInfo SET CCLastFour = @CCLastFour, CCExpirationDate = @CCExpDate ")
        sql.Append("WHERE UserId = @UserId; ")

        cmd.Connection = New SqlConnection(_ppcConStr)
        cmd.Connection.Open()

        cmd.Parameters.Add("@CCLastFour", SqlDbType.VarChar).Value = ccNum.Substring(ccNum.Length - 4)
        cmd.Parameters.Add("@CCExpDate", SqlDbType.VarChar).Value = ccExpDate
        cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = New Guid(Membership.GetUser(agent).ProviderUserKey.ToString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.ExecuteNonQuery

        cmd.Connection.Close()

        ' Now charge credit card
        Dim creditAgent As New CreditAgentAccountCharge

        creditAgent.CCNumber = creditCardNumber.Text
        creditAgent.CCExpiration = ccExpDate
        creditAgent.CCCode = creditCardCode.Text

        creditAgent.MiscellaneousCost = Decimal.Parse( agentPayAmount.Text, NumberStyles.Any )

        creditAgent.User = Membership.GetUser.UserName
        creditAgent.Agent = agent

        creditAgent.RunCharge()
        
        If creditAgent.hasCharged Then
            ReloadPage
        Else
            agntPayError.Style.Item("color") = "brown"
            agntPayError.InnerText = creditAgent.AuthMessage
        End If

    End Sub

    Protected Sub adminAgentPayBtn_Click(sender As Object, e As System.EventArgs) Handles adminAgentPayBtn.Click
        
        Dim aF As String = adminAgentPayFld.Text.Trim
        aF = aF.Replace("$", "")
        aF = aF.Replace(",", "")
        Dim amnt As Decimal = 0 - Decimal.Parse( aF )

        Dim con As SqlConnection = New SqlConnection(_ppcConStr)
        con.Open()

        Dim sql As StringBuilder = New StringBuilder
        ' Note: This only works if trans_type is 2 because this is taken into account when calculating
        ' agent credit used.
        sql.Append("INSERT INTO [authtrans] ([trans_type], [paydate], [total], [user], [agent], [charged]) ")
        sql.Append("VALUES (2, GETDATE(), @Amount, @User, @Agent, 1) ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), con)

        cmd.Parameters.Add("@Amount", SqlDbType.Money).Value = amnt
        cmd.Parameters.Add("@User", SqlDbType.VarChar).Value = Membership.GetUser.UserName
        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = salesRepDropdown.SelectedItem.Text

        cmd.ExecuteNonQuery()

        con.Close()

        adminAgentPayFld.Text = ""

        ReloadPage

    End Sub

    Protected Sub triggerUpdateBtn_Click(sender As Object, e As System.EventArgs) Handles triggerUpdateBtn.Click
        
        Dim cmd As New SqlCommand
        Dim sql As New StringBuilder
        Dim agent As String = salesRepDropdown.SelectedValue

        sql.Append("UPDATE AgentsCCInfo SET BillingFName = @BillFName, BillingLName = @BillLName, ")
        sql.Append("BillingAddress = @BillAddress, BillingCity = @BillCity, BillingState = @BillState, ")
        sql.Append("BillingZip = @BillZip, BillingPhone = @BillPhone, BillingEmail = @BillEmail ")
        sql.Append("WHERE UserId = @UserId; ")

        cmd.Connection = New SqlConnection(_ppcConStr)
        cmd.Connection.Open()

        cmd.Parameters.Add("@BillFName", SqlDbType.VarChar).Value = billFnameFld.Text.Trim
        cmd.Parameters.Add("@BillLName", SqlDbType.VarChar).Value = billLnameFld.Text.Trim
        cmd.Parameters.Add("@BillAddress", SqlDbType.VarChar).Value = billAddressFld.Text.Trim
        cmd.Parameters.Add("@BillCity", SqlDbType.VarChar).Value = billCityFld.Text.Trim
        cmd.Parameters.Add("@BillState", SqlDbType.VarChar).Value = billStateFld.Text.Trim
        cmd.Parameters.Add("@BillZip", SqlDbType.VarChar).Value = billZipFld.Text.Trim
        cmd.Parameters.Add("@BillPhone", SqlDbType.VarChar).Value = billPhoneFld.Text.Trim
        cmd.Parameters.Add("@BillEmail", SqlDbType.VarChar).Value = billEmailFld.Text.Trim
        cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = New Guid(Membership.GetUser(agent).ProviderUserKey.ToString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.ExecuteNonQuery

        cmd.Connection.Close()

        hasBillingInfo.Value = "true"

    End Sub


    Private Sub ReloadPage()
        If userLevel = 1 Then
            Dim str As String = "tab=" & creditAgentTabs.ActiveTabIndex.ToString
            Session("_RedirectSaveState") = str
        End If
        Response.Redirect( "~/Agents.aspx" )
    End Sub

End Class