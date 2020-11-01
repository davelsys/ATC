
Partial Class ManageAccounts
    Inherits System.Web.UI.Page
    Implements System.Web.UI.ICallbackEventHandler

    Dim _callBackResult As String

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            Response.Redirect(FormsAuthentication.LoginUrl)
        End If

        If Session.Item("UserLevel") Is Nothing Then
            Response.Redirect(FormsAuthentication.LoginUrl)
        End If

        BindAccountsGridview()

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

        ' Clear the error message
        editAccoutsErrorMsg.InnerText = ""

    End Sub

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

            e.Row.Attributes.Add("onmouseover", "this.style.cursor = 'pointer';this.style.backgroundColor = '#fdecc9';")
            e.Row.Attributes.Add("onmouseout", "this.style.backgroundColor = 'white';")

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

        HiddenAccountName.Value = editAccountNameFld.Text
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

        If HiddenAccountName.Value.Length > 0 Then
            If editAccountNameFld.Text.ToLower() <> HiddenAccountName.Value.ToLower() Then
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

    ' Handle ajax calls
    Public Sub RaiseCallbackEvent(ByVal eventArgument As String) _
        Implements System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent

        Dim getAction As String = eventArgument.Substring(0, (InStr(eventArgument, ":") - 1))
        
        If getAction = "GetAccountDetails" Then
            Dim accountId As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetAccountDetailsForClient(accountId)
        End If

    End Sub

    Public Function GetCallbackResult() As String _
        Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function

End Class
