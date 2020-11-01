
Partial Class ViewRenewals
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

        _gcsManager = New GlobalCellSearch(IsPostBack, Session("SearchedCell"), searchRenewCell)
        
        If Not IsPostBack Then
            BindRenewalsGridview()
        End If

        ' When a user double clicks a row, javascript postsback
        ' with the eventtarget set to the search button for this page.
        ' See renewalsGV_RowDataBound below.
        If Request.Form.Item("__EVENTTARGET") = "searchRenewBtn" Then
            BindRenewalsGridview()
        End If

    End Sub

    Private Sub BindRenewalsGridview()

        Dim userLevel As Integer = Session.Item("UserLevel")
        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        Dim dt As New DataTable

        If _gcsManager.IsGSOn Then
            Session("SearchedCell") = _gcsManager.TextBox.Text
        Else
            Session.Remove("SearchedCell")
        End If
        
        ' Note: There is code that depends on the header text of column being 'Cell'
        sql.Append("SELECT Renewals.[cell_num] AS [Cell], ")
        sql.Append("[Renewal Type] = CASE WHEN Renewals.[monthly_auto_renew] = 1 THEN 'Monthly' ")
        sql.Append("WHEN Renewals.[cash_auto_renew] = 1 THEN 'Cash' ")
        sql.Append("WHEN Renewals.[intl_auto_renew] = 1 THEN 'Intl' ELSE '' END, ")

        sql.Append("[Renewal Amount] = CASE WHEN Renewals.[monthly_auto_renew] = 1 THEN '$' + CONVERT(VARCHAR(12), isnull(plans.autopay_cost, [Plans].[monthly_cost]), 1) ") 'SG 01/18/17 get auto_pay cost
        sql.Append("WHEN Renewals.[cash_auto_renew] = 1 THEN '$' +  CONVERT( VARCHAR(12), [Plans].[plan_cost], 1 ) ")
        sql.Append("WHEN Renewals.[intl_auto_renew] = 1 THEN '$' +  CONVERT( VARCHAR(12), Renewals.[intl_amt], 1 ) ELSE NULL END, ")

        sql.Append("[Exp Date] = CASE WHEN Renewals.[monthly_auto_renew] = 1 THEN CONVERT( VARCHAR(8), Renewals.[Planexpdate], 1 ) ELSE NULL END, ")

        sql.Append("'$' + CONVERT( VARCHAR(12), [Balance], 1 ) AS [Balance], CONVERT( VARCHAR(8), [updated], 1 ) AS [Renew On], [status] AS [Status], ")

        sql.Append("[CC] = CASE WHEN RENEWALS.[cc_pay] = 1 THEN 'Yes' Else 'No' END, ")
        sql.Append("[charged] AS [Charged], [assigned] AS [Assigned]")

        sql.Append("FROM Renewals LEFT JOIN [Plans] ")
        sql.Append("ON Renewals.[monthly_plan_id] = [Plans].[planid] ")
        sql.Append("OR Renewals.[cash_plan_id] = [Plans].[planid] ")

        Dim isFiltered As Boolean = False

        If userLevel <> 1 Then
            sql.Append("JOIN orders ON Renewals.[cell_num] = orders.cell_num ")
            sql.Append("JOIN customers ON orders.customer_id = customers.customer_id ")
            sql.Append("WHERE [initial_agent] = '" & Membership.GetUser.UserName & "' " )
            isFiltered = True
        End If

        If searchRenewCell.Text.Length > 0 Then
            If Not isFiltered Then
                sql.Append("WHERE Renewals.[cell_num] like '%' + @CellNum + '%' ")
                isFiltered = True
            Else
                sql.Append("AND Renewals.[cell_num] like '%' + @CellNum + '%' ")
            End If
        End If

        Dim selectedValue As String = filterRenewalsRadioList.SelectedValue

        Select Case selectedValue
            Case "monthly"   
               If isFiltered Then
                    sql.Append("AND Renewals.[monthly_auto_renew] = 1 ")
                Else
                    sql.Append("WHERE Renewals.[monthly_auto_renew] = 1 ")
                End If
            Case "cash" 
                If isFiltered Then
                    sql.Append("AND Renewals.[cash_auto_renew] = 1 ")
                Else
                    sql.Append("WHERE Renewals.[cash_auto_renew] = 1 ")
                End If
            Case "intl" 
                If isFiltered Then
                    sql.Append("AND Renewals.[intl_auto_renew] = 1 ")
                Else
                    sql.Append("WHERE Renewals.[intl_auto_renew] = 1 ")
                End If
            Case Else
                Exit Select
        End Select

        sql.Append("ORDER BY [updated] DESC ")

        renewalsGVDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        renewalsGVDataSource.SelectCommand = sql.ToString()
        renewalsGVDataSource.CancelSelectOnNullParameter = False
        
        renewalsGV.DataBind()

    End Sub

    Protected Sub renewalsGV_DataBound(sender As Object, e As System.EventArgs) Handles renewalsGV.DataBound
        If renewalsGV.Rows.Count() = 0 Then
            downloadBtnDiv.Visible = False
        Else
            downloadBtnDiv.Visible = True
        End If
    End Sub

    Protected Sub renewalsGV_RowDataBound(sender As Object, e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles renewalsGV.RowDataBound
        If e.Row.RowType = DataControlRowType.DataRow Then

            e.Row.Attributes.Add("title", "Double click to globally search by cell number.")

        End If
    End Sub
    
    Protected Sub renewalsGV_PageIndexChanging(sender As Object, e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles renewalsGV.PageIndexChanging
        renewalsGV.PageIndex = e.NewPageIndex
        BindRenewalsGridview()
    End Sub

    Protected Sub renewalsGV_Sorting(sender As Object, e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles renewalsGV.Sorting
        BindRenewalsGridview()
    End Sub

    Protected Sub filterRenewalsRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filterRenewalsRadioList.SelectedIndexChanged
        BindRenewalsGridview()
    End Sub

    Protected Sub searchRenewBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchRenewBtn.Click
        BindRenewalsGridview()
    End Sub
    
    Protected Sub downloadRenewalsBtn_ServerClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles downloadRenewalsBtn.ServerClick
        
        Dim xlExporter As New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        BindRenewalsGridview()

        Dim dv As DataView = New DataView()
        dv = renewalsGVDataSource.Select(DataSourceSelectArguments.Empty)

        xlExporter.DataTable = dv.ToTable
        
        xlExporter.FileName = "Renewals"

        xlExporter.Export

    End Sub

End Class
