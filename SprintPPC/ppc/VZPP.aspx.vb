Imports System.IO
Class VZPP

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
            BindvzppRespGrid()
            'getPinsSummary()
        End If

        ' When a user double clicks a row, javascript postsback
        ' with the eventtarget set to the search button for this page.
        ' See pinsGridview_RowDataBound below.
        If Request.Form.Item("__EVENTTARGET") = "searchPinsBtn" Then
            BindvzppRespGrid()
        End If

        'pollvzppRadioList()

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

    End Sub

    Private Sub BindvzppRespGrid()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim dt As New DataTable
        Dim userLevel As Integer = Session.Item("UserLevel")

        If _gcsManager.IsGSOn Then
            Session("SearchedCell") = _gcsManager.TextBox.Text
        Else
            Session.Remove("SearchedCell")
        End If


        sql.Append("select vzppreqid, processed, reqtype, mdn, esn, p.planname, vzacct, vzpasswd, VendTransId, respackmsg, poll, pollcount, lastpoll ")

        sql.Append(" from vzpp.dbo.VZPPReq  v ")
        sql.Append(" left join ppc.dbo.plans p on p.planid = v.planid ")
        sql.Append("  where 1=1  ")

        ' See if there is any filtering ''' SG 01/19/17 removed and added where 1=1
        ' Dim isFilteredAlready As Boolean = False

        If phoneNumberSearchFld.Text.Length > 0 Then

            sql.Append("AND [MDN] LIKE '%' + @PhoneNumber + '%' ")

        End If

        If esnSearchFld.Text.Length > 0 Then

            sql.Append("AND [ESN] LIKE '%' + @Pin + '%' ")

        End If

        Dim selectedPollRadio As String = pollvzppRadioList.SelectedValue

        If selectedPollRadio = "pollon" Then

            sql.Append("AND [poll] = 1 ")

        ElseIf selectedPollRadio = "polloff" Then

            sql.Append("AND isnull([poll], 0) = 0 ")

        End If

        Dim selectedRadio As String = pollvzppRadioList.SelectedValue

        If selectedRadio = "open" Then

            sql.Append("AND (processed is null or isnull([RespAckMsg],'') like '%Incomplete%' or isnull([RespAckMsg],'') like '%connection was closed%') ")

            'sql.Append("Or ([RespAckMsg] not like '%Completed%'  and [RespAckMsg] not like '%Error%' and [reqtype] != 'UploadESN')")
            'sql.Append("Or ([reqtype] = 'UploadESN' and [ReqAckMsg] not like '%successfully loaded into table%')) ")


        ElseIf selectedRadio = "completed" Then

            sql.Append("AND processed is not null and (isnull([RespAckMsg],'') like '%Successful%' or isnull([RespAckMsg],'') like '%Complete%') ")


        ElseIf selectedRadio = "error" Then

            sql.Append("AND (isnull([RespStatus],'') like '%Error%' and isnull([RespAckMsg],'') not like '%connection was closed%') ")

        End If

        sql.Append("ORDER BY 1 DESC ")

        ' Response.Write(sql.ToString)
        'Response.End()

        vzppRespGridviewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        vzppRespGridviewDataSource.SelectCommand = sql.ToString()
        vzppRespGridviewDataSource.CancelSelectOnNullParameter = False

        vzppRespGridview.DataBind()

    End Sub


    Protected Sub vzppRespGridview_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles vzppRespGridview.RowDataBound

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
        Dim theid As Integer = Integer.Parse(vzppRespGridview.DataKeys(rowIndex).Value.ToString)

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim action As String = dropdown.SelectedValue
        Dim strSql As String = ""

        If action = "RePoll" Then

            strSql = "UPDATE vzpp.dbo.vzppreq SET [poll] = 1, pollcount = 0 WHERE [vzppreqid] = @vzppreqid "


            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)
            'cmd.Parameters.Add("Action", SqlDbType.VarChar).Value = action
            cmd.Parameters.Add("@vzppreqid", SqlDbType.Int).Value = theid
            cmd.ExecuteNonQuery()
            con.Close()


        End If

        If action = "ReSubmit" Then

            strSql = "Exec VZPP_ReSubmit " + theid.ToString()

            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)

            cmd.ExecuteNonQuery()
            con.Close()


        End If

        If action = "Clear" Then

            strSql = "UPDATE vzpp.dbo.vzppreq SET [processed] = GetDate() WHERE [vzppreqid] = @vzppreqid "

            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)
            cmd.Parameters.Add("@vzppreqid", SqlDbType.Int).Value = theid
            cmd.ExecuteNonQuery()
            con.Close()


        End If

        BindvzppRespGrid()

    End Sub

    Protected Sub vzppRespGridview_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles vzppRespGridview.PageIndexChanging
        vzppRespGridview.PageIndex = e.NewPageIndex
        BindvzppRespGrid()
    End Sub

    Protected Sub vzppRespGridview_Sorting(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles vzppRespGridview.Sorting
        BindvzppRespGrid()
    End Sub

    Protected Sub pollvzppcordRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles pollvzppRadioList.SelectedIndexChanged
        BindvzppRespGrid()
    End Sub
    Protected Sub filtervzppRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filtervzppRadioList.SelectedIndexChanged
        BindvzppRespGrid()
    End Sub
    Protected Sub searchBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchBtn.Click
        BindvzppRespGrid()
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


End Class