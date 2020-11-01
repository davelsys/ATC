
Partial Class CDRSummary
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

        If Session.Item("UserLevel") <> 1 Then
            FormsAuthentication.SignOut()
            Response.Redirect("~/")
        End If

        If Not IsPostBack Then
            Dim con As SqlConnection
            Dim cmd As SqlCommand

            con = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            con.Open()

            Dim str As String = "Select distinct Accountname from accounts where isnull(active,0) = 1 order by Accountname "
            cmd = New SqlCommand(str, con)
            Dim dtr As SqlDataReader = cmd.ExecuteReader

            dropAccounts.DataSource = dtr
            dropAccounts.DataTextField = "Accountname"
            dropAccounts.DataValueField = "Accountname"
            dropAccounts.DataBind()

            'dropAccounts.SelectedIndex = 0

            con.Close()

            dropAccounts.SelectedIndex = 0
            AccountChange()

        End If
    End Sub

    Protected Sub dropAccounts_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dropAccounts.SelectedIndexChanged
        AccountChange()
    End Sub

    Private Sub AccountChange()
        BindGrid()
    End Sub

    Private Sub BindGrid()
        Dim strB As New StringBuilder
        Dim reader As SqlDataReader
        Dim cmd As SqlCommand
        Dim strCategories As String = ""
        Dim strCategoryCols As String = ""


        Dim strFrom As String = ""
        Dim strTo As String = ""

        'If IsDate(txtFromDate.Text) Then
        '    strFrom = txtFromDate.Text
        'ElseIf IsDate(txtToDate.Text) Then
        '    txtFromDate.Text = txtToDate.Text
        '    strFrom = txtFromDate.Text
        'Else
        '    strFrom = ""
        'End If

        'If IsDate(txtToDate.Text) Then
        '    strTo = Date.Parse(txtToDate.Text) '.AddDays(1)
        'ElseIf IsDate(txtFromDate.Text) Then
        '    txtToDate.Text = Date.Today() 'txtFromDate.Text
        '    strTo = Date.Parse(txtToDate.Text) '.AddDays(1)
        'Else
        '    strTo = ""
        'End If


        Dim conn As New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim strSql As String = "Select distinct description from [PPCData].[dbo].[CDR] where (@accountname is null or accountname=@accountname) order by description "

        conn.Open()
        cmd = New SqlCommand(strSql, conn)
        If dropAccounts.SelectedItem.Text.Length > 0 Then
            cmd.Parameters.Add("@Accountname", SqlDbType.VarChar).Value = dropAccounts.SelectedValue
        Else
            cmd.Parameters.Add("@Accountname", SqlDbType.VarChar).Value = DBNull.Value
        End If
        reader = cmd.ExecuteReader

        If reader.HasRows Then
            While reader.Read
                strCategories = strCategories & reader.Item("description") & "~"

            End While
        End If

        If strCategories.Length > 0 Then
            strCategories = strCategories.Remove(strCategories.Length - 1, 1)
        End If

        strCategoryCols = strCategories.Replace("'", "''")
        strCategoryCols = "[" & strCategoryCols.Replace("~", "]~[") & "]"

        If strCategories.Length > 0 Then
            strCategories = "'" & strCategories.Replace("'", "''") & "'"
        End If

        reader.Close()

        conn.Close()

        If strCategories.Length > 0 Then


        strB.Append("SELECT phoneaccountid ,phoneaccount, description ")
        strB.Append("into #tempD ")
            strB.Append("from [PPCData].[dbo].[CDR] ")

        'If dropCategory.SelectedIndex > 0 Then
        '    strB.Append("and d.category='" & dropCategory.SelectedValue & "' ")
        'End If

        'If strFrom <> "" And strTo <> "" Then
        '    strB.Append("and convert(char(8),d.datescanned,112)   between convert(char(8),convert(datetime,'" & strFrom & "'),112) and convert(char(8),convert(datetime,'" & strTo & "'),112) ")
        'End If


        strB.Append("where 1 =1  ")




        If dropAccounts.SelectedItem.Text.Length > 0 Then
            'strB.Append("and ltrim(rtrim(n.rmbldgid)) = '" & dropBldg.SelectedValue & "' ")
            strB.Append("and ltrim(rtrim(accountname))  = '" & dropAccounts.SelectedValue & "' ")
        End If





        'If dropCategory.SelectedIndex > 0 Then
        '    strB.Append("SELECT   tenantname as [Tenant Name], bldgname as Building, unitid as Unit,nameid as [Name Id],Docs as [" & dropCategory.SelectedValue & "] ")
        '    strB.Append("FROM ")
        '    strB.Append("(SELECT count(id) as Docs, category, nameid, tenantname, bldgname, unitid ")
        '    strB.Append("FROM ")
        '    strB.Append("#tempd group by category, nameid, tenantname, bldgname, unitid ) p order by tenantname ")
        'Else
        strB.Append("if exists(select 1 from #tempD) begin ")
        strB.Append("SELECT  phoneaccount," & strCategoryCols.Replace("~", ",") & " ")
        strB.Append("FROM ")
        strB.Append("(SELECT phoneaccountid, phoneaccount, description ")
        strB.Append("FROM ")
        strB.Append("#tempd) p ")
        strB.Append("PIVOT ( COUNT(phoneaccountid) FOR description IN (" & strCategoryCols.Replace("~", ",") & ") ) AS pvt ")
        strB.Append("ORDER BY pvt.phoneaccount; ")
        strB.Append("end ")
        'End If

        strB.Append("drop table #tempd ")

        'Response.Write(strB.ToString)
        ' Exit Sub

        SqlDataSource1.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        SqlDataSource1.SelectCommandType = SqlDataSourceCommandType.Text
        SqlDataSource1.SelectCommand = strB.ToString
        SqlDataSource1.CancelSelectOnNullParameter = False

        gvSummary.DataBind()
        End If

    End Sub

End Class
