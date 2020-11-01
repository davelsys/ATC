Imports System.IO
Partial Class Pins
    Inherits System.Web.UI.Page
    Implements System.Web.UI.ICallbackEventHandler

    Dim _callBackResult As String
    Protected _gcsManager As GlobalCellSearch

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

        If Session.Item("UserLevel") <> 1 Then
            filterPinsDiv.Visible = False
            filterVendorDiv.Visible = False
            pinsGridview.Columns(3).Visible = False
        End If

        _gcsManager = New GlobalCellSearch(IsPostBack, Session("SearchedCell"), phoneNumberSearchFld)

        If Not IsPostBack Then
            BindPinsGrid()
            getPinsSummary()
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

        'If filterPinCarrier.SelectedValue.ToLower = "page plus" Then
        sql.Append("select [id], Assigned, [PIN], Cellnum, case when isnull(Vendor,'') like 'Epay%' then 'W' when Vendor='Saycor' then 'J' else left(1,vendor) end [Vendor],DatePurchased, PinType, [control], ")
        sql.Append("'$' + CONVERT( VARCHAR(12), postbalamt, 1 ) AS [postbalamt], postbalpin, [status], retrycount ")
        sql.Append("from pins ")
        'ElseIf filterPinCarrier.SelectedValue.ToLower = "all talk" Then
        'sql.Append("select [id], Assigned, [PIN], Cellnum, PinType, [control], confirmation_num, [status], retrycount ")
        'sql.Append("from atc_pins ")
        'End If

        ' See if there is any filtering
        Dim isFilteredAlready As Boolean = False

        If userLevel = 3 Then
            sql.Append("JOIN orders ON PINS.CellNum = orders.cell_num ")
            sql.Append("JOIN customers ON orders.customer_id = customers.customer_id ")
            sql.Append("WHERE [initial_agent] = '" & Membership.GetUser.UserName & "' ")
            isFilteredAlready = True
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

        If userLevel = 1 Then

            Dim selectedRadio As String = filterPinsRadioList.SelectedValue

            If selectedRadio = "failed" Then
                If isFilteredAlready Then
                    sql.Append("AND [status] = 'failed' ")
                Else
                    sql.Append("WHERE [status] = 'failed' ")
                    isFilteredAlready = True
                End If
            ElseIf selectedRadio = "assigned" Then
                If isFilteredAlready Then
                    sql.Append("AND isnull([Assigned], '') <> '' ")
                Else
                    sql.Append("WHERE isnull([Assigned], '') <> '' ")
                    isFilteredAlready = True
                End If
            ElseIf selectedRadio = "unassigned" Then
                If isFilteredAlready Then
                    sql.Append("AND isnull([Assigned], '') = '' ")
                Else
                    sql.Append("WHERE isnull([Assigned], '') = '' ")
                    isFilteredAlready = True
                End If
            End If

            If filterPinVendors.Enabled = True Then
                Dim selectedVendor As String = filterPinVendors.SelectedValue

                If selectedVendor = "J" Then
                    If isFilteredAlready Then
                        sql.Append("AND isnull([vendor],'')= 'Saycor' ")
                    Else
                        sql.Append("WHERE isnull([vendor],'') = 'Saycor' ")
                        isFilteredAlready = True
                    End If
                Else
                    If selectedVendor = "W" Then
                        If isFilteredAlready Then
                            sql.Append("AND isnull([vendor],'') like 'Epay%' ")
                        Else
                            sql.Append("WHERE isnull([vendor],'') like 'Epay%' ")
                            isFilteredAlready = True
                        End If
                    End If
                End If
            End If

        Else

            If isFilteredAlready Then
                sql.Append("AND isnull([Assigned], '') <> '' ")
            Else
                sql.Append("WHERE isnull([Assigned], '') <> '' ")
                isFilteredAlready = True
            End If

        End If



        sql.Append("ORDER BY Assigned DESC ")

        pinsGridviewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        pinsGridviewDataSource.SelectCommand = sql.ToString()
        pinsGridviewDataSource.CancelSelectOnNullParameter = False

        pinsGridview.DataBind()

    End Sub

    Protected Sub pinsGridview_DataBound(ByVal sender As Object, ByVal e As System.EventArgs) Handles pinsGridview.DataBound
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

                'FK added to hide dropdown when no cell num is there
                If row.Item("cellNum").ToString() = "" Then
                    dropdown.Visible = False
                End If
            End If


        End If

    End Sub

    Protected Sub downloadPinsBtn_ServerClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles downloadPinsBtn.ServerClick

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
        Dim value2 As Integer = 0 'this variable is used to assign correct header names for the all talk grid because all talk grid doesn't follow the pinsGridview headertexts order because in the all talk grid, some columns from teh pinsgridview are hidden
        For value As Integer = 0 To (dt.Columns.Count - 1)

            'If (value2 = 3 Or value2 = 7) And filterPinCarrier.SelectedValue.ToLower = "all talk" Then
            '    value2 += 2
            'End If

            dt.Columns(value).ColumnName() =
            pinsGridview.Columns(value2).HeaderText()

            value2 += 1
        Next


        xlExporter.DataTable = dt

        xlExporter.FileName = "Pins"

        xlExporter.Export()

    End Sub

    Protected Sub badTransDropdown_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim dropdown As DropDownList = DirectCast(sender, DropDownList)

        ' Get the row by going up two levels in the DOM tree.
        Dim row As GridViewRow = DirectCast(dropdown.Parent.Parent, GridViewRow)

        Dim rowIndex As Integer = row.RowIndex

        Dim theid As Integer = Integer.Parse(pinsGridview.DataKeys(rowIndex).Value.ToString)




        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim action As String = dropdown.SelectedValue
        Dim strSql As String = ""

        'If filterPinCarrier.SelectedValue.ToLower = "page plus" Then
        If Session.Item("UserLevel") = 1 And action = "OK" Then
            strSql = "UPDATE [PINS] SET [action] = '', [status] = @Action WHERE [id] = @PinId "
        ElseIf action = "Retry" Then
            strSql = "UPDATE [PINS] SET [action] = 'Retry', [status] = 'Retry', [retrycount] = isnull([retrycount],0) + 1 WHERE [id] = @PinId "
        End If
        'ElseIf filterPinCarrier.SelectedValue.ToLower = "all talk" Then
        '    If Session.Item("UserLevel") = 1 And action = "OK" Then
        '        strSql = "UPDATE [ATC_PINS] SET [action] = '', [status] = @Action WHERE [id] = @PinId "
        '    ElseIf action = "Retry" Then
        '        strSql = "UPDATE [ATC_PINS] SET [action] = 'Retry', [status] = 'Retry', [retrycount] = isnull([retrycount],0) + 1 WHERE [id] = @PinId "
        '    End If

        'End If



        Dim sql As String = strSql

        Dim cmd As SqlCommand = New SqlCommand(sql, con)
        cmd.Parameters.Add("Action", SqlDbType.VarChar).Value = action
        cmd.Parameters.Add("PinId", SqlDbType.Int).Value = theid

        cmd.ExecuteNonQuery()

        con.Close()

        'If filterPinCarrier.SelectedValue.ToLower = "all talk" And action = "Retry" Then
        '    Dim pin As String = ""
        '    Dim cellnum As String = ""
        '    Dim pinType As String = ""
        '    Dim conn As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        '    conn.Open()
        '    Dim sqlstr As String = "select pin, isnull(cellnum, '') [cellnum], PinType from atc_pins where ID=@id"
        '    Dim comm As SqlCommand = New SqlCommand(sqlstr, conn)
        '    comm.Parameters.Add("@id", SqlDbType.Int).Value = theid
        '    Dim reader As SqlDataReader = comm.ExecuteReader
        '    If reader.Read Then
        '        pin = reader("pin")
        '        cellnum = reader("cellnum")
        '        pinType = reader("PinType")
        '    End If
        '    reader.Close()
        '    conn.Close()
        '    If cellnum <> "" Then
        '        Dim statusmsg As String = ""
        '        'ATClass.ATCPinAssign.AssignPin(cellnum, pin, pinType, statusmsg)
        '        ATCPin.AssignPin(cellnum, pin, pinType, statusmsg)
        '        If statusmsg <> "" Then
        '            orderErrorMessage.InnerText = statusmsg
        '        Else
        '            orderErrorMessage.InnerText = ""
        '        End If
        '    End If

        'End If
        BindPinsGrid()

    End Sub

    Protected Sub pinsGridview_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles pinsGridview.PageIndexChanging
        pinsGridview.PageIndex = e.NewPageIndex
        BindPinsGrid()
    End Sub

    Protected Sub pinsGridview_Sorting(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles pinsGridview.Sorting
        BindPinsGrid()
    End Sub

    Protected Sub filterPinsRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filterPinsRadioList.SelectedIndexChanged
        BindPinsGrid()
    End Sub
    Protected Sub filterPinVendors_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filterPinVendors.SelectedIndexChanged
        BindPinsGrid()
    End Sub

    Protected Sub searchPinsBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchPinsBtn.Click
        BindPinsGrid()
    End Sub
    'Protected Sub filterPinCarrier_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filterPinCarrier.SelectedIndexChanged
    '    If filterPinCarrier.SelectedValue.ToLower = "page plus" Then
    '        filterPinVendors.Enabled = True
    '        'lblPinsSummary.Visible = False

    '        pinsGridview.Columns(3).Visible = True
    '        pinsGridview.Columns(4).Visible = True
    '        pinsGridview.Columns(8).Visible = True
    '        pinsGridview.Columns(9).Visible = True
    '        pinsGridview.Columns(7).Visible = False
    '    ElseIf filterPinCarrier.SelectedValue.ToLower = "all talk" Then
    '        filterPinVendors.Enabled = False
    '        'lblPinsSummary.Visible = True

    '        pinsGridview.Columns(3).Visible = False
    '        pinsGridview.Columns(4).Visible = False
    '        pinsGridview.Columns(8).Visible = False
    '        pinsGridview.Columns(9).Visible = False
    '        pinsGridview.Columns(7).Visible = True

    '    End If
    '    BindPinsGrid()
    '    getPinsSummary()
    'End Sub

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
    Private Sub getPinsSummary()
        Try
            Dim conn As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            conn.Open()

            Dim sqlstr As String = "DECLARE @combinedString VARCHAR(MAX) "
            sqlstr += "SELECT @combinedString = COALESCE(@combinedString + ' | ', '') + pintype + ': ' + convert(varchar, COUNT(*)) "

            ' If filterPinCarrier.SelectedValue.ToLower = "page plus" Then
            sqlstr += "FROM pins "
            'ElseIf filterPinCarrier.SelectedValue.ToLower = "all talk" Then
            'sqlstr += "FROM atc_pins "
            'End If

            sqlstr += "WHERE Assigned Is null and pin is not null "
            sqlstr += "group by Pintype "
            sqlstr += "order by pintype "
            sqlstr += "SELECT @combinedString as AvblPins "


            Dim comm As SqlCommand = New SqlCommand(sqlstr, conn)
            Dim reader As SqlDataReader = comm.ExecuteReader
            If reader.Read Then
                lblPinsSummary.InnerHtml = "<b>Available Pins: </b>" & reader("AvblPins")

            End If
            reader.Close()
            conn.Close()
        Catch ex As Exception

        End Try
    End Sub
End Class
