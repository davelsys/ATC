Imports System.IO
Class ver2

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

        'If Session.Item("UserLevel") <> 1 Then
        'refreshCellNum.Visible = False
        ' End If

        _gcsManager = New GlobalCellSearch(IsPostBack, Session("SearchedCell"), phoneNumberSearchFld)

        If Not IsPostBack Then
            BindverRespGrid()
            'getPinsSummary()
        End If

        ' When a user double clicks a row, javascript postsback
        ' with the eventtarget set to the search button for this page.
        ' See pinsGridview_RowDataBound below.
        If Request.Form.Item("__EVENTTARGET") = "searchPinsBtn" Then
            BindverRespGrid()
        End If

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

    End Sub

    Private Sub BindverRespGrid()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim dt As New DataTable
        Dim userLevel As Integer = Session.Item("UserLevel")

        If _gcsManager.IsGSOn Then
            Session("SearchedCell") = _gcsManager.TextBox.Text
        Else
            Session.Remove("SearchedCell")
        End If


        sql.Append("select verid,Processed,ReqType,PON,MDN,ESN,reqStatus,ISNULL(NullIF(RespStatus,''),reqstatus)[RespStatus],isnull(NullIF(RespAckMsg,''),ReqAckMsg)[RespAckMsg],Poll,PollCount,lastpoll ")
        sql.Append("from verreq ")

        ' See if there is any filtering
        Dim isFilteredAlready As Boolean = False

        If phoneNumberSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [MDN] LIKE '%' + @PhoneNumber + '%' ")
            Else
                sql.Append("WHERE [MDN] LIKE '%' + @PhoneNumber + '%' ")
                isFilteredAlready = True
            End If
        End If

        If esnSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [ESN] LIKE '%' + @Pin + '%' ")
            Else
                sql.Append("WHERE [ESN] LIKE '%' + @Pin + '%' ")
                isFilteredAlready = True
            End If
        End If

        Dim selectedPollRadio As String = pollVerRadioList.SelectedValue

        If selectedPollRadio = "pollon" Then
            If isFilteredAlready Then
                sql.Append("AND [poll] = 1 ")
            Else
                sql.Append("WHERE [poll] = 1 ")
                isFilteredAlready = True
            End If
        ElseIf selectedPollRadio = "polloff" Then
        If isFilteredAlready Then
            sql.Append("AND [poll] = 0 ")
        Else
            sql.Append("WHERE [poll] = 0 ")
            isFilteredAlready = True
            End If
        End If

        Dim selectedRadio As String = filterVerRadioList.SelectedValue

        If selectedRadio = "open" Then
            If isFilteredAlready Then
                sql.Append("AND (([RespAckmsg] is null   and [reqtype] != 'UploadESN')")
            Else
                sql.Append("WHERE (([RespAckmsg] is null   and [reqtype] != 'UploadESN')")
                isFilteredAlready = True
            End If
            sql.Append("Or ([RespAckMsg] not like '%Completed%'  and [RespAckMsg] not like '%Error%' and [reqtype] != 'UploadESN')")
            sql.Append("Or ([reqtype] = 'UploadESN' and [ReqAckMsg] not like '%successfully loaded into table%')) ")


        ElseIf selectedRadio = "completed" Then
            If isFilteredAlready Then
                sql.Append("AND isnull([RespAckMsg],'') like '%Completed%' ")
            Else
                sql.Append("WHERE  isnull([RespAckMsg],'') like '%Completed%' ")
                isFilteredAlready = True
            End If
        ElseIf selectedRadio = "error" Then
            If isFilteredAlready Then
                sql.Append("AND isnull([RespAckMsg],'') like '%Error%'  ")
            Else
                sql.Append("WHERE isnull([RespAckMsg],'') like '%Error%' ")
                isFilteredAlready = True
            End If
        End If

        sql.Append("ORDER BY verid DESC ")

        verRespGridviewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        verRespGridviewDataSource.SelectCommand = sql.ToString()
        verRespGridviewDataSource.CancelSelectOnNullParameter = False

        verRespGridview.DataBind()

    End Sub


    Protected Sub verRespGridview_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles verRespGridview.RowDataBound

        If e.Row.RowType = DataControlRowType.DataRow Then

            Dim row As DataRow = DirectCast(e.Row.DataItem, DataRowView).Row

            e.Row.Attributes.Add("title", "Double click to globally search by cell number.")
            e.Row.Attributes.Add("onmouseover", "this.style.cursor = 'pointer'; this.style.backgroundColor = '#fdecc9';")
            e.Row.Attributes.Add("onmouseout", "setBackgroundWhite(this);")
            ' If Session("UserLevel") = 1 Then
            e.Row.Attributes.Add("onclick", "setClickBGColor(this);")
            'End If

            Dim dropdown As DropDownList = DirectCast(e.Row.Controls(e.Row.Cells().Count() - 1).FindControl("badTransDropdown"), DropDownList)

            'Dim action As String = row.Item("status").ToString()
            'If dropdown IsNot Nothing Then
            'If action.ToLower <> "ok" Then
            dropdown.Visible = True

            'added to hide dropdown when no cell num is there
            ' If row.Item("MDN").ToString() = "" Then
            'dropdown.Visible = False
            'End If
        End If
    End Sub



    Protected Sub badTransDropdown_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim dropdown As DropDownList = DirectCast(sender, DropDownList)

        ' Get the row by going up two levels in the DOM tree.
        Dim row As GridViewRow = DirectCast(dropdown.Parent.Parent, GridViewRow)

        Dim rowIndex As Integer = row.RowIndex
        Dim theid As Integer = Integer.Parse(verRespGridview.DataKeys(rowIndex).Value.ToString)

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim action As String = dropdown.SelectedValue
        Dim strSql As String = ""

        If action = "RePoll" Then

            strSql = "UPDATE [verreq] SET [poll] = 1 WHERE [verid] = @verid "


            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)
            'cmd.Parameters.Add("Action", SqlDbType.VarChar).Value = action
            cmd.Parameters.Add("verId", SqlDbType.Int).Value = theid
            cmd.ExecuteNonQuery()
            con.Close()


        End If

        If action = "ReSubmit" Then

            strSql = "Exec Ver_ReSubmit " + theid.ToString()

            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)

            cmd.ExecuteNonQuery()
            con.Close()


        End If

        If action = "Clear" Then

            strSql = "UPDATE [verreq] SET [processed] = GetDate() WHERE [verid] = @verid "

            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)
            cmd.Parameters.Add("verId", SqlDbType.Int).Value = theid
            cmd.ExecuteNonQuery()
            con.Close()


        End If

        BindverRespGrid()

    End Sub

    Protected Sub verRespGridview_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles verRespGridview.PageIndexChanging
        verRespGridview.PageIndex = e.NewPageIndex
        BindverRespGrid()
    End Sub

    Protected Sub verRespGridview_Sorting(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles verRespGridview.Sorting
        BindverRespGrid()
    End Sub

    Protected Sub pollVerRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles pollVerRadioList.SelectedIndexChanged
        BindverRespGrid()
    End Sub
    Protected Sub filterVerRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filterVerRadioList.SelectedIndexChanged
        BindverRespGrid()
    End Sub
    Protected Sub searchBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchBtn.Click
        BindverRespGrid()
    End Sub



    ' Handle ajax calls
    Public Sub RaiseCallbackEvent(ByVal eventArgument As String) _
        Implements System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent

        Dim getAction As String = eventArgument.Substring(0, (InStr(eventArgument, ":") - 1))

        ' If getAction = "refreshCellNum" Then
        'Dim args As String = eventArgument.Substring(InStr(eventArgument, ":"))
        'getCurrentCellNumForClient(args)
        '  End If

    End Sub

    Public Function GetCallbackResult() As String _
        Implements System.Web.UI.ICallbackEventHandler.GetCallbackResult
        Return _callBackResult
    End Function

    Protected Sub restartVer_Click(ByVal sender As Object, e As EventArgs) Handles restartVer.Click
        Dim connection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim sqlcmd = New SqlCommand
        sqlcmd.Connection = connection
        sqlcmd.CommandText = "Ver_Restart_LTC"
        sqlcmd.CommandType = CommandType.StoredProcedure
        connection.Open()
        sqlcmd.ExecuteNonQuery()


        'sqlcmd = New SqlCommand
        'sqlcmd.CommandText = ""
        'sqlcmd.CommandType = CommandType.StoredProcedure
        'sqlcmd.ExecuteNonQuery()

        connection.Close()
        'Response.Write("In restars btn click function")

    End Sub
End Class