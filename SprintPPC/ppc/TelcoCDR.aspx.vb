
Partial Class TelcoCDR
    Inherits System.Web.UI.Page

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

        BindTelcoCDR()

    End Sub

    Private Sub BindTelcoCDR()

        Dim sql As StringBuilder = New StringBuilder

        sql.Append("SELECT [RECORD ID][REC], [BILLING NUMBER][BILL NUM], [MIN NUM], [DEVICE ID], [CALLTIME] , [OTHER PARTY NUMBER][CALL NUM],[FROM CITY]  + ' ' + [FROM STATE] [ORIGIN],")
        sql.Append(" [TO CITY] + ' ' + [TO STATE] [DEST],[TOTAL SECONDS OF CALL][DUR],[CALLDURMIN][MIN],[NRTRFileId][NRTR]")
        sql.Append(" FROM [telco].[dbo].[CDR_NEW] where calltime > dateadd(yy, -1, getdate()) ")
        sql.Append(" ORDER BY [SEQID] DESC ") 'SG 12/05/2016 only records within the last year

        telcoCdrGVSqlDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        telcoCdrGVSqlDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        telcoCdrGVSqlDataSource.SelectCommand = sql.ToString()
        telcoCdrGVSqlDataSource.CancelSelectOnNullParameter = False

        telcoCdrGV.DataBind()

    End Sub

    Protected Sub downloadTelCdrBtn_Click(sender As Object, e As System.EventArgs) Handles downloadTelCdrBtn.Click

        Dim xlExporter As New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        BindTelcoCDR()

        Dim dv As DataView = telcoCdrGVSqlDataSource.Select(DataSourceSelectArguments.Empty)

        xlExporter.DataTable = dv.ToTable

        xlExporter.FileName = "Telco CDR"

        xlExporter.Export()

    End Sub

End Class