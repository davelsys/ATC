Imports System.Web.Security

Partial Class Users
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

        If Not IsPostBack Then
            PopulateUserLevelDropdown()
        Else

        End If

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

        ' Clear the error message
        manageUsersErrorMsgDiv.InnerText = ""

    End Sub

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
        sql.Append("WHEN level = 3 THEN 'Agent' ELSE '' END) as userLevel ")
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

    Protected Sub searchUsersBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchUsersBtn.Click
        BindUserNameGrid()
        ResetUserFields()
    End Sub

    Protected Sub usersGridView_Sorted(ByVal sender As Object, ByVal e As System.EventArgs) Handles usersGridView.Sorted
        BindUserNameGrid()
        ResetUserFields()
    End Sub

    Protected Sub usersGridView_PageIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles usersGridView.PageIndexChanged
        BindUserNameGrid()
        ResetUserFields()
    End Sub

    Protected Sub usersGridView_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles usersGridView.RowDataBound
        If e.Row.RowType = DataControlRowType.DataRow Then

            e.Row.Attributes.Add("onmouseover", "this.style.cursor = 'pointer';this.style.backgroundColor = '#fdecc9';")
            e.Row.Attributes.Add("onmouseout", "this.style.backgroundColor = 'white';")

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
        Dim creditLimit As Decimal = Decimal.Parse(If(creditLimitField.Text.Length > 0, creditLimitField.Text, "0"))
        Dim monitorAgnt As Boolean = monitorAgent.Checked

        Try
            newUser = Membership.CreateUser(userId, password)

            newUser.Email = email
            Membership.UpdateUser(newUser)

        Catch cue As MembershipCreateUserException
            manageUsersErrorMsgDiv.InnerText = GetErrorMessage(cue.StatusCode)
            Exit Sub
        Catch httpe As HttpException
            manageUsersErrorMsgDiv.InnerText = httpe.Message
            Exit Sub
        End Try

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As String = "UPDATE [ASPNETDB].[dbo].[aspnet_Users] SET [UserPersonalName] = @FullName, "
        sql &= "[Level] = @UserLevel, [CreditLimit] = @CreditLimit, [UserMonitor] = @UserMonitor WHERE [UserId] = @UserId "

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        cmd.Parameters.Add("FullName", SqlDbType.VarChar).Value = If(personalName.Length > 0, personalName, "")
        cmd.Parameters.Add("UserLevel", SqlDbType.SmallInt).Value = userLevel
        cmd.Parameters.Add("UserMonitor", SqlDbType.Bit).Value = monitorAgnt
        cmd.Parameters.Add("CreditLimit", SqlDbType.Money).Value = creditLimit
        cmd.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = New Guid(newUser.ProviderUserKey.ToString())

        Dim rowsAffected = cmd.ExecuteNonQuery()
        If rowsAffected > 0 Then
            manageUsersErrorMsgDiv.InnerText = "User successfully created."
            BindUserNameGrid()
            SetEditableFields()
        End If

        con.Close()

    End Sub

    Private Function GetErrorMessage(ByVal status As MembershipCreateStatus) As String

        Select Case status
            Case MembershipCreateStatus.DuplicateUserName
                Return "User id already exists. Please enter a different user id."

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

    Protected Sub updateUserBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles updateUserBtn.Click

        Dim personalName As String = userNameEditFld.Text
        Dim userId As String = userIdEditFld.Text
        Dim userLevel As Integer = userLevelDropdown.SelectedItem.Value
        Dim monitorAgnt As Boolean = monitorAgent.Checked
        Dim email As String = emailEditFld.Text
        Dim creditLimit As Decimal = Decimal.Parse(If(creditLimitField.Text.Length > 0, creditLimitField.Text, "0"))

        Dim user As MembershipUser = Membership.GetUser(userId)

        'user.UnlockUser()

        If user Is Nothing Then
            manageUsersErrorMsgDiv.InnerText = "Failed to update user. Please check the user id."
            Exit Sub
        End If

        user.Email = email
        Membership.UpdateUser(user)

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As String = "UPDATE [ASPNETDB].[dbo].[aspnet_Users] SET [UserPersonalName] = @FullName, "
        sql &= "[Level] = @UserLevel, [CreditLimit] = @CreditLimit, [UserMonitor] = @UserMonitor WHERE [UserId] = @UserId "

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        cmd.Parameters.Add("FullName", SqlDbType.VarChar).Value = If(personalName.Length > 0, personalName, "")
        cmd.Parameters.Add("UserLevel", SqlDbType.SmallInt).Value = userLevel
        cmd.Parameters.Add("UserMonitor", SqlDbType.Bit).Value = monitorAgnt
        cmd.Parameters.Add("CreditLimit", SqlDbType.Money).Value = creditLimit
        cmd.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = New Guid(user.ProviderUserKey.ToString())

        Dim rowsAffected = cmd.ExecuteNonQuery()
        If rowsAffected > 0 Then
            manageUsersErrorMsgDiv.InnerText = "User successfully updated."
            BindUserNameGrid()
            SetEditableFields()
        End If

        con.Close()

    End Sub

    Protected Sub deleteUserBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles deleteUserBtn.Click

        Dim deleted As Boolean
        
        Try
            deleted = Membership.DeleteUser(userIdEditFld.Text)
        Catch Ex As ArgumentException
            deleted = False
        End Try

        If deleted Then
            ResetUserFields()
        Else
            manageUsersErrorMsgDiv.InnerText = "Failed to delete user. Please check the user id."
        End If

        BindUserNameGrid()

    End Sub

    Private Sub ResetUserFields()
        userNameEditFld.Text = ""
        userIdEditFld.Text = ""
        userLevelDropdown.SelectedIndex = userLevelDropdown.Items.IndexOf(userLevelDropdown.Items.FindByText("Agent"))
        emailEditFld.Text = ""
        monitorAgent.Checked = True
        creditLimitField.Text = ""

        ResetFields()

    End Sub

    Private Sub GetUserDetails(ByVal userId As String)

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As String = "SELECT TOP 1 [UserPersonalName] AS FullName, [UserName], [Level], [CreditLimit], [Email], [UserMonitor] "
        sql &= "FROM [ASPNETDB].[dbo].[aspnet_Users] LEFT JOIN [ASPNETDB].[dbo].[aspnet_Membership] "
        sql &= "ON [aspnet_Users].UserId = [aspnet_Membership].UserId "
        sql &= "WHERE [UserName] = @UserID "

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        cmd.Parameters.Add("UserID", SqlDbType.NVarChar).Value = userId

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If reader.HasRows Then
            Dim ds As New DataSet
            ds.Load(reader, LoadOption.PreserveChanges, "aspnet_Users")
            _callBackResult = ds.GetXml()
        Else
            _callBackResult = "none"
        End If

        reader.Close()
        con.Close()
    End Sub

    Private Sub SetEditableFields()
        userIdEditFld.ReadOnly = True
        passwordEditFld.Enabled = False
        confirmPassword.Enabled = False
    End Sub

    Private Sub ResetFields()
        userIdEditFld.ReadOnly = False
        passwordEditFld.Enabled = True
        confirmPassword.Enabled = True
    End Sub

    ' Handle ajax calls
    Public Sub RaiseCallbackEvent(ByVal eventArgument As String) _
        Implements System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent

        Dim getAction As String = eventArgument.Substring(0, (InStr(eventArgument, ":") - 1))

        If getAction = "GetUserDetails" Then
            Dim userId As String = eventArgument.Substring(InStr(eventArgument, ":"))
            GetUserDetails(userId)
        End If

    End Sub

    Public Function GetCallbackResult() As String _
        Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function

End Class
