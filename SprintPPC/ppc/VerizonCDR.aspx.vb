
Partial Class VerizonCDR
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

        BindVerizonCDR()

    End Sub

    Private Sub BindVerizonCDR()

        Dim sql As StringBuilder = New StringBuilder

        'sql.Append("SELECT [RECORD ID], [NET ELEM NO], [CALL DATA RCD DT], [SWCH TYPE IND], [AUTO STRCTR CD] ")
        'sql.Append(",[SWCH NO], [MID], [MIN], [DLD DGT NO], [OPLSD DGT NO], [EQPMT SRL NO/MEID] ")
        'sql.Append(",cdi.[LookupVal] AS [CALL DIRN IND], rsi.[LookupVal] AS [ROMR STAT IND] ")
        'sql.Append(",[SZR DT TM], [SZR DURTN CNT], [ANSW DT TM] , [ANSW DURTN CNT] ,[INIT CELL NO] ")
        'sql.Append(",[FINL CELL NO], [TRK MBR ID], ci.[LookupVal] AS [CCI IND], [MOTO CXR ID] ")
        'sql.Append(",[MOTO CALL TYPE CD], [MOTO MIDN ROLL IND], actc.[LookupVal] AS [AUTO CALL TYPE CD] ")
        'sql.Append(",[AUTO CPN NO], [AUTO OVS IND], [AUTO SVC FEAT CD], [AUTO CXR PRFX CD], [ANSW STAT IND] ")
        'sql.Append(",sfc.[LookupVal] AS [SVC FEAT CD], [NORT SVC FEAT CD], [SIDBID NO] ")
        'sql.Append(",tc.[LookupVal] AS [TERM CD], [CALL DLVRY IND], [FEAT SETUP IND], [THREE WAY CALL IND] ")
        'sql.Append(",[CALL WAIT IND], [BUSY XFER IND], [NO ANSW XFER IND], [CALL FWD IND], [NORT CXR ID] ")
        'sql.Append(",[WORLD NO], [NRTRFileId], [CALLDURMIN] ")
        'sql.Append("FROM [Verizon].[dbo].[CDR] vcdr ")
        'sql.Append("LEFT OUTER JOIN [LT_CALL_DIRN_IND] cdi ON cdi.[LookupNum] = vcdr.[CALL DIRN IND] ")
        'sql.Append("LEFT OUTER JOIN [LT_ROMR_STAT_IND] rsi ON rsi.[LookupNum] = vcdr.[CALL DIRN IND] ")
        'sql.Append("LEFT OUTER JOIN [LT_CCI_IND] ci ON ci.[LookupNum] = vcdr.[CCI IND] ")
        'sql.Append("LEFT OUTER JOIN [LT_AUTO_CALL_TYPE_CD] actc ON actc.[LookupNum] = vcdr.[AUTO CALL TYPE CD] ")
        'sql.Append("LEFT OUTER JOIN [LT_SVC_FEAT_CD] sfc ON sfc.[LookupNum] = vcdr.[SVC FEAT CD] ")
        'sql.Append("LEFT OUTER JOIN [LT_TERM_CD] tc ON tc.[LookupNum] = vcdr.[TERM CD] ")
        'sql.Append("WHERE [CALL DIRN IND] != '5' ")
        'sql.Append("ORDER BY [SEQID] DESC ")

        'sql.Append("SELECT [RECORD ID][REC], [BILLING NUMBER][BILL NUM], [MIN NUM], [DEVICE ID], [CALLTIME] , [OTHER PARTY NUMBER][CALL NUM],[FROM CITY]  + ' ' + [FROM STATE] [ORIGIN],")
        'sql.Append(" [TO CITY] + ' ' + [TO STATE] [DEST],[TOTAL SECONDS OF CALL][DUR],[CALLDURMIN][MIN],[NRTRFileId][NRTR]")
        'sql.Append(" FROM [Verizon].[dbo].[CDR_NEW] vcdr ")
        'sql.Append(" ORDER BY [SEQID] DESC ")

        verizonCdrGVSqlDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("verizonConnectionString").ConnectionString
        verizonCdrGVSqlDataSource.SelectCommandType = SqlDataSourceCommandType.Text
        verizonCdrGVSqlDataSource.SelectCommand = sql.ToString()
        verizonCdrGVSqlDataSource.CancelSelectOnNullParameter = False

        verizonCdrGV.DataBind()

    End Sub

    Protected Sub downloadVerCdrBtn_Click(sender As Object, e As System.EventArgs) Handles downloadVerCdrBtn.Click
        
        Dim xlExporter As New DataTableToExcel

        ' Rebinding the grid to retrieve the DataSource.
        BindVerizonCDR()

        Dim dv As DataView = verizonCdrGVSqlDataSource.Select(DataSourceSelectArguments.Empty)

        xlExporter.DataTable = dv.ToTable
        
        xlExporter.FileName = "Verizon CDR"

        xlExporter.Export

    End Sub

End Class