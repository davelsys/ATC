
Partial Class Account_Login
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Request.Browser.Browser <> "IE" Then
            pleaseEnterPTag.Visible = False
            LoginUser.Visible = False
            notIELbl.Text = "System only supports Internet Explorer. <br /> Please login using an Internet Explorer web browser."
        End If

        ' This unsets the session variable for auto searching
        ' by number. This should never be set when logging in.
        If Session("SearchedCell") IsNot Nothing
            Session("SearchedCell") = Nothing
        End If

    End Sub

    Protected Sub LoginUser_LoggedIn(ByVal sender As Object, ByVal e As System.EventArgs) Handles LoginUser.LoggedIn
        ' Initialize values.
        Dim connection As New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmd As SqlCommand
        Dim sql As New StringBuilder

        sql.Append("SELECT [Level], [UserMonitor] FROM [ASPNETDB].[dbo].[aspnet_Users] WHERE [UserId] = @UserID")

        connection.Open()

        cmd = New SqlCommand(sql.ToString, connection)
        cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = Membership.GetUser(LoginUser.UserName).ProviderUserKey.ToString()

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            If reader.Item("Level") IsNot DBNull.Value Then
                Session("UserLevel") = reader.Item("Level")
            Else
                Session("UserLevel") = 3    ' Lowest level
            End If

            If reader.Item("UserMonitor") IsNot DBNull.Value Then
                Session("UserMonitor") = reader.Item("UserMonitor")
            Else
                Session("UserMonitor") = 1  ' Default to monitor
            End If
        End If

        reader.Close()
        connection.Close()

    End Sub

    Protected Sub LoginUser_LoggingIn(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.LoginCancelEventArgs) Handles LoginUser.LoggingIn
        
        Dim user As MembershipUser = Membership.GetUser(LoginUser.UserName)
        
        If user IsNot Nothing Then
            If user.IsLockedOut Then
                LoginUser.FailureText = "This user has been locked out."
            End If
        End If

    End Sub

End Class