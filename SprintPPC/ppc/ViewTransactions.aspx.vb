Imports System.Globalization

Partial Class ViewTransactions
    Inherits System.Web.UI.Page

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

        _gcsManager = New GlobalCellSearch(IsPostBack, Session("SearchedCell"), searchCellFld)

        If Not IsPostBack Then
            BindTransGridView()
        End If

        ' When a user double clicks a row, javascript postsback
        ' with the eventtarget set to the search button for this page.
        ' See transGridView_RowDataBound below.
        If Request.Form.Item("__EVENTTARGET") = "searchTransBtn" Then
            BindTransGridView()
        End If

    End Sub

    Private Sub BindTransGridView()

        Dim userLevel As Integer = Session.Item("UserLevel")
        Dim isFilteredAlready As Boolean = False

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter

         If _gcsManager.IsGSOn Then
            Session("SearchedCell") = _gcsManager.TextBox.Text
        Else
            Session.Remove("SearchedCell")
        End If

        sql.Append("SELECT [cell_num] AS [Cell], ")
        sql.Append("[Type] = CASE [trans_type]  WHEN 1 THEN 'Credit Card' WHEN 2 THEN 'Agent Account' ELSE '' End, ")
        sql.Append("[user] AS [User], [agent] AS [Agent], [paydate] AS [paydate], [monthly_amt], ")
        sql.Append("[cash_amt], [intl_amt], ")
        sql.Append("[item_amt], [authmessage], ")
        sql.Append("[authtransid], [total] ")
        sql.Append("FROM [authtrans] ")

        Dim selectedValue As String = selectTransactionRadioList.SelectedValue
        
        If selectedValue = "cc" Then
            sql.Append("WHERE [trans_type] = 1 ")
            isFilteredAlready = True
        ElseIf selectedValue = "agent" Then
            sql.Append("WHERE [trans_type] = 2 ")
            isFilteredAlready = True
        End If

        If userLevel <> 1 Then
            HideAgentSearchField()
            If Not isFilteredAlready Then
                sql.Append("WHERE [agent] = '" & Membership.GetUser.UserName & "' ")
                isFilteredAlready = True
            Else
                sql.Append("AND [agent] = '" & Membership.GetUser.UserName & "' ")
            End If
        End If

        If searchCellFld.Text.Length > 0 Then
            If Not isFilteredAlready Then
                sql.Append("WHERE [cell_num] like '%' + @CellNumber + '%' ")
                isFilteredAlready = True
            Else
                sql.Append("AND [cell_num] like '%' + @CellNumber + '%' ")
            End If
        End If

        If searchDateFld.Text.Length > 0 Then

            If Not IsNothing(transGridViewDataSource.SelectParameters.Item("PayDate")) Then
                transGridViewDataSource.SelectParameters.RemoveAt(
                    transGridViewDataSource.SelectParameters.IndexOf(
                        transGridViewDataSource.SelectParameters.Item("PayDate")))
            End If
            
            Try
                transGridViewDataSource.SelectParameters.Add("PayDate", Date.Parse(searchDateFld.Text).GetDateTimeFormats("d"c)(3))
                transGridViewDataSource.SelectParameters.Item("PayDate").DbType = DbType.Date
                If Not isFilteredAlready Then
                    sql.Append("WHERE CONVERT(DATE, [paydate]) = @PayDate ")
                    isFilteredAlready = True
                Else
                    sql.Append("AND CONVERT(DATE, [paydate]) = @PayDate ")
                End If
            Catch ex As FormatException
                transGridViewDataSource.SelectParameters.Add("PayDate", searchDateFld.Text)
                transGridViewDataSource.SelectParameters.Item("PayDate").DbType = DbType.String
            	If Not isFilteredAlready Then
                    sql.Append("WHERE CONVERT(VARCHAR, [paydate]) = @PayDate ")
                	isFilteredAlready = True
            	Else
                    sql.Append("AND CONVERT(VARCHAR, [paydate]) = @PayDate ")
            	End If
            End Try
            
        End If

        If searchAgentFld.Text.Length > 0 Then
            If Not isFilteredAlready Then
                sql.Append("WHERE [agent] like '%' + @Agent + '%' ")
                isFilteredAlready = True
            Else
                sql.Append("AND  [agent] like '%' + @Agent + '%' ")
            End If
        End If

        sql.Append("ORDER BY paydate DESC ")

        transGridViewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        transGridViewDataSource.SelectCommand = sql.ToString()
        transGridViewDataSource.CancelSelectOnNullParameter = False

        transGridView.DataBind()

    End Sub

    Protected Sub transGridView_DataBound(sender As Object, e As System.EventArgs) Handles transGridView.DataBound
        If transGridView.Rows.Count() = 0 Then
            downloadBtnDiv.Visible = False
        Else
            downloadBtnDiv.Visible = True
        End If
    End Sub

    Protected Sub transGridView_RowDataBound(sender As Object, e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles transGridView.RowDataBound
        If e.Row.RowType = DataControlRowType.DataRow Then

            e.Row.Attributes.Add("title", "Double click to globally search by cell number.")
            e.Row.Attributes.Add("onmouseover", "this.style.cursor = 'pointer'; this.style.backgroundColor = '#fdecc9';")
            e.Row.Attributes.Add("onmouseout", "this.style.backgroundColor = 'white';")

        End If
    End Sub
    
    Protected Sub transGridView_PageIndexChanging(sender As Object, e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles transGridView.PageIndexChanging
        transGridView.PageIndex = e.NewPageIndex
        BindTransGridView()
    End Sub

    Protected Sub transGridView_Sorting(sender As Object, e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles transGridView.Sorting
        BindTransGridView()
    End Sub

    Protected Sub searchTransBtn_Click(sender As Object, e As System.EventArgs) Handles searchTransBtn.Click
        BindTransGridView()
    End Sub
    
    Protected Sub selectTransactionRadioList_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles selectTransactionRadioList.SelectedIndexChanged
        BindTransGridView()
    End Sub

    Private Sub HideAgentSearchField()
        searchAgentSpan.Attributes.Add("style", "display: none;")
    End Sub

    Protected Sub downloadTransactionBtn_ServerClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles downloadTransactionBtn.ServerClick
        
        Dim xlExporter As DataTableToExcel = New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        BindTransGridView()

        Dim dt As DataTable = CType(transGridViewDataSource.Select(DataSourceSelectArguments.Empty), DataView).ToTable
        Dim dtCopy As DataTable = New DataTable
        Dim dc As DataColumn
        Dim itemArray(dt.Columns.Count - 1) As Object

        ' There is a problem formatting DataTable money columns so copy the DataTable
        ' and format the columns before data is written to it.
        For i As Integer = 0 To (dt.Columns.Count - 1)
            ' Give header text used in the gridview.
            dtCopy.Columns.Add(New DataColumn(transGridView.Columns.Item(i).HeaderText))
        Next

        For Each row As DataRow In dt.Rows
            For i As Integer = 0 To (dt.Columns.Count - 1)
                dc = dt.Columns.Item(i)
                itemArray(i) = If(row(dc) Is DBNull.Value, Nothing, row(dc))
                If dc.DataType Is GetType(Decimal) Then
                    itemArray(i) = FormatCurrency(itemArray(i), 2)
                End If
            Next
            dtCopy.Rows.Add(itemArray)
        Next
        
        xlExporter.DataTable = dtCopy
        
        xlExporter.FileName = "Transactions"

        xlExporter.Export

    End Sub
    
End Class
