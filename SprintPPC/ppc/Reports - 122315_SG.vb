Imports System.ComponentModel

Partial Class Reports
    Inherits System.Web.UI.Page

    Private _sortExpression As String = ""
    Private _gvSorting As Boolean = False

    Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load

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
        

    End Sub

    Protected Sub selectReportDropdown_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles selectReportDropdown.SelectedIndexChanged
        ' Reset gridview page index when a new report is selected.
        displayReportGridview.PageIndex = 0
        If selectReportDropdown.SelectedValue = "totalMin" Then
            dateRange.Visible = True
        Else
            dateRange.Visible = False
        End If
        ViewState("SortExpression") = Nothing
        ToDate.Text = ""
        FromDate.Text = ""
        DirectCalls()
    End Sub

    Private Sub DirectCalls()
        Dim value As String = selectReportDropdown.SelectedValue

        RemoveItemsReportColumns()

        'If value <> "itemsPerOrder" Then
        '    RemoveItemsReportColumns()
        'End If

        If value = "none" Then
            ClearReportsGridview()
        ElseIf value = "minPerCycle" Then
            BindMinutesPerCycleReport()
        ElseIf value = "itemsPerOrder" Then
            BindItemsPerOrderReport()
        ElseIf value = "unscrapedOrders" Then
            BindUnscrapedOrdersReport()
        ElseIf value = "mdnWithoutOrders" Then
            BindMDNsWithoutOrders()
        ElseIf value = "esnStatus" Then
            BindEsnStatus()
        ElseIf value = "verRpt" Then
            BindVerRprt()
        ElseIf value = "totalMin" Then
            BindTotalMinByDate()
        End If


    End Sub

    Private Sub ClearReportsGridview()
        displayReportGridview.DataSource = Nothing
        displayReportGridview.DataBind()
    End Sub

    Private Sub BindMinutesPerCycleReport()

        If IsNothing(Cache("MinutesPerCycle")) Then
            Cache.Insert("MinutesPerCycle", QueryMinutesPerCycle(), Nothing, DateTime.Now.AddMinutes(15), Cache.NoSlidingExpiration)
        End If

        displayReportGridview.DataSource = Cache("MinutesPerCycle")
        displayReportGridview.DataBind()

        SetBgGridviewColor()

    End Sub
    
    Private Function QueryMinutesPerCycle() As DataTable

        Dim cmd As SqlCommand = New SqlCommand
        Dim dataAdapter As SqlDataAdapter
        Dim dt As New DataTable

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "GetCellUsageByPeriod"
        cmd.CommandTimeout = 90

        cmd.Connection.Open()

        dataAdapter = New SqlDataAdapter(cmd)

        dataAdapter.Fill(dt)

        cmd.Connection.Close()

        Return dt

    End Function
    Private Sub BindEsnStatus()

        If IsNothing(Cache("EsnStatus")) Then
            Cache.Insert("EsnStatus", QueryEsnStatus(), Nothing, DateTime.Now.AddMinutes(15), Cache.NoSlidingExpiration)
        End If

        displayReportGridview.DataSource = Cache("EsnStatus")
        displayReportGridview.DataBind()

    End Sub
    Private Function QueryEsnStatus() As DataTable

        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "OrderDisplay"

        cmd.Connection.Open()

        Dim dt As DataTable = New DataTable
        dt.Load(cmd.ExecuteReader)

        displayReportGridview.DataSource = dt
        displayReportGridview.DataBind()

        cmd.Connection.Close()

        Return dt
    End Function

    Private Sub SetBgGridviewColor()

        ' Set colors for alternating cycles. We only start from the columns that are cycles.
        ' Right now the cycles begin from column 5.
        Dim temp As Integer
        Dim counter As Integer

        ' First do the header.
        displayReportGridview.HeaderRow.Cells(3).Width = "80"   ' The plan name column.
        For column As Integer = 5 To (displayReportGridview.HeaderRow.Cells().Count() - 1) Step 2
            temp = counter Mod 2
            If temp = 0 Then
                displayReportGridview.HeaderRow.Cells(column).BackColor = Drawing.Color.WhiteSmoke
                displayReportGridview.HeaderRow.Cells(column + 1).BackColor = Drawing.Color.WhiteSmoke
            End If
            counter = counter + 1
        Next

        ' Now do the rest of the rows.
        For i As Integer = 0 To displayReportGridview.Rows().Count() - 1
            counter = 0
            For column As Integer = 5 To (displayReportGridview.Rows(i).Cells().Count() - 1) Step 2
                temp = counter Mod 2
                If temp = 0 Then
                    displayReportGridview.Rows(i).Cells(column).BackColor = Drawing.Color.WhiteSmoke
                    displayReportGridview.Rows(i).Cells(column + 1).BackColor = Drawing.Color.WhiteSmoke
                End If
                counter = counter + 1
            Next
        Next

    End Sub

    Private Sub BindItemsPerOrderReport()
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "GetItemsPerOrder"

        cmd.Connection.Open()

        Dim dt As DataTable = New DataTable
        dt.Load(cmd.ExecuteReader)
        GenerateItemGvColumns(dt.Columns)

        If IsNothing(ViewState("SortExpression")) Then
            ViewState("SortExpression") = "Agent"
        End If

        If IsNothing(ViewState("SortDirection")) Then
            ViewState("SortDirection") = "ASC"
        End If

        If _gvSorting Then
            ViewState("SortExpression") = _sortExpression
            ViewState("SortDirection") = If(TryCast(ViewState("SortDirection"), String) = "ASC", "DESC", "ASC")
        End If

        dt.DefaultView.Sort = ViewState("SortExpression").ToString & " " & ViewState("SortDirection").ToString
        
        displayReportGridview.DataSource = dt
        displayReportGridview.DataBind()

        cmd.Connection.Close()

    End Sub

    Private Sub BindUnscrapedOrdersReport()
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "GetUnscrapedOrders"

        cmd.Connection.Open()

        Dim dt As DataTable = New DataTable
        dt.Load(cmd.ExecuteReader)

        displayReportGridview.DataSource = dt
        displayReportGridview.DataBind()

        cmd.Connection.Close()
    End Sub

    Private Sub BindMDNsWithoutOrders()

        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        Dim dt As New DataTable

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "GetMDNsWithoutOrders"

        adapter.SelectCommand = cmd
        adapter.Fill(dt)

        For Each row As DataRow In dt.Rows
            row.Item("Balance") = FormatCurrency(row.Item("Balance"), 2)
        Next

        displayReportGridview.DataSource = dt
        displayReportGridview.DataBind()

    End Sub

    Private Sub RemoveItemsReportColumns()
        displayReportGridview.AutoGenerateColumns = True
        displayReportGridview.AllowSorting = False
        displayReportGridview.Columns.Clear()
    End Sub

    Protected Sub displayReportGridview_DataBound(sender As Object, e As System.EventArgs) Handles displayReportGridview.DataBound
        If displayReportGridview.Rows.Count() = 0 Then
            downloadBtnDiv.Visible = False
        Else
            downloadBtnDiv.Visible = True
        End If
    End Sub

    Protected Sub displayReportGridview_PageIndexChanging(sender As Object, e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles displayReportGridview.PageIndexChanging
        displayReportGridview.PageIndex = e.NewPageIndex

        DirectCalls()
    End Sub

    Protected Sub displayReportGridview_Sorting(sender As Object, e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles displayReportGridview.Sorting
        _sortExpression = e.SortExpression
        _gvSorting = True
        DirectCalls()
    End Sub

    Private Sub GenerateItemGvColumns(ByVal cols As DataColumnCollection)

        If displayReportGridview.Columns.Count > 0 Then
            Exit Sub
        End If

        displayReportGridview.AutoGenerateColumns = False
        displayReportGridview.AllowSorting = True
        Dim column As BoundField = Nothing

        For Each col As DataColumn In cols
            column = New BoundField()
            column.HeaderText = col.ColumnName
            column.DataField = col.ColumnName
            column.SortExpression = col.ColumnName

            displayReportGridview.Columns.Add(column)
        Next

    End Sub

    Protected Sub downloadReportBtn_ServerClick(sender As Object, e As System.EventArgs) Handles downloadReportBtn.ServerClick

        Dim xlExporter As DataTableToExcel = New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        DirectCalls()

        xlExporter.DataTable = displayReportGridview.DataSource()
        
        Select Case selectReportDropdown.SelectedValue 
            Case "minPerCycle"
		        xlExporter.FileName = "Minutes Per Cycle Report"
                ' Client side code uses this value to determine when excel download completes.
                ' Client side code deletes the cookie when done.
                Response.AppendCookie(New HttpCookie("fileDownloadToken", downloadTokenHdnFld.Value))
	        Case "itemsPerOrder"
		        xlExporter.FileName = "Items Per Order Report"
	        Case "unscrapedOrders"
		        xlExporter.FileName = "Unlinked Orders Report"
            Case "mdnWithoutOrders"
                xlExporter.FileName = "MDN Without Orders Report"
            Case "esnStatus"
                xlExporter.FileName = "ESN Status"
            Case "verRpt"
                xlExporter.FileName = "Verizon Report"
            Case "totalMin"
                xlExporter.FileName = "Total Minutes By Date"
            Case Else
                Exit Sub
        End Select

        xlExporter.Export

    End Sub
    Protected Sub BindVerRprt()
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "Ver_Rpt"
        cmd.CommandTimeout = 0


        cmd.Connection.Open()

        Dim dt As DataTable = New DataTable
        dt.Load(cmd.ExecuteReader)
        GenerateItemGvColumns(dt.Columns)

        displayReportGridview.DataSource = dt
        displayReportGridview.DataBind()

        cmd.Connection.Close()
    End Sub

    Protected Sub BindTotalMinByDate()
        Dim cmd As SqlCommand = New SqlCommand

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("verizonConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "TotalsByDateRange"
        cmd.CommandTimeout = 0


        If ToDate.Text.Length <= 0 Or FromDate.Text.Length <= 0 Then
            ToDate.Text = DateTime.Today.ToShortDateString()
            FromDate.Text = DateTime.Today.AddDays(-30).ToShortDateString()
        End If

        If CellNum.Text.Length > 0 Then
            cmd.Parameters.AddWithValue("@billingNum", CellNum.Text)
        End If

        cmd.Parameters.AddWithValue("@startdate", DateTime.Parse(FromDate.Text))
        cmd.Parameters.AddWithValue("@enddate", DateTime.Parse(ToDate.Text))


        cmd.Connection.Open()

        Dim dt As DataTable = New DataTable
        dt.Load(cmd.ExecuteReader)
        GenerateItemGvColumns(dt.Columns)
        displayReportGridview.AllowSorting = True

        If IsNothing(ViewState("SortExpression")) Then
            ViewState("SortExpression") = "Earliest"
        End If

        If IsNothing(ViewState("SortDirection")) Then
            ViewState("SortDirection") = "ASC"
        End If

        If _gvSorting Then
            ViewState("SortExpression") = _sortExpression
            ViewState("SortDirection") = If(TryCast(ViewState("SortDirection"), String) = "ASC", "DESC", "ASC")
        End If

        dt.DefaultView.Sort = ViewState("SortExpression").ToString & " " & ViewState("SortDirection").ToString

        displayReportGridview.DataSource = dt
        displayReportGridview.DataBind()

        cmd.Connection.Close()
    End Sub


    Protected Sub go_Click(sender As Object, e As EventArgs) Handles go.Click
        BindTotalMinByDate()
       
    End Sub
End Class