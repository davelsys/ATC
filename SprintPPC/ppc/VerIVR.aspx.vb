
Partial Class VerIVR
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

        If Not IsPostBack Then
            BindivrvalGrid()
        End If
    End Sub

    Private Sub BindivrvalGrid()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        'Dim dt As New DataTable

        sql.Append("SELECT cid AS [CallerID], mid AS [CellNum], ")
        sql.Append("start AS [CallTime], Activity, ")
        sql.Append("CASE WHEN newcc is null OR newcc = '' Then curcc else newcc end AS [CreditCard], ")
        sql.Append("(SELECT planname AS [Plan] from ppc.dbo.Plans p where p.Planid = newplan)as [NewPlan], ")
        sql.Append("(SELECT planname AS [Plan] from ppc.dbo.Plans p where p.Planid = curplan)as [CurrentPlan], ")
        sql.Append("renewattempt AS [RenewAttempt], renewresult AS [RenewResult] ")
        sql.Append("FROM IVR_calls ")

        If phoneNumberSearchFld.Text.Length > 0 Then
            sql.Append("WHERE mid LIKE '%' + @PhoneNumber + '%' ")
        End If

        sql.Append("ORDER BY ivrid DESC ")

        ivrvalGridviewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        ivrvalGridviewDataSource.SelectCommand = sql.ToString()
        ivrvalGridviewDataSource.CancelSelectOnNullParameter = False

        ivrvalGridview.DataBind()

    End Sub
    Protected Sub ivrvalGridview_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles ivrvalGridview.PageIndexChanging
        ivrvalGridview.PageIndex = e.NewPageIndex
        BindivrvalGrid()
    End Sub
    Protected Sub ivrvalGridview_Sorting(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles ivrvalGridview.Sorting
        BindivrvalGrid()
    End Sub
    Protected Sub searchBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchBtn.Click
        BindivrvalGrid()
    End Sub
End Class
