
Partial Class Site
    Inherits System.Web.UI.MasterPage

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            NavigationMenu.Style.Add("display", "none")
        End If

        Dim level As Integer = 0

        If Session.Item("UserLevel") IsNot Nothing Then
            level = Session("UserLevel")
        End If

        If level <> 1 Then 
            ' Remove restricted MenuItems
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("Admin")))
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("Sprint")))
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("Con")))
            'NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("V CDR")))

            'NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("Summary")))
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("Rpts")))
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("ESN")))
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("IVR")))
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("Telco")))
            NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("VZPP")))
            If level <> 2 Then
                NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("Cust")))
            End If
        End If

        'Disable for all  - DL 051219
        NavigationMenu.Items.RemoveAt(NavigationMenu.Items.IndexOf(NavigationMenu.FindItem("T CDR")))

        ' Set up the MenuItem selected highlight
        Dim ThisPage As String = Page.AppRelativeVirtualPath
        Dim SlashPos As Integer = InStrRev(ThisPage, "/")

        Dim PageName As String = Right(ThisPage, Len(ThisPage) - SlashPos)
        '-- Select menu item with matching NavigateUrl property
        
        If PageName = "Login.aspx" Then
            NavigationMenu.Style.Add("display", "none")
            Exit Sub
        End If

        For Each ParentMenu As MenuItem In NavigationMenu.Items
            If ParentMenu.NavigateUrl.Replace("~/", "") = PageName Then
                ParentMenu.Selected = True
            End If
        Next

    End Sub

End Class