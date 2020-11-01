
Partial Class OrderSummary
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

        _gcsManager = New GlobalCellSearch(IsPostBack, Session("SearchedCell"), searchCusByCell)

        ToggleTotalCustomers()

        If Not IsPostBack Then
            BindOrderSummaryGrid()
        End If

        ' When a user double clicks a row, javascript postsback
        ' with the eventtarget set to the search button for this page.
        ' See ordersSummmaryGridview_RowDataBound below.
        If Request.Form.Item("__EVENTTARGET") = "searchCusBtn" Then
            BindOrderSummaryGrid()
        End If
        
        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)
       
    End Sub

    Protected Sub ordersSummmaryGVDataSource_Selecting(sender As Object, e As SqlDataSourceSelectingEventArgs) Handles ordersSummmaryGVDataSource.Selecting
        e.Command.CommandTimeout = 0
    End Sub

    Private Sub BindOrderSummaryGrid()

        Dim sql As New StringBuilder

        If _gcsManager.IsGSOn Then
            Session("SearchedCell") = _gcsManager.TextBox.Text
        Else
            Session.Remove("SearchedCell")
        End If


        If ddlCarrier.SelectedValue.ToLower = "Telco" Then
            ordersSummmaryGVDataSource.SelectCommandType = SqlDataSourceCommandType.StoredProcedure
            ordersSummmaryGVDataSource.SelectCommand = "[Telco_CustReport]"
            ordersSummmaryGVDataSource.CancelSelectOnNullParameter = False
        Else

            ' Note: Client code depends on the header text of column being 'Cell'
            sql.Append("SELECT [cell_num] AS [Cell], CONVERT( VARCHAR(8), [create_date], 1 ) AS [Signup Date], (isnull([fname], '') + ' ' + [lname]) AS [Name], [initial_agent] AS [Agent], ")
            sql.Append("[planname] AS [Plan Name], ")
            If ddlCarrier.SelectedValue.ToLower = "verizon" Or ddlCarrier.SelectedValue.ToLower = "concord" Or ddlCarrier.SelectedValue.ToLower = "telco" Or ddlCarrier.SelectedValue.ToLower = "vzpp" Or ddlCarrier.SelectedValue.ToLower = "sprint" Then
                sql.Append("  [Stacked] AS [Stacked], mdn.[status] as [Status],[ESN] as [ESN], ")

            Else
                sql.Append(" [RatePlan] AS [Vendor Plan], [PINCount] AS [Stacked],[ESN] as [ESN], ")
            End If
            sql.Append("(SELECT '$' + CONVERT( VARCHAR(12), isnull(AutoPay_Cost, [monthly_cost]), 1) FROM [Plans] WHERE [planid] = [orders].[monthly_plan_id]) AS [M], ") 'SG 01/18/17 get auto_pay cost
            sql.Append("(SELECT '$' + CONVERT( VARCHAR(12), [plan_cost], 1 ) FROM [Plans] WHERE [planid] = [orders].[cash_plan_id]) AS [C], ")
            sql.Append("'$' + CONVERT( VARCHAR(12), [intl_amt], 1 ) AS [I], ")
            If ddlCarrier.SelectedValue.ToLower = "verizon" Or ddlCarrier.SelectedValue.ToLower = "concord" Or ddlCarrier.SelectedValue.ToLower = "telco" Or ddlCarrier.SelectedValue.ToLower = "vzpp" Or ddlCarrier.SelectedValue.ToLower = "sprint" Then
                sql.Append("CONVERT( VARCHAR(8), [MonthExpDate], 1) AS [Exp Date], CONVERT( VARCHAR(8), [PlanExpDate], 1 ) AS [Plan Expiration] ")
            ElseIf ddlCarrier.SelectedValue.ToLower = "page plus" Then
                sql.Append("CONVERT( VARCHAR(8), [ExpDate], 1) AS [Exp Date], CONVERT( VARCHAR(8), [PlanExpDate], 1 ) AS [Plan Expiration] ")
            End If
            If ddlCarrier.SelectedValue.ToLower = "concord" Or ddlCarrier.SelectedValue.ToLower = "vzpp" Then
                sql.Append(", mdn.MinutesAvailable [Minutes], mdn.MinutesUpdated [As Of] ")
            End If

            If ddlCarrier.SelectedValue.ToLower = "telco" Then
                sql.Append(", (select isnull(SUM(CALLDURMIN),0) from telco.dbo.cdr_new where [BILLING NUMBER] = mdn.mid and CALLTIME > mdn.planstartdate) as MinUsed ")
            End If

            sql.Append("FROM [orders] INNER JOIN [customers] ON [orders].[customer_id] = [customers].[customer_id] ")
            sql.Append("LEFT OUTER JOIN [Plans] ON [orders].[plan_id] = [Plans].[planid] ")
            If ddlCarrier.SelectedValue.ToLower = "page plus" Then 'if searching for page plus, pull info from mdn table; but if searching for all talk, pull info from atc_mdn table
                sql.Append("LEFT OUTER JOIN [ppc].[dbo].[MDN] mdn ON [orders].[cell_num] = mdn.[PhoneNumber] ")
                'ElseIf ddlCarrier.SelectedValue.ToLower = "all talk" Then
                '    sql.Append("LEFT OUTER JOIN [ppc].[dbo].[ATC_MDN] mdn ON [orders].[cell_num] = mdn.[PhoneNumber] ")
            ElseIf ddlCarrier.SelectedValue.ToLower = "verizon" Then
                sql.Append(" LEFT OUTER JOIN [ppc].[dbo].[VERMDN]  mdn ON [orders].[cell_num] = mdn.[MID]")
            ElseIf ddlCarrier.SelectedValue.ToLower = "concord" Then
                sql.Append(" LEFT OUTER JOIN [ppc].[dbo].[conmdn]  mdn ON [orders].[cell_num] = mdn.[MID]")
            ElseIf ddlCarrier.SelectedValue.ToLower = "telco" Then
                sql.Append(" LEFT OUTER JOIN [ppc].[dbo].[telcomdn]  mdn ON [orders].[cell_num] = mdn.[MID]")
            ElseIf ddlCarrier.SelectedValue.ToLower = "vzpp" Then
                sql.Append(" LEFT OUTER JOIN [ppc].[dbo].[vzppmdn]  mdn ON [orders].[cell_num] = mdn.[MID]")
            ElseIf ddlCarrier.SelectedValue.ToLower = "sprint" Then
                sql.Append(" LEFT OUTER JOIN [ppc].[dbo].[sprintmdn]  mdn ON [orders].[cell_num] = mdn.[MID]")
            End If

            sql.Append("WHERE carrier_name = '" & ddlCarrier.SelectedValue & "' ")

            Dim isFiltered As Boolean = True

            Dim userLevel As Integer = Session.Item("UserLevel")
            If userLevel <> 1 Then
                'sql.Append("WHERE initial_agent = '" & Membership.GetUser().UserName & "' ")
                sql.Append("AND initial_agent = '" & Membership.GetUser().UserName & "' ")
                isFiltered = True
            End If

            If searchCusByCell.Text.Length > 0 Then
                If isFiltered Then
                    sql.Append("AND [orders].[cell_num] LIKE '%' + @CellNum + '%' ")
                Else
                    sql.Append("WHERE [orders].[cell_num] LIKE '%' + @CellNum + '%' ")
                    isFiltered = True
                End If
            End If

            If searchCusByName.Text.Length > 0 Then
                If isFiltered Then
                    sql.Append("AND (isnull([fname], '') + ' ' + [lname]) LIKE '%' + @CusName + '%' ")
                Else
                    sql.Append("WHERE (isnull([fname], '') + ' ' + [lname]) LIKE '%' + @CusName + '%' ")
                End If
            End If

            'Response.Write(sql)
            'Response.End()

            ordersSummmaryGVDataSource.SelectCommandType = SqlDataSourceCommandType.Text
            ordersSummmaryGVDataSource.SelectCommand = sql.ToString()
            ordersSummmaryGVDataSource.CancelSelectOnNullParameter = False
        End If

        ordersSummmaryGridview.DataBind()

    End Sub

    Protected Sub ordersSummmaryGridview_DataBound(sender As Object, e As System.EventArgs) Handles ordersSummmaryGridview.DataBound
        If ordersSummmaryGridview.Rows.Count = 0 Then
            downloadBtnDiv.Visible = False
        Else
            downloadBtnDiv.Visible = True
        End If
    End Sub
    
    Protected Sub ordersSummmaryGridview_RowDataBound(sender As Object, e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles ordersSummmaryGridview.RowDataBound
        If e.Row.RowType = DataControlRowType.DataRow Then

            e.Row.Attributes.Add("title", "Double click to globally search by cell number.")

        End If
    End Sub

    Protected Sub searchCusBtn_Click(sender As Object, e As System.EventArgs) Handles searchCusBtn.Click
        BindOrderSummaryGrid()
    End Sub

    Protected Sub ordersSummmaryGridview_PageIndexChanging(sender As Object, e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles ordersSummmaryGridview.PageIndexChanging
        ordersSummmaryGridview.PageIndex = e.NewPageIndex
        BindOrderSummaryGrid()
    End Sub

    Protected Sub ordersSummmaryGridview_Sorting(sender As Object, e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles ordersSummmaryGridview.Sorting
        BindOrderSummaryGrid()
    End Sub

    Protected Sub downloadCustomersBtn_ServerClick(sender As Object, e As System.EventArgs) Handles downloadCustomersBtn.ServerClick
        
        Dim xlExporter As DataTableToExcel = New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        BindOrderSummaryGrid()

        Dim dv As DataView = New DataView()
        dv = ordersSummmaryGVDataSource.Select(DataSourceSelectArguments.Empty)

        xlExporter.DataTable = dv.ToTable
        
        xlExporter.FileName = "Customers"

        xlExporter.Export

    End Sub

    Private Sub ToggleTotalCustomers()
        If Session("UserLevel") = 1 Then
            totalCustomersDiv.Visible = True
            totalCustomerLbl.InnerHtml = GetTotalCustomers()
        Else
            totalCustomersDiv.Visible = False
        End If
    End Sub

    Private Function GetTotalCustomers(Optional ByVal isAjax As Boolean = False) As String
        Dim cmd As SqlCommand = New SqlCommand
        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)

        cmd.Connection.Open()
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT (SELECT 'VER: ' + convert(varchar,COUNT(*)) FROM [orders] where carrier_name = 'Verizon') + "
        'cmd.CommandText += "(SELECT '   AT:'+ convert(varchar,COUNT(*))  FROM [orders] where carrier_name = 'All Talk') + " 'SG 11/8/16 removed at added con
        cmd.CommandText += "(SELECT '   CON: '+ convert(varchar,COUNT(*))  FROM [orders] where carrier_name = 'Concord')  + "
        cmd.CommandText += "(SELECT '   TEL: '+ convert(varchar,COUNT(*))  FROM [orders] where carrier_name = 'Telco')  + "
        cmd.CommandText += "(SELECT '   PP: ' + convert(varchar,COUNT(*))  FROM [orders] where carrier_name = 'Page Plus') + '<br />' + "
        cmd.CommandText += "(SELECT '   VZPP: ' + convert(varchar,COUNT(*))  FROM [orders] where carrier_name = 'VZPP') + "
        cmd.CommandText += "(SELECT '   SPRINT: ' + convert(varchar,COUNT(*))  FROM [orders] where carrier_name = 'Sprint')  + "
        cmd.CommandText += "(SELECT '   TOTAL: ' + convert(varchar,COUNT(*)) FROM [orders]) As TotalCustomers"

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        Dim totalstr As String = If(reader.Read(), reader.Item("TotalCustomers"), 0)

        cmd.Connection.Close()

        If isAjax Then
            _callBackResult = totalstr
            Return Nothing
        End If

        Return totalstr

    End Function


    ' Handle ajax calls
    Public Sub RaiseCallbackEvent(ByVal eventArgument As String) _
        Implements System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent

        Dim getAction As String = eventArgument.Substring(0, (InStr(eventArgument, ":") - 1))

        If getAction = "refreshCustomerTotal" Then
            GetTotalCustomers(True)
        End If

    End Sub

    Public Function GetCallbackResult() As String _
        Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function
    Protected Sub ddlCarrier_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlCarrier.SelectedIndexChanged
        'Response.Write("selectedindexchanged")
        BindOrderSummaryGrid()
    End Sub

End Class
