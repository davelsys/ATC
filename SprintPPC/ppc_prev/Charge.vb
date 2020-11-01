﻿Imports Microsoft.VisualBasic

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

    Protected varTotal As Decimal = 0

    Protected varMonthlyPlanId As Integer = 0
    Protected varCashPlanId As Integer = 0

    Protected varTransactionId As Integer = 0
    Protected varCharged As Boolean = False

    Public WriteOnly Property OrderId() As Integer
        Set(value As Integer)
            varOrderId = value
        End Set
    End Property

    Public WriteOnly Property CellNumber() As String
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
    Public WriteOnly Property User() As String
        Set(ByVal value As String)
            varUser = value
        End Set
    End Property

    Public WriteOnly Property Agent() As String
        Set(value As String)
            varAgent = value
        End Set
    End Property

    Public WriteOnly Property InfoChanged() As String
        Set(value As String)
            varBillingInfoChanged = value
        End Set
    End Property

    Public WriteOnly Property MonthlyAmnt() As Decimal
        Set(value As Decimal)
            varMonthlyAmnt = value
        End Set
    End Property

    Public WriteOnly Property CashAmnt() As Decimal
        Set(value As Decimal)
            varCashAmnt = value
        End Set
    End Property

    Public WriteOnly Property IntlAmnt() As Decimal
        Set(value As Decimal)
            varIntlAmnt = value
        End Set
    End Property

    Public WriteOnly Property ItemAmnt() As Decimal
        Set(value As Decimal)
            varItemAmnt = value
        End Set
    End Property

    Public WriteOnly Property MonthlyPlanId() As Integer
        Set(value As Integer)
            varMonthlyPlanId = value
        End Set
    End Property

    Public WriteOnly Property CashPlanId() As Integer
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

        Dim con As SqlConnection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim failedAuthSql As String = "UPDATE [authtrans] SET [authmessage] = '" & AuthMsg & "' WHERE [transid] = " & transid
        Dim failedAuthCmd = New SqlCommand(failedAuthSql, con)

        failedAuthCmd.ExecuteNonQuery()

        con.Close()

        Return False

    End Function


End Class
