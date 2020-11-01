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

        If Not IsPostBack Then
            selectReportDropdown.Attributes.Add("onchange", "blockPage(); ")
        End If

        'This report is based off the "totalPlan" report
        If PlanName.Value.Length > 0 Then
            displayTotalReportDiv.Visible = True
            BindTotalsDetails()
            PlanName.Value = ""
        Else
            displayTotalReportDiv.Visible = False
        End If

        go.Attributes.Add("onclick", "showUpdateProgress();")
        

    End Sub

    Protected Sub selectReportDropdown_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles selectReportDropdown.SelectedIndexChanged
        ' Reset gridview page index when a new report is selected.

        displayReportGridview.PageIndex = 0
        If selectReportDropdown.SelectedValue = "totalPlan" Then
            ClearReportsGridview()
            dateRange.Visible = True
            'ToLbl.Text = DateTime.Today.ToShortDateString()
            'FromLbl.Text = DateTime.Today.AddDays(-30).ToShortDateString()
        Else
            dateRange.Visible = False
        End If
        If selectReportDropdown.SelectedValue = "verRpt" Then
            ClearReportsGridview()
            selecttime.Visible = True
        Else
            selecttime.Visible = False
            'ToLbl.Text = ""
            'FromLbl.Text = ""
        End If

        ViewState("SortExpression") = Nothing

        DirectCalls()

        'Dim strScript As String = "<script language='javascript'>alert('in the event');</script>"
        'ClientScript.RegisterStartupScript(GetType(Page), "", strScript)

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
        ElseIf value = "mismatchedplans" Then
            BindMismatchedReport()
            'ElseIf value = "totalPlan" Then
            'BindTotalMinByDate()
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


        If selectReportDropdown.SelectedValue = "verRpt" Then
            BindVerRprt()
        ElseIf selectReportDropdown.SelectedValue = "totalPlan" Then
            BindTotalMinByDate()
        Else
            DirectCalls()
        End If

    End Sub

    Protected Sub displayReportGridview_Sorting(sender As Object, e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles displayReportGridview.Sorting
        _sortExpression = e.SortExpression
        _gvSorting = True
        'DirectCalls()
        If selectReportDropdown.SelectedValue = "verRpt" Then
            BindVerRprt()
        ElseIf selectReportDropdown.SelectedValue = "totalPlan" Then
            BindTotalMinByDate()
        Else
            DirectCalls()
        End If
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
        'DirectCalls()

        If selectReportDropdown.SelectedValue = "verRpt" Then
            BindVerRprt()
        ElseIf selectReportDropdown.SelectedValue = "totalPlan" Then
            BindTotalMinByDate()
        Else
            DirectCalls()
        End If

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
            Case "totalPlan"
                xlExporter.FileName = "Total Minutes By Plan"
            Case "mismatchedplans"
                xlExporter.FileName = "Mismatched Plans Report"
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
        cmd.Parameters.AddWithValue("@timerange", Integer.Parse(selecttimedropdown.SelectedValue))

        cmd.Connection.Open()

        Dim dt As DataTable = New DataTable
        dt.Load(cmd.ExecuteReader)
        GenerateItemGvColumns(dt.Columns)
        displayReportGridview.DataSource = dt

        displayReportGridview.DataBind()

        cmd.Connection.Close()
    End Sub

    Protected Sub selecttimedropdown_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles selecttimedropdown.SelectedIndexChanged

        If selecttimedropdown.SelectedValue = "0" Then
            Exit Sub
        Else
            BindVerRprt()
        End If

    End Sub

    Protected Sub BindTotalMinByDate()
        Dim cmd As SqlCommand = New SqlCommand

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "GetTotalsPerPlan"
        cmd.CommandTimeout = 0


        'If txtTo.Value.Length <= 0 Or FromLbl.Text.Length <= 0 Then
        '    txtTo.Value = DateTime.Today.ToShortDateString()
        '    'Dim ToLbl As DateTime = DateTime.Today.ToShortDateString()
        '    txtFrom.Value = DateTime.Today.AddDays(-30).ToShortDateString()
        'End If

        'If CellNum.Text.Length > 0 Then
        '    cmd.Parameters.AddWithValue("@billingNum", CellNum.Text)
        'End If

        cmd.Parameters.AddWithValue("@st", DateTime.Parse(txtFrom.Value))
        'cmd.Parameters.AddWithValue("@end", DateTime.Parse(ToLbl.Text))
        cmd.Parameters.AddWithValue("@end", DateTime.Parse(txtTo.Value))

        txtbFrom.Text = txtFrom.Value
        txtbTo.Text = txtTo.Value

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

    Protected Sub displayReportGridview_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles displayReportGridview.RowDataBound

        If selectReportDropdown.SelectedValue = "totalPlan" Then

            If e.Row.RowType = DataControlRowType.DataRow Then

                Dim plan As String = e.Row.Cells(0).Text.ToString()
                Dim row As DataRow = DirectCast(e.Row.DataItem, DataRowView).Row

                e.Row.Attributes.Add("title", "Click to view details")
                e.Row.Attributes.Add("onmouseover", "this.style.cursor = 'pointer';")
                'e.Row.Attributes.Add("onmouseout", "setBackgroundWhite(this);")
                e.Row.Attributes.Add("onclick", "getplan('" & plan & "'); setClickBGColor(this);")
                'e.Row.Attributes.Add("onmouseover", "setClickBGColor(this);")
            End If
        End If
    End Sub
    Protected Sub BindTotalsDetails()
        Dim cmd As SqlCommand = New SqlCommand

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "GetCellPerPlan"
        cmd.CommandTimeout = 0

        ' Dim ToLbl As DateTime = DateTime.Today.ToShortDateString()


        cmd.Parameters.AddWithValue("@st", DateTime.Parse(txtFrom.Value))
        cmd.Parameters.AddWithValue("@end", DateTime.Parse(txtTo.Value))
        'cmd.Parameters.AddWithValue("@end", DateTime.Parse(ToLbl.Text))
        cmd.Parameters.AddWithValue("@plan", PlanName.Value.ToString())

        cmd.Connection.Open()

        Dim dt1 As DataTable = New DataTable
        dt1.Load(cmd.ExecuteReader)
        GenerateItemGvColumns(dt1.Columns)

        TotalDetailGv.DataSource = dt1
        TotalDetailGv.DataBind()

        cmd.Connection.Close()
        PlanName.Value = ""
    End Sub

    Private Sub BindMismatchedReport()

        Dim cmd As SqlCommand = New SqlCommand

        cmd.Connection =
           New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandText = "ver_mismatched_plans_report"

        cmd.Connection.Open()
        cmd.CommandTimeout = 0

        Dim dt As DataTable = New DataTable
        dt.Load(cmd.ExecuteReader)
        GenerateItemGvColumns(dt.Columns)

        If IsNothing(ViewState("SortExpression")) Then
            ViewState("SortExpression") = "Type"
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


End Class
