Imports System.IO

Partial Class Pins
    Inherits System.Web.UI.Page
    Implements System.Web.UI.ICallbackEventHandler

    Dim _callBackResult As String
    Dim Protected _gcsManager As GlobalCellSearch

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
            refreshCellNum.Visible = False
        End If

        _gcsManager = New GlobalCellSearch(IsPostBack, Session("SearchedCell"), phoneNumberSearchFld)

        If Not IsPostBack Then
            BindPinsGrid()
        End If

        ' When a user double clicks a row, javascript postsback
        ' with the eventtarget set to the search button for this page.
        ' See pinsGridview_RowDataBound below.
        If Request.Form.Item("__EVENTTARGET") = "searchPinsBtn" Then
            BindPinsGrid()
        End If

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

    End Sub

    Private Sub BindPinsGrid()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim dt As New DataTable
        Dim userLevel As Integer = Session.Item("UserLevel")

        If _gcsManager.IsGSOn Then
            Session("SearchedCell") = _gcsManager.TextBox.Text
        Else
            Session.Remove("SearchedCell")
        End If

        sql.Append("select [id], DatePurchased, [PIN], Cellnum, Assigned, PinType, [control], ")
        sql.Append("'$' + CONVERT( VARCHAR(12), postbalamt, 1 ) AS [postbalamt], postbalpin, [status], retrycount ")
        sql.Append("from pins ")

        ' See if there is any filtering
        Dim isFilteredAlready As Boolean = False

        If userLevel = 3 Then
            sql.Append("JOIN orders ON PINS.CellNum = orders.cell_num ")
            sql.Append("JOIN customers ON orders.customer_id = customers.customer_id ")
            sql.Append("WHERE [initial_agent] = '" & Membership.GetUser.UserName & "' ")
            isFilteredAlready = True
        End If

        Dim selectedRadio As String = filterPinsRadioList.SelectedValue
        If selectedRadio = "failed" Then
            If isFilteredAlready Then
                sql.Append("AND [status] = 'failed' ")
            Else
                sql.Append("WHERE [status] = 'failed' ")
                isFilteredAlready = True
            End If
        End If

        If phoneNumberSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [CellNum] LIKE '%' + @PhoneNumber + '%' ")
            Else
                sql.Append("WHERE [CellNum] LIKE '%' + @PhoneNumber + '%' ")
                isFilteredAlready = True
            End If
        End If

        If pinSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [Pin] LIKE '%' + @Pin + '%' ")
            Else
                sql.Append("WHERE [Pin] LIKE '%' + @Pin + '%' ")
                isFilteredAlready = True
            End If
        End If

        sql.Append("ORDER BY DatePurchased DESC ")

        pinsGridviewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        pinsGridviewDataSource.SelectCommand = sql.ToString()
        pinsGridviewDataSource.CancelSelectOnNullParameter = False

        pinsGridview.DataBind()

    End Sub

    Protected Sub pinsGridview_DataBound(sender As Object, e As System.EventArgs) Handles pinsGridview.DataBound
        If pinsGridview.Rows.Count() = 0 Then
            downloadBtnDiv.Visible = False
        Else
            downloadBtnDiv.Visible = True
        End If
    End Sub

    Protected Sub pinsGridview_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles pinsGridview.RowDataBound

        If e.Row.RowType = DataControlRowType.DataRow Then

            Dim row As DataRow = DirectCast(e.Row.DataItem, DataRowView).Row

            e.Row.Attributes.Add("title", "Double click to globally search by cell number.")
            e.Row.Attributes.Add("onmouseover", "this.style.cursor = 'pointer'; this.style.backgroundColor = '#fdecc9';")
            e.Row.Attributes.Add("onmouseout", "setBackgroundWhite(this);")
            If Session("UserLevel") = 1 Then
                e.Row.Attributes.Add("onclick", "setClickBGColor(this);")
            End If

            Dim dropdown As DropDownList = DirectCast(e.Row.Controls(e.Row.Cells().Count() - 1).FindControl("badTransDropdown"), DropDownList)

            Dim action As String = row.Item("status").ToString()
            If dropdown IsNot Nothing Then
                If action.ToLower <> "ok" Then
                    dropdown.Visible = True
                End If
            End If

        End If

    End Sub

    Protected Sub downloadPinsBtn_ServerClick(sender As Object, e As System.EventArgs) Handles downloadPinsBtn.ServerClick
        
        Dim xlExporter As DataTableToExcel = New DataTableToExcel
        Dim dt As DataTable

        ' Rebinding the grid to retrieve the DataSource.
        BindPinsGrid()

        Dim dv As DataView = New DataView()
        dv = pinsGridviewDataSource.Select(DataSourceSelectArguments.Empty)

        dt = dv.ToTable

        ' Do some work on the DataTable before exporting to excel
        ' Remove the id
        dt.Columns.Remove("id")
        
        ' Format headers. Note: Only iterate over datatable columns because
        ' the GridView has more columns than the DataTable.
        ' Also, the order of the DataTable columns have to match the order of the GridView columns
        For value As Integer = 0 To (dt.Columns.Count - 1)
            dt.Columns(value).ColumnName() =
                pinsGridview.Columns(value).HeaderText
	    Next

        xlExporter.DataTable = dt
        
        xlExporter.FileName = "Pins"

        xlExporter.Export

    End Sub

    Protected Sub badTransDropdown_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim dropdown As DropDownList = DirectCast(sender, DropDownList)

        ' Get the row by going up two levels in the DOM tree.
        Dim row As GridViewRow = DirectCast(dropdown.Parent.Parent, GridViewRow)

        Dim rowIndex As Integer = row.RowIndex

        Dim theid As Integer = Integer.Parse(pinsGridview.DataKeys(rowIndex).Value.ToString)
        
        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As String = "UPDATE [PINS] SET [action] = @Action, [status] = @Action WHERE [id] = @PinId "

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        Dim action As String = dropdown.SelectedValue
        cmd.Parameters.Add("Action", SqlDbType.VarChar).Value = action
        cmd.Parameters.Add("PinId", SqlDbType.Int).Value = theid

        cmd.ExecuteNonQuery()

        con.Close()

        BindPinsGrid()

    End Sub

    Protected Sub pinsGridview_PageIndexChanging(sender As Object, e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles pinsGridview.PageIndexChanging
        pinsGridview.PageIndex = e.NewPageIndex
        BindPinsGrid()
    End Sub

    Protected Sub pinsGridview_Sorting(sender As Object, e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles pinsGridview.Sorting
        BindPinsGrid()
    End Sub

    Protected Sub filterPinsRadioList_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles filterPinsRadioList.SelectedIndexChanged
        BindPinsGrid()
    End Sub

    Protected Sub searchPinsBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchPinsBtn.Click
        BindPinsGrid()
    End Sub

    Private Sub getCurrentCellNumForClient(ByVal args As String)

        Dim parts As String() = args.Split(",")
        Dim cellNum As String = parts(0)
        Dim rowIndex As Integer = parts(1)

        ' rowIndex is from the client side that seems to consider the first row index one.
        ' Here we are getting the row index for the gridview which considers the first row index zero.
        Dim id As Integer = Integer.Parse(pinsGridview.DataKeys(rowIndex - 1).Value.ToString)
        
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        cmd.CommandType = Data.CommandType.StoredProcedure
        cmd.CommandText = "UpdatePinCell"
        cmd.Parameters.Add("OldCell", SqlDbType.VarChar).Value = cellNum
        cmd.Parameters.Add("Id", SqlDbType.Int).Value = id

        cmd.Connection.Open()

        Dim reader As SqlDataReader = cmd.ExecuteReader
        If reader.Read Then
            If reader.Item("new_cell") IsNot DBNull.Value Then
                _callBackResult = reader.Item("new_cell")
            Else
                _callBackResult = "isNull"
            End If
        Else
            _callBackResult = "failed"
        End If

        cmd.Connection.Close()

    End Sub


    ' Handle ajax calls
    Public Sub RaiseCallbackEvent(ByVal eventArgument As String) _
        Implements System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent

        Dim getAction As String = eventArgument.Substring(0, (InStr(eventArgument, ":") - 1))

        If getAction = "refreshCellNum" Then
            Dim args As String = eventArgument.Substring(InStr(eventArgument, ":"))
            getCurrentCellNumForClient(args)
        End If

    End Sub

    Public Function GetCallbackResult() As String _
        Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function

End Class