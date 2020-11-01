Imports System.IO
Class TelcoResp

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
            BindtelcoRespGrid()
            'getPinsSummary()
        End If

        ' When a user double clicks a row, javascript postsback
        ' with the eventtarget set to the search button for this page.
        ' See pinsGridview_RowDataBound below.
        If Request.Form.Item("__EVENTTARGET") = "searchPinsBtn" Then
            BindtelcoRespGrid()
        End If

        Dim cbReference As String
        Dim cbScript As String
        cbReference = Page.ClientScript.GetCallbackEventReference(Me, "arg", "getInfoFromServer", "context")
        cbScript = "function UseCallBack(arg, context){" & cbReference & ";}"
        Page.ClientScript.RegisterClientScriptBlock(Me.GetType, "UseCallBack", cbScript, True)

    End Sub

    Private Sub BindtelcoRespGrid()

        Dim sql As New StringBuilder
        Dim cmd As New SqlCommand
        Dim dt As New DataTable
        Dim userLevel As Integer = Session.Item("UserLevel")

        If _gcsManager.IsGSOn Then
            Session("SearchedCell") = _gcsManager.TextBox.Text
        Else
            Session.Remove("SearchedCell")
        End If


        sql.Append("select telreqid,Processed,ReqType,PON,MDN,ESN,reqStatus,ISNULL(NullIF(RespStatus,''),reqstatus)[RespStatus],isnull(NullIF(RespAckMsg,''),ReqAckMsg)[RespAckMsg],")
        sql.Append(" Poll,PollCount,lastpoll ")
        sql.Append(" from Telco.dbo.telcoreq where 1 = 1 ")
        'sql.Append(" join conmdn cm on cr.MDN = cm.mid ")


        ''''''''''SG 01/19/17 add where 1=1 - always is filtered
        ' See if there is any filtering
        'Dim isFilteredAlready As Boolean = False

        If phoneNumberSearchFld.Text.Length > 0 Then
            sql.Append("AND [MDN] LIKE '%' + @PhoneNumber + '%' ")
        End If

        If esnSearchFld.Text.Length > 0 Then
            sql.Append("AND [ESN] LIKE '%' + @Pin + '%' ")
        End If

        Dim selectedPollRadio As String = pollTelcoRadioList.SelectedValue

        If selectedPollRadio = "pollon" Then
            sql.Append("AND [poll] = 1 ")
        ElseIf selectedPollRadio = "polloff" Then
            sql.Append("AND [poll] = 0 ")
        End If

        Dim selectedRadio As String = filterTelcoRadioList.SelectedValue

        If selectedRadio = "open" Then
            sql.Append("AND (processed is null or isnull(RespAckMsg, '') like '%Incomplete%' or isnull(RespAckMsg, '') like '%connection was closed%') ") 'SG 01/24/17
            ' sql.Append("Or ([RespAckMsg] not like '%Completed%'  and [RespAckMsg] not like '%Error%' and [reqtype] != 'UploadESN')")
            'sql.Append("Or ([reqtype] = 'UploadESN' and [ReqAckMsg] not like '%successfully loaded into table%')) ")
        ElseIf selectedRadio = "completed" Then
            sql.Append("AND (processed is not null and isnull([RespAckMsg],'') like '%Successful%' ) ")
        ElseIf selectedRadio = "error" Then
            sql.Append("AND (isnull([RespStatus],'') like '%Error%'  and isnull(RespAckMsg, '') not like '%connection was closed%') ")
        End If

        sql.Append("ORDER BY telreqid DESC ")

        telcoRespGridviewDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        telcoRespGridviewDataSource.SelectCommand = sql.ToString()
        telcoRespGridviewDataSource.CancelSelectOnNullParameter = False

        telcoRespGridview.DataBind()

    End Sub


    Protected Sub telcoRespGridview_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles telcoRespGridview.RowDataBound

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
        Dim theid As Integer = Integer.Parse(telcoRespGridview.DataKeys(rowIndex).Value.ToString)

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim action As String = dropdown.SelectedValue
        Dim strSql As String = ""

        If action = "RePoll" Then

            strSql = "UPDATE Telco.dbo.telcoreq SET [poll] = 1, pollcount = 0 WHERE [telreqid] = @telcoid "


            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)
            'cmd.Parameters.Add("Action", SqlDbType.VarChar).Value = action
            cmd.Parameters.Add("@telcoId", SqlDbType.Int).Value = theid
            cmd.ExecuteNonQuery()
            con.Close()


        End If

        If action = "ReSubmit" Then

            strSql = "Exec Telco_ReSubmit " & theid '.ToString()

            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)

            cmd.ExecuteNonQuery()
            con.Close()


        End If

        If action = "Clear" Then

            strSql = "UPDATE Telco.dbo.telcoreq SET [processed] = GetDate() WHERE [telreqid] = @telcoId "

            Dim sql As String = strSql

            Dim cmd As SqlCommand = New SqlCommand(sql, con)
            cmd.Parameters.Add("@telcoId", SqlDbType.Int).Value = theid
            cmd.ExecuteNonQuery()
            con.Close()


        End If

        BindtelcoRespGrid()

    End Sub

    Protected Sub telcoRespGridview_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles telcoRespGridview.PageIndexChanging
        telcoRespGridview.PageIndex = e.NewPageIndex
        BindtelcoRespGrid()
    End Sub

    Protected Sub telcoRespGridview_Sorting(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles telcoRespGridview.Sorting
        BindtelcoRespGrid()
    End Sub

    Protected Sub pollTelcoRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles pollTelcoRadioList.SelectedIndexChanged
        BindtelcoRespGrid()
    End Sub
    Protected Sub filterTelcoRadioList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles filterTelcoRadioList.SelectedIndexChanged
        BindtelcoRespGrid()
    End Sub
    Protected Sub searchBtn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchBtn.Click
        BindtelcoRespGrid()
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