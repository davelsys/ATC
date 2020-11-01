
Partial Class Esn
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
            BindesnvalGrid()
        End If
    End Sub

    Private Sub BindesnvalGrid()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim dt As New DataTable


        sql.Append("(select o.cell_num as [CellNum], o.Esn  as [OrderESN], e.Esn as [ppESN], DeviceMake, DeviceMode, DeviceModel, DeviceState,  LastModified from ESNValidation e ")
        sql.Append("JOIN orders o ON e.MDN = o.cell_num ")
        sql.Append("WHERE o.monitor = 1 and o.esn != e.esn ")
        sql.Append("union ")
        sql.Append("select o.cell_num,'', e.Esn as [ppESN], DeviceMake, DeviceMode, DeviceModel, DeviceState,  LastModified from ESNValidation e ")
        sql.Append("join orders o on o.esn= e.Esn ")
        sql.Append("where DeviceState = 'UNKNOWN') ")
        sql.Append("order by 8")


        esnvalGridviewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        esnvalGridviewDataSource.SelectCommand = sql.ToString()
        esnvalGridviewDataSource.CancelSelectOnNullParameter = False

        esnvalGridview.DataBind()

    End Sub
    Protected Sub downloadENSBtn_ServerClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles downloadESNBtn.ServerClick

        Dim xlExporter As DataTableToExcel = New DataTableToExcel


        ' Rebinding the grid to retrieve the DataSource.
        BindesnvalGrid()

        Dim dv As DataView = New DataView()
        dv = esnvalGridviewDataSource.Select(DataSourceSelectArguments.Empty)


        xlExporter.DataTable = dv.ToTable

        xlExporter.FileName = "ENSval"

        xlExporter.Export()

    End Sub
    Protected Sub esnvalGridview_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles esnvalGridview.PageIndexChanging
        esnvalGridview.PageIndex = e.NewPageIndex
        BindesnvalGrid()
    End Sub
    Protected Sub esnvalGridview_Sorting(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles esnvalGridview.Sorting
        BindesnvalGrid()
    End Sub
End Class
