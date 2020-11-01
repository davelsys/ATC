Imports Microsoft.VisualBasic

Public MustInherit Class Charge

    Protected varOrderId As Integer = 0
    Protected varCellNum As String = ""
    Protected varAuthCode As String = ""
    Protected varAuthMsg As String = ""

    Protected varUser As String = ""
    Protected varAgent As String = ""
    Protected varBillingInfoChanged As String = ""

    Protected varMonthlyAmnt As Decimal = 0
    Protected varCashAmnt As Decimal = 0
    Protected varIntlAmnt As Decimal = 0
    Protected varItemAmnt As Decimal = 0

    Protected varMiscName As String = ""
    Protected varMiscCost As Decimal = 0

    Public varTotal As Decimal = 0

    Protected varMonthlyPlanId As Integer = 0
    Protected varCashPlanId As Integer = 0

    Protected varTransactionId As Integer = 0
    Protected varCharged As Boolean = False

    Public Overridable Property OrderId() As Integer
        Get
            Return varOrderId
        End Get
        Set(value As Integer)
            varOrderId = value
        End Set
    End Property

    Public Property CellNumber() As String
        Get
            Return varCellNum
        End Get
        Set(value As String)
            varCellNum = value
        End Set
    End Property

    Public ReadOnly Property AuthMessage() As String
        Get
            Return varAuthMsg
        End Get
    End Property

    Public ReadOnly Property AuthCode() As String
        Get
            Return varAuthCode
        End Get
    End Property

    Public Property User() As String
        Get
            Return varUser
        End Get
        Set(ByVal value As String)
            varUser = value
        End Set
    End Property

    Public Property Agent() As String
        Get
            Return varAgent
        End Get
        Set(value As String)
            varAgent = value
        End Set
    End Property

    Public Property InfoChanged() As String
        Get
            Return varBillingInfoChanged
        End Get
        Set(value As String)
            varBillingInfoChanged = value
        End Set
    End Property

    Public Property MonthlyAmnt() As Decimal
        Get
            Return varMonthlyAmnt
        End Get
        Set(value As Decimal)
            varMonthlyAmnt = value
        End Set
    End Property

    Public Property CashAmnt() As Decimal
        Get
            Return varCashAmnt
        End Get
        Set(value As Decimal)
            varCashAmnt = value
        End Set
    End Property

    Public Property IntlAmnt() As Decimal
        Get
            Return varIntlAmnt
        End Get
        Set(value As Decimal)
            varIntlAmnt = value
        End Set
    End Property

    Public Property ItemAmnt() As Decimal
        Get
            Return varItemAmnt
        End Get
        Set(value As Decimal)
            varItemAmnt = value
        End Set
    End Property

    Public Property MonthlyPlanId() As Integer
        Get
            Return varMonthlyPlanId
        End Get
        Set(value As Integer)
            varMonthlyPlanId = value
        End Set
    End Property

    Public Property CashPlanId() As Integer
        Get
            Return varCashPlanId
        End Get
        Set(value As Integer)
            varCashPlanId = value
        End Set
    End Property

    Public Property MiscellaneousName() As String
        Get
            Return varMiscName
        End Get
        Set(ByVal value As String)
            varMiscName = value
        End Set
    End Property

    Public Property MiscellaneousCost() As Decimal
        Get
            Return varMiscCost
        End Get
        Set(ByVal value As Decimal)
            varMiscCost = value
        End Set
    End Property


    Public ReadOnly Property hasCharged() As Boolean
        Get
            Return varCharged
        End Get
    End Property

    Public ReadOnly Property TransactionId() As Integer
        Get
            Return varTransactionId
        End Get
    End Property


    Protected Sub SetTotal()
        varTotal = varMonthlyAmnt + varCashAmnt + varIntlAmnt + varItemAmnt + varMiscCost
    End Sub

    Protected Function GetStrValue(ByRef rdr As SqlDataReader, ByRef Field As String) As String
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            Return ""
        Else
            Return rdr.GetValue(rdr.GetOrdinal(Field)).ToString()
        End If
    End Function

    Protected Function GetIntValue(ByRef rdr As SqlDataReader, ByRef Field As String) As Int32
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            Return 0
        Else
            Return rdr.GetValue(rdr.GetOrdinal(Field))
        End If
    End Function

    Protected Function SetAuthErrorMessage(ByRef AuthMsg As String, ByRef transid As Int32) As Boolean

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim failedAuthSql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transid
        Dim failedAuthCmd = New SqlCommand(failedAuthSql, con)

        failedAuthCmd.ExecuteNonQuery()

        con.Close()

        Return False

    End Function

    Protected Sub insertErrorDetail(ByVal clientid As Integer, ByVal ccnum As String, ByVal ccexp As String, ByVal ccCode As String, _
                                  ByVal amount As String, ByVal err As String, ByVal errorLocation As String)

        Dim tm As String = DateTime.Now.ToString()

        Dim strSql As String = "INSERT ErrorLog (clientId, CCnum, CCexp, CCcode, amount, error, errorLocation, errorTime) "
        strSql &= "VALUES(" & clientid & ", "
        strSql &= "'" & ccnum & "', "
        strSql &= "'" & ccexp & "', "
        strSql &= "'" & ccCode & "', "
        strSql &= "'" & amount & "', "
        strSql &= "'" & err.Replace("'", "''") & "', "
        strSql &= "'" & errorLocation & "', "
        strSql &= "'" & tm & "') "

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim comm As SqlCommand = New SqlCommand(strSql, con)

        con.Open()
        comm.ExecuteNonQuery()
        con.Close()

    End Sub
    Protected Function WriteTrace(ByVal Msg As String) As Boolean

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Msg = Msg.Replace("'", "''")

        Dim Sql As String = "Exec WriteTrace '" & Msg & "'"
        Dim TraceCmd = New SqlCommand(Sql, con)

        TraceCmd.ExecuteNonQuery()

        con.Close()

    End Function
End Class
