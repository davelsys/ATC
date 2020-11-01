
Partial Class SearchCDR
    Inherits System.Web.UI.Page


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

        If Not IsPostBack Then
            Dim con As SqlConnection
            Dim cmd As SqlCommand

            con = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            con.Open()

            Dim str As String = "Select distinct Accountname from accounts where isnull(active,0) = 1 order by Accountname "
            cmd = New SqlCommand(str, con)
            Dim dtr As SqlDataReader = cmd.ExecuteReader

            dropAccounts.DataSource = dtr
            dropAccounts.DataTextField = "Accountname"
            dropAccounts.DataValueField = "Accountname"
            dropAccounts.DataBind()

            con.Close()

            dropAccounts.SelectedIndex = 0
            AccountChange()

        End If
    End Sub

  
    Protected Sub btnSearch_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        GridView1.DataBind()
    End Sub

    Protected Sub dropAccounts_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dropAccounts.SelectedIndexChanged
        AccountChange()

    End Sub

    Private Sub AccountChange()
        Dim con As SqlConnection
        Dim cmd As SqlCommand

        con = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim str As String = "Select distinct PhoneAccount,phoneAccountid from [PPCData].[dbo].[CDR] where accountname='" & dropAccounts.SelectedValue & "' order by phoneaccount "
        cmd = New SqlCommand(str, con)
        Dim dtr As SqlDataReader = cmd.ExecuteReader

        dropPhoneAccounts.DataSource = dtr
        dropPhoneAccounts.DataTextField = "PhoneAccount"
        dropPhoneAccounts.DataValueField = "PhoneAccountId"
        dropPhoneAccounts.DataBind()

        dropPhoneAccounts.Items.Insert(0, New ListItem("All", -1))

        dropPhoneAccounts.SelectedIndex = 0

        con.Close()
    End Sub
End Class
