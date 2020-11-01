Imports System.Globalization

' Note: This code was moved from other files and combined.
' Please take note of the comments.

Partial Class Admin
    Inherits System.Web.UI.Page
    Implements System.Web.UI.ICallbackEventHandler

    Dim _callBackResult As String

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        If Session.Item("UserLevel") Is Nothing Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        If Session.Item("UserLevel") <> 1 Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

        ' Calls for manage account
        BindAccountsGridview()
        editAccoutsErrorMsg.InnerText = ""


        ' Calls for manage ESN
        BindEsnGrid()
        uploadStatusLbl.Text        = ""
        manageEsnErrorMsg.InnerText = ""
     

        ' Calls for manage Pins
        uploadPinsErrorMsg.Text = ""


        ' Calls for manage Users
        BindUserNameGrid()
        SetEditingUserId
        manageUsersErrorMsgDiv.InnerText = ""

        ' Calls for manage Commission Plans


        ' Calls for Verizon MDN
        ' BindVerMdnGv()
    

        ' Calls for manage account
        'BindESNOrdersGV()

        If Not IsPostBack Then
            PopulateUserLevelDropdown()
            PopulateComPlanDropdown()
            BindComPlanRepeater()
            LoadVZPPBalance()
        End If

        ' To prevent resubmits we redirect, however we lose ViewState so we maintain
        ' some states in the session as a serialized string
        If Not IsNothing(Session("_RedirectSaveState")) Then
            Dim parts() As String = CType(Session("_RedirectSaveState"), String).Split("&")
            adminTabContainer.ActiveTabIndex = CType(CType(parts(0), String).Split("=")(1), Integer)
            Session.Remove("_RedirectSaveState")
        End If


    End Sub

 
    ' Manage account functions
    Private Sub BindAccountsGridview()

        Dim sql As New StringBuilder

        sql.Append("SELECT [id], [AccountName], [Password], ")
        sql.Append("[Active] = CASE [Active] WHEN 1 Then 'Yes' When 0 Then 'No' Else '' End ")
        sql.Append("FROM [Accounts] ")

        If searchNameFld.Text.Length > 0 Then
            sql.Append("WHERE [AccountName] LIKE '%' + @AccountName + '%' ")
        End If

        searchAccountsDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        searchAccountsDataSource.SelectCommandType = SqlDataSourceCommandType.Text

        searchAccountsDataSource.SelectCommand = sql.ToString()
        searchAccountsDataSource.CancelSelectOnNullParameter = False

        accountsGridView.DataBind()

    End Sub

    Protected Sub accountsGridView_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles accountsGridView.RowDataBound
        If e.Row.RowType = DataControlRowType.DataRow Then

            Dim row As DataRow = DirectCast(e.Row.DataItem, DataRowView).Row
            Dim accountId = row.Item("id")

            e.Row.Attributes.Add("onclick", "GetAccountInfo('" & accountId & "');")

        End If
    End Sub

    Private Sub GetAccountDetailsForClient(ByVal accountId As Integer)
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As New StringBuilder

        sql.Append("SELECT * ")
        sql.Append("FROM [Accounts] ")
        sql.Append("WHERE id = @AccountId ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), con)

        cmd.Parameters.Add("AccountId", SqlDbType.Int).Value = accountId

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            Dim ds As New DataSet
            ds.Load(reader, LoadOption.PreserveChanges, "Accounts")
            _callBackResult = ds.GetXml()
        Else
            _callBackResult = "none"
        End If

        reader.Close()
        con.Close()
    End Sub

    Protected Sub createAccountBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles createAccountBtn.Click
        CreateAccount()
    End Sub

    Protected Sub updateAccountBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles updateAccountBtn.Click
        UpdateAccount()
    End Sub

    Private Sub CreateAccount()
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("SELECT [AccountName] FROM [Accounts] WHERE [AccountName] like @AccountName ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), con)

        ' Client side code makes sure that the length is greater than zero
        cmd.Parameters.Add("AccountName", SqlDbType.VarChar).Value = editAccountNameFld.Text

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            editAccoutsErrorMsg.InnerText = "Account name '" & editAccountNameFld.Text & "' is already taken. Please choose another name."
            Exit Sub
        End If

        sql.Clear()
        reader.Close()

        sql.Append("INSERT INTO [Accounts] ([AccountName], [Password], [Active]) ")
        sql.Append("VALUES (@AccountName, @Password, @Active) ")
        sql.Append("SELECT SCOPE_IDENTITY() AS account_id; ")

        cmd = New SqlCommand(sql.ToString(), con)

        ' Client side code makes sure that the length is greater than zero
        cmd.Parameters.Add("AccountName", SqlDbType.VarChar).Value = editAccountNameFld.Text
        cmd.Parameters.Add("Password", SqlDbType.VarChar).Value = editPasswordFld.Text
        cmd.Parameters.Add("Active", SqlDbType.Bit).Value = accountActiveChk.Checked

        reader = cmd.ExecuteReader()

        hiddenAccountName.Value = editAccountNameFld.Text
        If reader.HasRows Then
            reader.Read()
            hiddenAccountId.Value = reader.Item("account_id")
        End If
        BindAccountsGridview()

        con.Close()

    End Sub

    Private Sub UpdateAccount()
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As StringBuilder
        Dim cmd As SqlCommand
        Dim isAccountNameUpdated As Boolean

        If hiddenAccountName.Value.Length > 0 Then
            If editAccountNameFld.Text.ToLower() <> hiddenAccountName.Value.ToLower() Then
                isAccountNameUpdated = True
            Else
                isAccountNameUpdated = False
            End If
        End If

        If isAccountNameUpdated Then
            ' Make sure that the acoount name doesn't exist.
            sql = New StringBuilder
            sql.Append("SELECT [AccountName] FROM [Accounts] WHERE [AccountName] like @AccountName ")

            cmd = New SqlCommand(sql.ToString(), con)

            ' Client side code makes sure that the length is greater than zero
            cmd.Parameters.Add("AccountName", SqlDbType.VarChar).Value = editAccountNameFld.Text

            Dim reader As SqlDataReader = cmd.ExecuteReader

            If reader.HasRows Then
                editAccoutsErrorMsg.InnerText = "Account name '" & editAccountNameFld.Text & "' is already taken. Please choose another name."
                con.Close()
                Exit Sub
            End If

            reader.Close()

        End If


        sql = New StringBuilder

        sql.Append("UPDATE [Accounts] SET [AccountName] = @AccountName, ")
        sql.Append("[Password] = @Password, ")
        sql.Append("[Active] = @Active ")
        sql.Append("WHERE id = @AccountId ")

        cmd = New SqlCommand(sql.ToString(), con)

        ' Client side code makes sure that the length is greater than zero
        cmd.Parameters.Add("AccountName", SqlDbType.VarChar).Value = editAccountNameFld.Text
        cmd.Parameters.Add("Password", SqlDbType.VarChar).Value = editPasswordFld.Text
        cmd.Parameters.Add("Active", SqlDbType.Bit).Value = accountActiveChk.Checked
        cmd.Parameters.Add("AccountId", SqlDbType.Int).Value = If(hiddenAccountId.Value.Length > 0, hiddenAccountId.Value, "0")

        cmd.ExecuteNonQuery()

        con.Close()

        BindAccountsGridview()

    End Sub

    Private Sub ClearFieldsMsgs()
        editAccountNameFld.Text = ""
        accountNameMsgLbl.Text = ""
        editPasswordFld.Text = ""
        accountPasswordMsgLbl.Text = ""
        accountActiveChk.Checked = True
    End Sub


    ' Manage ESN functions
    Private Sub BindEsnGrid()

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("SELECT * FROM [SerialESN]  ")

        ' See if there is any filtering
        Dim isFilteredAlready As Boolean = False

        If serialNumSearchFld.Text.Length > 0 Then
            sql.Append("WHERE [Serial#] LIKE '%' + @SerialNum + '%' ")
            isFilteredAlready = True
        End If

        If esnSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [ESN] LIKE '%' + @ESN + '%' ")
            Else
                sql.Append("WHERE [ESN] LIKE '%' + @ESN + '%' ")
                isFilteredAlready = True
            End If
        End If

        If intlSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND cast( cast([International] as bigINT) as varchar(12)) LIKE '%' + @Intl + '%' ")
            Else
                sql.Append("WHERE cast( cast([International] as bigINT) as varchar(12)) LIKE '%' + @Intl + '%' ")
                isFilteredAlready = True
            End If
        End If

        If cusPinSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND cast( cast([CustomerPin] as bigINT) as varchar(12)) LIKE '%' + @CustPin + '%' ")
            Else
                sql.Append("WHERE cast( cast([CustomerPin] as bigINT) as varchar(12)) LIKE '%' + @CustPin + '%' ")
                isFilteredAlready = True
            End If
        End If

        If dateCreatedSearchFld.Text.Length > 0 Then

            Dim date1 As Date

            If dateCreatedSearchFld.Text.Length >= 8 Then
                Try
                    date1 = dateCreatedSearchFld.Text
                    esnDataSource.SelectParameters.Item("DateCreated").DefaultValue =
                        date1.ToString("d", DateTimeFormatInfo.InvariantInfo)
                Catch ex As InvalidCastException
                    esnDataSource.SelectParameters.Item("DateCreated").DefaultValue = dateCreatedSearchFld.Text
                End Try
            Else
                esnDataSource.SelectParameters.Item("DateCreated").DefaultValue = dateCreatedSearchFld.Text
            End If

            If isFilteredAlready Then
                sql.Append("AND CONVERT(VARCHAR(20), [InsertedDate], 101) LIKE '%' + @DateCreated + '%' ")
            Else
                sql.Append("WHERE CONVERT(VARCHAR(20), [InsertedDate], 101) LIKE '%' + @DateCreated + '%' ")
                isFilteredAlready = True
            End If

        End If

        sql.Append("ORDER BY [InsertedDate] DESC ")

        esnDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        esnDataSource.SelectCommandType = SqlDataSourceCommandType.Text

        esnDataSource.SelectCommand = sql.ToString()
        esnDataSource.CancelSelectOnNullParameter = False

        esnGridView.DataBind()
        If esnGridView.Rows.Count <> 0 Then
            downloadReportBtn.Visible = True
        Else
            downloadReportBtn.Visible = False
        End If
    End Sub

    Protected Sub esnGridView_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles esnGridView.RowDataBound
        If e.Row.RowType = DataControlRowType.DataRow Then

            Dim row As DataRow = DirectCast(e.Row.DataItem, DataRowView).Row
            Dim serial = row.Item("Serial#")
            Dim esn = row.Item("ESN")
            Dim international = row.Item("International")
            Dim customerPin = row.Item("CustomerPin")

            Dim script As StringBuilder = New StringBuilder()
            script.Append("PopulateEsnInfo( ")
            script.Append("'" & serial & "', ")
            script.Append("'" & esn & "', ")
            script.Append("'" & international & "', ")
            script.Append("'" & customerPin & "' ")
            script.Append("); ")

            e.Row.Attributes.Add("onclick", script.ToString)

        End If
    End Sub

    Protected Sub createEsnBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles createEsnBtn.Click

        If Not isSerialUnique(manageSerialFld.Text) Then
            manageEsnErrorMsg.InnerText = "Serial number already exists."
            manageSerialFld.Text = ""
            Exit Sub
        End If

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.Connection.Open()
        Dim sql As String = "INSERT INTO SerialESN ([Serial#], [ESN], [International], [CustomerPin], [InsertedDate]) "
        sql &= "VALUES (@Serial, @ESN, @Intl, @CusPin, getdate()) "
        cmd.CommandText = sql

        cmd.Parameters.Add("Serial", SqlDbType.NVarChar).Value = manageSerialFld.Text
        cmd.Parameters.Add("ESN", SqlDbType.NVarChar).Value = manageEsnFld.Text
        cmd.Parameters.Add("Intl", SqlDbType.Float).Value = manageIntlFld.Text
        cmd.Parameters.Add("CusPin", SqlDbType.Float).Value = manageCusPinFld.Text

        Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

        If rowsAffected = 0 Then
            manageEsnErrorMsg.InnerText = "ESN create failed."
        Else
            ' Reset the hidden serial to the new serial number.
            originalSerial.Value = manageSerialFld.Text
            manageEsnErrorMsg.InnerText = "ESN successfully created."
        End If

        cmd.Connection.Close()

        BindEsnGrid()

    End Sub

    Protected Sub deleteEsnBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles deleteEsnBtn.Click
        Dim serial As String = manageSerialFld.Text
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandText = "DELETE FROM SerialESN WHERE [Serial#] = @Serial "
        cmd.Parameters.Add("Serial", SqlDbType.NVarChar).Value = serial

        cmd.Connection.Open()

        Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
        If rowsAffected = 0 Then
            manageEsnErrorMsg.InnerText = "Serial number not found."
        ElseIf rowsAffected = 1 Then
            manageSerialFld.Text = ""
            manageEsnFld.Text = ""
            manageIntlFld.Text = ""
            manageCusPinFld.Text = ""
        Else
            manageEsnErrorMsg.InnerText = "Duplicate serial numbers found."
        End If

        cmd.Connection.Close()

        BindEsnGrid()

    End Sub

    Protected Sub updateEsnBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles updateEsnBtn.Click

        If manageSerialFld.Text <> originalSerial.Value Then
            If Not isSerialUnique(manageSerialFld.Text) Then
                manageEsnErrorMsg.InnerText = "Serial number already exists."
                Exit Sub
            End If
        End If

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.Connection.Open()
        Dim sql As String = "UPDATE SerialESN SET [Serial#] = @Serial, [ESN] = @ESN, [International] = @Intl, "
        sql &= "[CustomerPin] = @CusPin WHERE [Serial#] = @OldSerial "
        cmd.CommandText = sql

        cmd.Parameters.Add("Serial", SqlDbType.NVarChar).Value = manageSerialFld.Text
        cmd.Parameters.Add("ESN", SqlDbType.NVarChar).Value = manageEsnFld.Text
        cmd.Parameters.Add("Intl", SqlDbType.Float).Value = manageIntlFld.Text
        cmd.Parameters.Add("CusPin", SqlDbType.Float).Value = manageCusPinFld.Text
        cmd.Parameters.Add("OldSerial", SqlDbType.NVarChar).Value = originalSerial.Value

        Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

        If rowsAffected = 1 Then
            ' Reset the hidden serial to the new serial number.
            originalSerial.Value = manageSerialFld.Text
            manageEsnErrorMsg.InnerText = "Successfully updated."
        ElseIf rowsAffected = 0 Then
            manageEsnErrorMsg.InnerText = "Update failed."
        Else
            manageEsnErrorMsg.InnerText = "Duplicate serial numbers found."
        End If

        cmd.Connection.Close()

        BindEsnGrid()

    End Sub

    Protected Sub uploadEsnFile_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        Dim con As SqlConnection = Nothing
        Dim cmd As SqlCommand = Nothing

        Dim path As String = Server.MapPath("~/uploads/") & "UploadedESN.xlsx"

        Try

            esnUploadControl.SaveAs(path)

            con = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            con.Open()

            'take data from file to DB table
            cmd = New SqlCommand("ImportSerialEsn", con)
            cmd.CommandType = CommandType.StoredProcedure

            cmd.ExecuteNonQuery()

            uploadStatusLbl.Text = "ESN imported successfully."

            BindEsnGrid()

        Catch ex As Exception
            uploadStatusLbl.Text = ex.Message + ":Upload failed. Please check the data in the spreadsheet."
        Finally
            con.Close()
        End Try

    End Sub

    Private Function isSerialUnique(ByVal serial As String) As Boolean

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandText = "SELECT [Serial#] FROM SerialEsn WHERE [Serial#] = @Serial "
        cmd.Connection.Open()
        cmd.Parameters.Add("Serial", SqlDbType.NVarChar).Value = serial

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            cmd.Connection.Close()
            Return False
        Else
            cmd.Connection.Close()
            Return True
        End If

    End Function


    ' Manage pins functions
    Protected Sub importPinsBtn_Click(sender As Object, e As System.EventArgs) Handles importPinsBtn.Click
        TempPinsImport()
    End Sub

    Private Sub TempPinsImport()
        Dim cmd As SqlCommand = New SqlCommand
        Dim sql As StringBuilder = New StringBuilder

        Try
            uploadPinsControl.SaveAs(Server.MapPath("~/uploads/") & "UploadedPins.csv")

            cmd.Connection =
                New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            cmd.Connection.Open()

            sql.Append("DELETE PinsUpload; ")
            sql.Append("INSERT INTO PinsUpload ")
            sql.Append("SELECT * FROM ")
            sql.Append("OpenRowSet('Microsoft.ace.OLEDB.12.0', 'Text;Database=" & Server.MapPath("~/uploads/") & "; HDR=YES;', 'SELECT * from  [UploadedPins.csv] '); ")

            'take data from file to DB table
            cmd.CommandType = CommandType.Text
            cmd.CommandText = sql.ToString
            cmd.ExecuteNonQuery()

            ' Display temp pins for validation.
            BindSeePinsGrid()

            uploadPinsErrorMsg.Text = "Please look over pins and select continue or cancel."

        Catch ex As Exception
            uploadPinsErrorMsg.Text = "Upload failed (Error = " & ex.Message.ToString() + "). Please check the data in the spreadsheet."
        Finally
            cmd.Connection.Close()
        End Try

    End Sub

    Private Sub BindSeePinsGrid()

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
                New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT * FROM PinsUpload;"

        cmd.Connection.Open()

        Dim ds As New DataSet
        ds.Load(cmd.ExecuteReader(), LoadOption.PreserveChanges, "PinsUpload")
        seePinsGridview.DataSource = ds
        seePinsGridview.DataBind()

        cmd.Connection.Close()

    End Sub

    Private Sub ClearSeePinsGrid()
        seePinsGridview.DataSource = Nothing
        seePinsGridview.DataBind()
    End Sub

    Protected Sub seePinsGridview_DataBound(sender As Object, e As System.EventArgs) Handles seePinsGridview.DataBound
        If seePinsGridview.Rows.Count = 0 Then
            uploadPinsBtnDiv.Visible = False
        Else
            uploadPinsBtnDiv.Visible = True
        End If
    End Sub

    Protected Sub seePinsGridview_PageIndexChanging(sender As Object, e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles seePinsGridview.PageIndexChanging
        seePinsGridview.PageIndex = e.NewPageIndex
        BindSeePinsGrid()
    End Sub

    Protected Sub contPinsUpload_Click(sender As Object, e As System.EventArgs) Handles contPinsUpload.Click
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
                New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.Text
        'If filterPinCarrier.SelectedValue.ToLower = "page plus" Then
        cmd.CommandText = "INSERT INTO [PINS] ([Pin], [PinType], Vendor, DatePurchased, Control, PurchaseStatus ) SELECT [pins], case when [Pin Type] is null then @PinType else [Pin Type] end, 'Saycor',  GETDATE(), Control,'Success' FROM [PinsUpload]; "
        'ElseIf filterPinCarrier.SelectedValue.ToLower = "all talk" Then
        'cmd.CommandText = "INSERT INTO [ATC_PINS] ([Pin], [PinType], Control ) SELECT [pins], case when [Pin Type] is null then @PinType else [Pin Type] end, Control FROM [PinsUpload]; "
        'End If
        cmd.Parameters.Add("PinType", SqlDbType.VarChar).Value = pinTypeDropdown.SelectedItem.Text

        cmd.Connection.Open()

        Dim rowAffected As Integer = cmd.ExecuteNonQuery()

        If rowAffected > 0 Then
            uploadPinsErrorMsg.Text = "Pins successfully imported."
            ClearSeePinsGrid()
        Else
            uploadPinsErrorMsg.Text = "Import failed. Please check the data in the spreadsheet."
        End If

        cmd.Connection.Close()

    End Sub


    Protected Sub cancelPinsUpload_Click(sender As Object, e As System.EventArgs) Handles cancelPinsUpload.Click
        ClearSeePinsGrid()
        pinTypeDropdown.SelectedIndex = 0
    End Sub


    ' Manage Users functions
    Private Sub BindUserNameGrid()

        Dim sql As New StringBuilder

        sql.Append("SELECT [UserName], [UserPersonalName] AS FullName FROM [ASPNETDB].[dbo].[aspnet_Users] ")

        Dim isFilteredAlready As Boolean = False

        If userNameSearchFld.Text.Length > 0 Then
            sql.Append("WHERE [UserPersonalName] LIKE '%' + @UserName + '%' ")
            isFilteredAlready = True
        End If

        If userIdSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [UserName] LIKE '%' + @UserID + '%' ")
            Else
                sql.Append("WHERE [UserName] LIKE '%' + @UserID + '%' ")
            End If
            isFilteredAlready = True
        End If

        sql.Append("ORDER BY UserName; ")

        searchUsersDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        searchUsersDataSource.SelectCommandType = SqlDataSourceCommandType.Text

        searchUsersDataSource.SelectCommand = sql.ToString()
        searchUsersDataSource.CancelSelectOnNullParameter = False

        usersGridView.DataBind()

    End Sub

    Private Sub PopulateUserLevelDropdown()

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As New StringBuilder

        sql.Append("SELECT DISTINCT level INTO #mytemp from [ASPNETDB].[dbo].[aspnet_Users] WHERE level IS NOT NULL; ")
        sql.Append("SELECT level, (CASE WHEN level = 1 THEN 'Administrator' WHEN level = 2 THEN 'Office' ")
        sql.Append("ELSE 'Agent' END) as userLevel ")
        sql.Append("FROM #mytemp; ")
        sql.Append("drop table #mytemp;")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString(), con)
        Dim reader As SqlDataReader = cmd.ExecuteReader()

        userLevelDropdown.DataSource = reader
        userLevelDropdown.DataValueField = "level"
        userLevelDropdown.DataTextField = "userLevel"
        userLevelDropdown.DataBind()

        userLevelDropdown.SelectedIndex = userLevelDropdown.Items.IndexOf(userLevelDropdown.Items.FindByText("Agent"))

        reader.Close()
        con.Close()

    End Sub

    Private Sub PopulateComPlanDropdown()

        Dim cmd As New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT DISTINCT CommissionPlan FROM CommissionPlans ORDER BY CommissionPlan ASC; "

        cmd.Connection.Open()

        commissionPlansDrop.DataSource = cmd.ExecuteReader
        commissionPlansDrop.DataTextField = "CommissionPlan"
        commissionPlansDrop.DataBind()

        commissionPlansDrop.Items.Insert(0, New ListItem("No Commission"))

        cmd.Connection.Close()

    End Sub

    Protected Sub usersGridView_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles usersGridView.RowDataBound
        If e.Row.RowType = DataControlRowType.DataRow Then

            Dim row As DataRow = DirectCast(e.Row.DataItem, DataRowView).Row
            Dim userId = row.Item("UserName")
            e.Row.Attributes.Add("onclick", "GetUserInfo('" & userId & "');")

        End If
    End Sub

    Protected Sub createUserBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles createUserBtn.Click

        Dim newUser As MembershipUser

        Dim personalName As String = userNameEditFld.Text
        Dim userId As String = userIdEditFld.Text
        Dim userLevel As Integer = userLevelDropdown.SelectedItem.Value
        Dim password As String = passwordEditFld.Text
        Dim email As String = emailEditFld.Text
        Dim creditLimit As Decimal =
            Decimal.Parse(If(creditLimitField.Text.Length > 0, creditLimitField.Text, "0"), NumberStyles.Any)
        Dim monitorAgnt As Boolean = monitorAgent.Checked

        Try
            newUser = Membership.CreateUser(userId, password)

            newUser.Email = email
            Membership.UpdateUser(newUser)

        Catch cue As MembershipCreateUserException
            manageUsersErrorMsgDiv.InnerText = GetErrorMessage(cue.StatusCode, userId)
            userIdEditFld.Text = ""
            Exit Sub
        Catch httpe As HttpException
            manageUsersErrorMsgDiv.InnerText = httpe.Message
            Exit Sub
        End Try

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As New StringBuilder
        sql.Append("UPDATE [ASPNETDB].[dbo].[aspnet_Users] SET [UserPersonalName] = @FullName, ")
        sql.Append("[Level] = @UserLevel, [CreditLimit] = @CreditLimit, ")
        sql.Append("[UserMonitor] = @UserMonitor, [CommissionPlan] = @CommissionPlan ")
        sql.Append("WHERE [UserId] = @UserId; ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)

        cmd.Parameters.Add("FullName", SqlDbType.VarChar).Value = If(personalName.Length > 0, personalName, "")
        cmd.Parameters.Add("UserLevel", SqlDbType.SmallInt).Value = userLevel
        cmd.Parameters.Add("UserMonitor", SqlDbType.Bit).Value = monitorAgnt
        cmd.Parameters.Add("CreditLimit", SqlDbType.Money).Value = creditLimit
        cmd.Parameters.Add("CommissionPlan", SqlDbType.VarChar).Value = commissionPlansDrop.SelectedItem.Text
        cmd.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = New Guid(newUser.ProviderUserKey.ToString())

        Dim rowsAffected = cmd.ExecuteNonQuery()
        If rowsAffected > 0 Then

            cmd.Connection.Close()

            cmd.Connection.Open()
            cmd.CommandType = CommandType.Text
            cmd.CommandText = "INSERT INTO AgentsCCInfo ( UserId ) VALUES ( @UserId );  "
            cmd.ExecuteNonQuery()

            manageUsersErrorMsgDiv.InnerText = "User successfully created."
            BindUserNameGrid()
            SetEditableFields()

            ' Set hidden field so that the update functionality works.
            hiddenUpdateUserId.Value = userId
            ToggleApplyCommissions()
        End If

        con.Close()

    End Sub

    Protected Sub updateUserBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles updateUserBtn.Click

        Dim personalName As String = userNameEditFld.Text
        Dim userId As String = hiddenUpdateUserId.Value
        Dim userLevel As Integer = userLevelDropdown.SelectedItem.Value
        Dim monitorAgnt As Boolean = monitorAgent.Checked
        Dim email As String = emailEditFld.Text
        Dim creditLimit As Decimal =
            Decimal.Parse(If(creditLimitField.Text.Length > 0, creditLimitField.Text, "0"), NumberStyles.Any)

        Dim user As MembershipUser = Membership.GetUser(userId)

        If user Is Nothing Then
            manageUsersErrorMsgDiv.InnerText = "Failed to update user. Please check the user id."
            Exit Sub
        End If

        user.Email = email
        Membership.UpdateUser(user)

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()


        Dim sql As New StringBuilder
        sql.Append("UPDATE [ASPNETDB].[dbo].[aspnet_Users] SET [UserPersonalName] = @FullName, ")
        sql.Append("[Level] = @UserLevel, [CreditLimit] = @CreditLimit, ")
        sql.Append("[UserMonitor] = @UserMonitor, [CommissionPlan] = @CommissionPlan ")
        sql.Append("WHERE [UserId] = @UserId; ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)

        cmd.Parameters.Add("FullName", SqlDbType.VarChar).Value = If(personalName.Length > 0, personalName, "")
        cmd.Parameters.Add("UserLevel", SqlDbType.SmallInt).Value = userLevel
        cmd.Parameters.Add("UserMonitor", SqlDbType.Bit).Value = monitorAgnt
        cmd.Parameters.Add("CreditLimit", SqlDbType.Money).Value = creditLimit
        cmd.Parameters.Add("CommissionPlan", SqlDbType.VarChar).Value = commissionPlansDrop.SelectedItem.Text
        cmd.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = New Guid(user.ProviderUserKey.ToString())

        Dim rowsAffected = cmd.ExecuteNonQuery()

        If rowsAffected > 0 Then
            manageUsersErrorMsgDiv.InnerText = "User successfully updated."
            BindUserNameGrid()
            SetEditableFields()
            ToggleApplyCommissions()
        End If

        con.Close()

    End Sub

    Protected Sub deleteUserBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles deleteUserBtn.Click

        Dim deleted As Boolean
        Dim cmd As New SqlCommand
        Dim aspnetUserId As Guid = New Guid(Membership.GetUser(hiddenUpdateUserId.Value).ProviderUserKey.ToString)

        Try
            deleted = Membership.DeleteUser(hiddenUpdateUserId.Value)
        Catch Ex As ArgumentException
            deleted = False
        End Try

        If deleted Then
            cmd.Connection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            cmd.Connection.Open()
            cmd.CommandType = CommandType.Text
            cmd.CommandText = "DELETE FROM AgentsCCInfo WHERE UserId = @UserId; "
            cmd.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = aspnetUserId
            cmd.ExecuteNonQuery()
            cmd.Connection.Close()

            ResetUserFields()
        Else
            manageUsersErrorMsgDiv.InnerText = "Failed to delete user. Please check the user id."
        End If

        BindUserNameGrid()

    End Sub

    Protected Sub unlockUserBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles unlockUserBtn.Click

        Dim userId As String = hiddenUpdateUserId.Value

        Dim user As MembershipUser = Membership.GetUser(userId)

        If user Is Nothing Then
            manageUsersErrorMsgDiv.InnerText = "Failed to unlock user. Please check the user id."
            Exit Sub
        End If

        If user.IsLockedOut Then
            If user.UnlockUser() Then
                manageUsersErrorMsgDiv.InnerText = "User successfully unlocked."
            Else
                manageUsersErrorMsgDiv.InnerText = "Failed to unlock user. Please check the user id."
            End If
        Else
            manageUsersErrorMsgDiv.InnerText = "User isn't locked out."
        End If

        ' The visible user id field id disabled and loses its value,
        ' when we update a user we want the value to remain.
        userIdEditFld.Text = userId

        SetEditableFields()

    End Sub

    Protected Sub triggerPwResetBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles triggerPwResetBtn.Click
        Dim userId As String = hiddenUpdateUserId.Value

        Dim user As MembershipUser = Membership.GetUser(userId)
        Dim newPw As String = resetPwField.Text

        If user Is Nothing Then
            resetPwErrorMsg.InnerText = "There is a problem with the user name."
            resetPassowrdModalPopup.Show()
            Exit Sub
        End If

        user.ChangePassword(user.ResetPassword(), newPw)

    End Sub

    Private Function GetErrorMessage(ByVal status As MembershipCreateStatus, ByVal userId As String) As String

        Select Case status
            Case MembershipCreateStatus.DuplicateUserName
                Return "User '" & userId & "' already exists. Please enter a different user id."

            Case MembershipCreateStatus.DuplicateEmail
                Return "A user id for that e-mail address already exists. Please enter a different e-mail address."

            Case MembershipCreateStatus.InvalidPassword
                Return "The password provided is invalid. Please enter a valid password value."

            Case MembershipCreateStatus.InvalidEmail
                Return "The e-mail address provided is invalid. Please check the value and try again."

                'Case MembershipCreateStatus.InvalidAnswer
                '    Return "The password retrieval answer provided is invalid. Please check the value and try again."

                'Case MembershipCreateStatus.InvalidQuestion
                '    Return "The password retrieval question provided is invalid. Please check the value and try again."

            Case MembershipCreateStatus.InvalidUserName
                Return "The user id provided is invalid. Please check the value and try again."

            Case MembershipCreateStatus.ProviderError
                Return "The authentication provider Returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator."

            Case MembershipCreateStatus.UserRejected
                Return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator."

            Case Else
                Return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator."
        End Select

    End Function

    Private Sub ResetUserFields()
        userNameEditFld.Text = ""
        userIdEditFld.Text = ""
        hiddenUpdateUserId.Value = ""
        userLevelDropdown.SelectedIndex = userLevelDropdown.Items.IndexOf(userLevelDropdown.Items.FindByText("Agent"))
        emailEditFld.Text = ""
        monitorAgent.Checked = True
        creditLimitField.Text = ""

        userIdEditFld.Enabled = True
        passwordFieldsContainer.Style.Item("display") = "block"

    End Sub

    Private Sub SetEditingUserId()

        ' The visible user id field is disabled and loses its value
        ' on postback so we maintain it and make it non-editable.

        If hiddenUpdateUserId.Value.Length > 0 Then
            userIdEditFld.Text = hiddenUpdateUserId.Value
            userIdEditFld.Enabled = False
            passwordFieldsContainer.Style.Item("display") = "none"
            ToggleApplyCommissions()
        Else
            userIdEditFld.Enabled = True
            passwordFieldsContainer.Style.Item("display") = "block"
        End If

    End Sub

    Private Sub GetUserDetails(ByVal userId As String)

        Dim sql As New StringBuilder
        Dim json As New StringBuilder
        Dim con As SqlConnection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        sql.Append("DECLARE @comNum INT = (SELECT COUNT(*) FROM Commissions WHERE agent = @UserID); ")
        sql.Append("SELECT TOP 1 [UserPersonalName] AS FullName, [UserName], [Level], [CreditLimit], [Email], ")
        sql.Append("COALESCE( [UserMonitor], 1 ) AS [UserMonitor], @comNum AS ComNum, [CommissionPlan] ")
        sql.Append("FROM [ASPNETDB].[dbo].[aspnet_Users] LEFT JOIN [ASPNETDB].[dbo].[aspnet_Membership] ")
        sql.Append("ON [aspnet_Users].UserId = [aspnet_Membership].UserId ")
        sql.Append("WHERE [UserName] = @UserID; ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)

        cmd.Parameters.Add("UserID", SqlDbType.NVarChar).Value = userId

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            reader.Read()
            json.Append("{ ""fullName"": """ & reader.Item("FullName") & """, ")
            json.Append("""userName"": """ & reader.Item("UserName") & """,")
            json.Append("""level"": """ & reader.Item("Level") & """,")
            json.Append("""creditLimit"": """ & reader.Item("CreditLimit") & """,")
            json.Append("""email"": """ & reader.Item("Email") & """,")
            json.Append("""monitor"": """ & reader.Item("UserMonitor") & """,")
            json.Append("""comNum"": """ & reader.Item("ComNum") & """,")
            json.Append("""comPlan"": """ & reader.Item("CommissionPlan") & """ }")
            _callBackResult = json.ToString
        Else
            _callBackResult = "none"
        End If

        reader.Close()
        con.Close()

    End Sub

    Private Sub SetEditableFields()

        userIdEditFld.Enabled = False

        passwordFieldsContainer.Attributes.Add("style", "display: none;")

    End Sub

    Protected Sub retroCommissionsBtn_Click(sender As Object, e As System.EventArgs) Handles retroCommissionsBtn.Click
        Dim cmd As New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "RetroAgentCommissions"
        cmd.Parameters.Add("Agent", SqlDbType.VarChar).Value = hiddenUpdateUserId.Value

        cmd.Connection.Open()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

        ReloadPage()
    End Sub

    Private Sub ToggleApplyCommissions()
        Dim cmd As New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "DECLARE @ComNum INT = (SELECT COUNT(*) FROM Commissions WHERE agent = @UserID); "
        cmd.CommandText &= "SELECT @ComNum AS ComNum, COALESCE( CommissionPlan, 'No Commission' ) AS Com FROM "
        cmd.CommandText &= "ASPNETDB.dbo.aspnet_Users WHERE UserName = @UserID; "
        cmd.Parameters.Add("UserID", SqlDbType.VarChar).Value = hiddenUpdateUserId.Value

        cmd.Connection.Open()

        Using reader As SqlDataReader = cmd.ExecuteReader
            If reader.Read Then
                If reader.Item("Com").ToString.ToLower <> "no commission" _
                    And Integer.Parse(reader.Item("ComNum")) = 0 Then
                    retroCommissionsBtn.Style("display") = "inline"
                Else
                    retroCommissionsBtn.Style("display") = "none"
                End If
            End If
        End Using

        cmd.Connection.Close()
    End Sub



    ' Manage Commission Plans functions
    Private Sub BindComPlanRepeater()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand

        sql.Append("select CommissionItem AS Item FROM CommissionPlans where CommissionItem not like 'At%' UNION ")
        sql.Append("SELECT planname AS Item from plans where carrier != 'All Talk' UNION ")
        sql.Append("SELECT item_name AS Item FROM Items WHERE item_name != 'misc'  ")
        sql.Append("ORDER BY Item ASC; ")

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.Connection.Open()

        comPlanItemRepeater.DataSource = cmd.ExecuteReader
        comPlanItemRepeater.DataBind()

        cmd.Connection.Close()

    End Sub

    Protected Sub comPlanItemRepeater_ItemDataBound(sender As Object, e As System.Web.UI.WebControls.RepeaterItemEventArgs) Handles comPlanItemRepeater.ItemDataBound

        Dim hdnItem As HiddenField = Nothing

        For Each cont In e.Item.Controls
            If cont.GetType Is GetType(HiddenField) Then
                hdnItem = CType(cont, HiddenField)
                Exit For
            End If
        Next

        hdnItem.Value = CType(e.Item.DataItem("Item"), String)

    End Sub

    Private Sub GetComPlan(ByVal name As String)

        Dim cmd As New SqlCommand
        Dim reader As SqlDataReader
        Dim json As New StringBuilder
        Dim hasMore As Boolean = True

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT CommissionItem AS Item, CONVERT(varchar(12), CommissionAmount, 1 ) AS Amount FROM CommissionPlans WHERE CommissionPlan = @Plan; "

        cmd.Parameters.Add("@Plan", SqlDbType.VarChar).Value = name

        cmd.Connection.Open()

        reader = cmd.ExecuteReader
        If reader.HasRows Then
            json.Append("[ ")
            reader.Read()

            Do While hasMore
                json.Append("{ ""item"": """ & reader.Item("Item") & """, ")
                json.Append("""amount"": """ & reader.Item("Amount") & """ } ")
                hasMore = reader.Read
                If hasMore Then
                    json.Append(", ")
                End If
            Loop

            json.Append("] ")

        End If

        cmd.Connection.Close()

        _callBackResult = json.ToString

    End Sub

    Protected Sub createComPlanBtn_Click(sender As Object, e As System.EventArgs) Handles createComPlanBtn.Click

        Dim amntStr As String = Nothing
        Dim amntFld As TextBox = Nothing
        Dim hdnItem As HiddenField = Nothing
        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand

        For Each plan As RepeaterItem In comPlanItemRepeater.Items

            amntFld = Nothing
            hdnItem = Nothing

            For Each cont In plan.Controls
                If cont.GetType Is GetType(TextBox) Then
                    amntFld = CType(cont, TextBox)
                ElseIf cont.GetType Is GetType(HiddenField) Then
                    hdnItem = CType(cont, HiddenField)
                End If
                If Not IsNothing(amntFld) And Not IsNothing(hdnItem) Then
                    Exit For
                End If
            Next

            amntStr = If(amntFld.Text.Length > 0, amntFld.Text, "0")

            sql.Append("INSERT INTO CommissionPlans ( CommissionPlan, CommissionItem, CommissionAmount ) VALUES ")
            sql.Append("( '" & createComPlanNameFld.Text & "', '" & hdnItem.Value & "', " & amntStr & " ); ")

        Next

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.Connection.Open()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

        ' Refresh the gridview that displays the commission plans on the commission tab.
        selectComPlanGV.DataBind()

        ' Refresh the commission dropdown on the users tab
        'so that the new commissionplan can be assigned
        PopulateComPlanDropdown()

    End Sub

    Protected Sub updateComPlanBtn_Click(sender As Object, e As System.EventArgs) Handles updateComPlanBtn.Click

        Dim amntStr As String = Nothing
        Dim amntFld As TextBox = Nothing
        Dim hdnItem As HiddenField = Nothing
        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand

        For Each plan As RepeaterItem In comPlanItemRepeater.Items

            amntFld = Nothing
            hdnItem = Nothing

            For Each cont In plan.Controls
                If cont.GetType Is GetType(TextBox) Then
                    amntFld = CType(cont, TextBox)
                ElseIf cont.GetType Is GetType(HiddenField) Then
                    hdnItem = CType(cont, HiddenField)
                End If
                If Not IsNothing(amntFld) And Not IsNothing(hdnItem) Then
                    Exit For
                End If
            Next

            amntStr = If(amntFld.Text.Length > 0, amntFld.Text, "0")

            sql.Append("IF EXISTS( SELECT TOP (1) 1 FROM CommissionPlans ")
            sql.Append("WHERE CommissionPlan = '" & updateComPlanHdn.Value & "' AND CommissionItem = '" & hdnItem.Value & "' ) ")

            sql.Append("BEGIN UPDATE CommissionPlans SET CommissionAmount = " & amntStr & " WHERE ")
            sql.Append("CommissionPlan = '" & updateComPlanHdn.Value & "' AND CommissionItem = '" & hdnItem.Value & "' END ")

            sql.Append("ELSE BEGIN INSERT INTO CommissionPlans ( CommissionPlan, CommissionItem, CommissionAmount ) VALUES ")
            sql.Append("( '" & updateComPlanHdn.Value & "', '" & hdnItem.Value & "', " & amntStr & " ); END ")

        Next

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        cmd.CommandType = CommandType.Text
        cmd.CommandText = sql.ToString

        cmd.Connection.Open()

        cmd.ExecuteNonQuery()

        cmd.Connection.Close()

        selectComPlanGV.DataBind()

    End Sub



    ' Verizon MDN tab functions
    'Private Sub BindVerMdnGv()

    '    verMdnSqlDS.SelectCommandType = SqlDataSourceCommandType.Text
    '    verMdnSqlDS.SelectCommand = "SELECT top 50 * FROM VERMDN; "

    '    verMdnGV.DataBind()

    'End Sub

    'ESN Orders tab functions
    'Private Sub BindESNOrdersGV()

    '    esnOrdersDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
    '    esnOrdersDataSource.SelectCommandType = SqlDataSourceCommandType.StoredProcedure

    '    esnOrdersDataSource.SelectCommand = "OrderDisplay"
    '    esnOrdersDataSource.CancelSelectOnNullParameter = False

    '    esnOrdersGV.DataBind()
    'End Sub
    'Private Sub btnDownload_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnDownload.Click
    '    Response.Clear()
    '    Response.AddHeader("content-disposition", "attachment; filename=ESNStatus.xls")

    '    Response.ContentType = "application/vnd.xls"
    '    Dim stringWrite As New System.IO.StringWriter
    '    Dim htmlWrite As New HtmlTextWriter(stringWrite)
    '    esnOrdersGV.AllowPaging = False
    '    Me.BindESNOrdersGV()
    '    esnOrdersGV.RenderControl(htmlWrite)
    '    Response.Write(stringWrite.ToString())
    '    Response.End()
    'End Sub
    'Public Overrides Sub VerifyRenderingInServerForm(Control As Control)

    'End Sub

    Private Sub ReloadPage()
        Dim str As String = "tab=" & adminTabContainer.ActiveTabIndex.ToString

        Session("_RedirectSaveState") = str
        Response.Redirect("~/Admin.aspx")
    End Sub


    ' Handle ajax calls
    Public Sub RaiseCallbackEvent(ByVal eventArgument As String) _
        Implements System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent

        Dim getAction As String = eventArgument.Substring(0, (InStr(eventArgument, ":") - 1))

        If getAction = "GetAccountDetails" Then
            Dim accountId As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetAccountDetailsForClient(accountId)
        End If

        If getAction = "GetUserDetails" Then
            Dim userId As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetUserDetails(userId)
        End If

        If getAction = "GetComPlan" Then
            Dim planName As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetComPlan(planName)
        End If

    End Sub

    Public Function GetCallbackResult() As String _
        Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function
    'Protected Sub filterPinCarrier_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filterPinCarrier.SelectedIndexChanged
    '    For Each pin In pinTypeDropdown.Items()
    '        If pin.enabled = False Then
    '            pin.enabled = True
    '        Else
    '            pin.enabled = False
    '        End If
    '    Next
    '    pinTypeDropdown.Items(0).Enabled = True

    'End Sub

    Protected Sub downloadReportBtn_ServerClick(sender As Object, e As System.EventArgs) Handles downloadReportBtn.ServerClick

        Dim xlExporter As DataTableToExcel = New DataTableToExcel

        Dim dv As DataView
        Dim Table As DataTable
        dv = CType(esnDataSource.Select(DataSourceSelectArguments.Empty), DataView)
        Table = dv.Table()

        xlExporter.DataTable = Table

        xlExporter.FileName = "ESN"

        xlExporter.Export()


    End Sub

    Private Sub LoadVZPPBalance()
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim cmd As New SqlCommand
        cmd.Connection = con
        cmd.CommandText = "Select top 1 isnull(balance, 0) balance, isnull(updatedate, getdate()) updatedate from vzppbalance order by updatedate desc "

        Dim dtr As SqlDataReader = cmd.ExecuteReader()

        While dtr.Read()
            lblVZPPBalace.InnerText = Math.Round(CDbl(dtr.Item("balance")), 2)
            lblVZPPLastUpdated.InnerText = dtr.Item("updatedate")
        End While

    End Sub

   

End Class