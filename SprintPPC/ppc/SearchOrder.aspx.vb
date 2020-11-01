Imports System.Collections.Generic

Partial Class SearchOrder
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

        If Session.Item("UserLevel") = 1 Then
            vendorSelectRadios.Items.FindByValue("Telco").Enabled = True 'SG only admin can use
        Else
            vendorSelectRadios.Items.FindByValue("Telco").Enabled = False
        End If


        If IsPostBack Then
            BindOrderSearchGrid()
        End If

    End Sub

    Private Sub BindOrderSearchGrid()

        Dim level As Integer = Session("UserLevel")
        Dim carrier As String = Nothing
        Dim ver As Boolean

        If level < 1 Or level > 3 Then
            Exit Sub
        End If

        Dim sql As New StringBuilder

        If vendorSelectRadios.SelectedValue.ToLower <> "ver" Then
            ver = False
            sql.Append("SELECT [order_id], [serial_num], fname, lname, cell_num, [esn],")
            sql.Append(" '' [Last Usage],'' [Expiration],'' [MinutesAvailable],'' [CashBalance],'' [Status]")
            sql.Append(" FROM [customers] INNER JOIN [orders] ")
            sql.Append(" ON [customers].[customer_id] = [orders].[customer_id] ")
        Else
            ver = True
            sql.Append(" SELECT [order_id], [serial_num], fname, lname, cell_num, [esn],")
            sql.Append(" (select max(calltime) from verizon.dbo.CDR_NEW c where c.[BILLING NUMBER] = v.mid)[Last Usage],")
            sql.Append(" isnull(MonthExpdate,PlanExpDate)[Expiration],MinutesAvailable,CashBalance,Status")
            sql.Append(" from vermdn v join orders o on o.cell_num = v.mid join customers c on c.customer_id = o.customer_id")
            sql.Append(" left outer join Plans p on o.Plan_Id= p.planid ")
        End If

        ' See if there is any filtering
        Dim isFilteredAlready As Boolean = False

        If cellNumberInput.Text.Length > 0 Then
            sql.Append("WHERE [cell_num] LIKE '%' + @CellNumber + '%' ")
            isFilteredAlready = True
        End If

        If lnameInput.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [lname] LIKE '%' + @LastName + '%' ")
            Else
                sql.Append("WHERE [lname] LIKE '%' + @LastName + '%' ")
                isFilteredAlready = True
            End If
        End If

        If searchSerialNumFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [serial_num] LIKE '%' + @SerialNumber + '%' ")
            Else
                sql.Append("WHERE [serial_num] LIKE '%' + @SerialNumber + '%' ")
                isFilteredAlready = True
            End If
        End If

        If vendorSelectRadios.SelectedValue.ToLower <> "all" Then
            If isFilteredAlready Then
                sql.Append("AND [carrier_name] = @Carrier ")
            Else
                sql.Append("WHERE [carrier_name] = @Carrier ")
                isFilteredAlready = True
            End If
        End If

        If level = 3 Then
            If isFilteredAlready Then
                sql.Append("AND [initial_agent] = @InitialAgent ")
            Else
                sql.Append("WHERE [initial_agent] = @InitialAgent ")
                isFilteredAlready = True
            End If

        End If

        If lnameInput.Text.Length > 0 Then
            sql.Append("ORDER BY lname")
        Else
            If ver Then
                sql.Append("ORDER BY [Last Usage] desc")
            Else
                sql.Append("ORDER BY cell_num")
            End If
        End If

        'Response.Write(sql.ToString())
        'Response.End()

        SqlDataSource1.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        SqlDataSource1.SelectCommandType = SqlDataSourceCommandType.Text

        SqlDataSource1.SelectCommand = sql.ToString()

        SqlDataSource1.SelectParameters.Item("InitialAgent").DefaultValue = Membership.GetUser().UserName

        If vendorSelectRadios.SelectedValue.ToLower = "pp" Then
            carrier = "page plus"
        ElseIf vendorSelectRadios.SelectedValue.ToLower = "ver" Then
            carrier = "verizon"
            'ElseIf vendorSelectRadios.SelectedValue.ToLower = "at" Then
            '    carrier = "all talk"
        ElseIf vendorSelectRadios.SelectedValue.ToLower = "concord" Then
            carrier = "concord"
        ElseIf vendorSelectRadios.SelectedValue.ToLower = "telco" Then
            carrier = "telco"
        ElseIf vendorSelectRadios.SelectedValue.ToLower = "vzpp" Then
            carrier = "VZPP"
        ElseIf vendorSelectRadios.SelectedValue.ToLower = "sprint" Then
            carrier = "sprint"
        End If
        If IsNothing(SqlDataSource1.SelectParameters.Item("Carrier")) Then
            SqlDataSource1.SelectParameters.Add("Carrier", DbType.String, carrier)
        Else
            SqlDataSource1.SelectParameters.Item("Carrier").DefaultValue = carrier
        End If

        For Each col As BoundField In GridView1.Columns
            Select Case col.HeaderText
                Case "ESN"
                    col.Visible = If(level = 1, True, False)
                Case "Last Usage", "Expiration", "Minutes Avail", "Cash Balance", "Status"
                    col.Visible = ver
            End Select
        Next

        SqlDataSource1.CancelSelectOnNullParameter = False

        GridView1.DataBind()

    End Sub

    Protected Sub add_order_btn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles add_order_btn.Click
        Response.Redirect("Order.aspx")
    End Sub

    Protected Sub GridView1_RowDataBound(ByVal sender As Object, ByVal e As GridViewRowEventArgs) Handles GridView1.RowDataBound

        If e.Row.RowType = DataControlRowType.DataRow Then

            Dim row As DataRow = DirectCast(e.Row.DataItem, DataRowView).Row

            Dim hashedId As String = StringEncode(row.Item("order_id"))

            e.Row.Attributes.Add("onclick", "location = 'Order.aspx?oid=" & hashedId & "'")

        End If

    End Sub

    <System.Web.Services.WebMethodAttribute(), System.Web.Script.Services.ScriptMethodAttribute()> 
    Public Shared Function LastNamesCompletion(ByVal prefixText As String, ByVal count As Integer) As String()

        Dim cmd As New SqlCommand
        Dim reader As SqlDataReader
        Dim items As New List(Of String)

        cmd.Connection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        cmd.CommandType = CommandType.Text
        cmd.CommandText = "SELECT DISTINCT [lname] FROM [customers] WHERE [lname] LIKE '%' + @prefix + '%' "
        
        If CType(HttpContext.Current.Session("UserLevel"), Integer) = 3 Then
            cmd.CommandText = cmd.CommandText & "AND initial_agent = @Agent; "
        End If
 
        cmd.Parameters.Add("@prefix", SqlDbType.VarChar).Value = prefixText
        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = Membership.GetUser().UserName

        cmd.Connection.Open()

        reader = cmd.ExecuteReader()

        While reader.Read
            items.Add(reader.Item("lname").ToString.ToLower)
        End While

        cmd.Connection.Close()

        Return items.ToArray

    End Function

    Function StringEncode(ByVal value As String) As String
        Return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))
    End Function

End Class
