Imports Microsoft.VisualBasic

Public Class AgentCharge
    Inherits Charge

    Private varBillingFirstName As String = ""
    Private varBillingLastName As String = ""
    Private varBillingAddress As String = ""
    Private varBillingCity As String = ""
    Private varBillingState As String = ""
    Private varBillingZip As String = ""
    Private varBillingPhone As String = ""
    Private varBillingEmail As String = ""

    Private varAgentCreditLimit As Decimal = 0
    Private varAgentUsed As Decimal = 0

    Public Property BillingFName() As String
        Get
            Return varBillingFirstName
        End Get
        Set(value As String)
            varBillingFirstName = value
        End Set
    End Property

    Public Property BillingLName() As String
        Get
            Return varBillingLastName
        End Get
        Set(value As String)
            varBillingLastName = value
        End Set
    End Property

    Public Property BillingAddress() As String
        Get
            Return varBillingAddress
        End Get
        Set(value As String)
            varBillingAddress = value
        End Set
    End Property

    Public Property BillingCity() As String
        Get
            Return varBillingCity
        End Get
        Set(value As String)
            varBillingCity = value
        End Set
    End Property

    Public Property BillingState() As String
        Get
            Return varBillingState
        End Get
        Set(value As String)
            varBillingState = value
        End Set
    End Property

    Public Property BillingZip() As String
        Get
            Return varBillingZip
        End Get
        Set(value As String)
            varBillingZip = value
        End Set
    End Property

    Public Property BillingPhone() As String
        Get
            Return varBillingPhone
        End Get
        Set(value As String)
            varBillingPhone = value
        End Set
    End Property

    Public Property BillingEmail() As String
        Get
            Return varBillingEmail
        End Get
        Set(value As String)
            varBillingEmail = value
        End Set
    End Property


    ' Functions
    Public Sub RunAgentAccountCharge()

        ' This needs to be called for the total to be accurate
        SetTotal()

        Dim con As SqlConnection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim allAgentCharges = varTotal + GetAgentCreditUsed()

        If allAgentCharges >= GetAgentCreditLimit() Then
            varAuthMsg = "Transaction amount exceeds available credit."
            varCharged = False
            varTotal = 0
        Else
            varAuthMsg = "Transaction successful"
            varCharged = True
        End If

        Dim sql As StringBuilder = New StringBuilder

        sql.Append("INSERT INTO [authtrans] ")
        sql.Append("(orderID, [cell_num], trans_type, paydate, monthly_amt, cash_amt, ")
        sql.Append("intl_amt, item_amt, total, [user], agent,")
        sql.Append("authmessage, charged, billing_fname, billing_lname, ")
        sql.Append("billing_address, billing_city, billing_state,")
        sql.Append("billing_zip, billing_phone, billing_email, month_plan_id, cash_plan_id)")
        sql.Append("VALUES (@OrderID, @CellNum, 2, getdate(), @MonthlyAmt, @CashAmt, @IntlAmt, @ItemAmt, @Total, @User, @Agent, @Msg, @Charged,")
        sql.Append("@bFname, @bLname, @bAddress, @bCity, @bState, @bZip, @bPhone, @bEmail, @MonthPlanID, @CashPlanID);")
        sql.Append("SELECT SCOPE_IDENTITY() AS trans_id; ")

        Dim cmd As SqlCommand = New SqlCommand(sql.ToString, con)

        cmd.Parameters.Add("@OrderID", SqlDbType.VarChar).Value = varOrderId

        cmd.Parameters.Add("@CellNum", SqlDbType.VarChar).Value = varCellNum

        cmd.Parameters.Add("@MonthlyAmt", SqlDbType.VarChar).Value = varMonthlyAmnt

        cmd.Parameters.Add("@CashAmt", SqlDbType.VarChar).Value = varCashAmnt

        cmd.Parameters.Add("@IntlAmt", SqlDbType.VarChar).Value = varIntlAmnt

        cmd.Parameters.Add("@ItemAmt", SqlDbType.VarChar).Value = varItemAmnt

        cmd.Parameters.Add("@Total", SqlDbType.VarChar).Value = varTotal

        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = varAgent

        cmd.Parameters.Add("@User", SqlDbType.VarChar).Value = varUser

        cmd.Parameters.Add("@Msg", SqlDbType.VarChar).Value = varAuthMsg

        cmd.Parameters.Add("@bFname", SqlDbType.VarChar).Value = varBillingFirstName

        cmd.Parameters.Add("@bLname", SqlDbType.VarChar).Value = varBillingLastName

        cmd.Parameters.Add("@bAddress", SqlDbType.VarChar).Value = varBillingAddress

        cmd.Parameters.Add("@bCity", SqlDbType.VarChar).Value = varBillingCity

        cmd.Parameters.Add("@bState", SqlDbType.VarChar).Value = varBillingState

        cmd.Parameters.Add("@bZip", SqlDbType.VarChar).Value = varBillingZip

        cmd.Parameters.Add("@bPhone", SqlDbType.VarChar).Value = varBillingPhone

        cmd.Parameters.Add("@bEmail", SqlDbType.VarChar).Value = varBillingEmail


        cmd.Parameters.Add("@Charged", SqlDbType.Bit).Value = varCharged

        cmd.Parameters.Add("@MonthPlanID", SqlDbType.Int).Value = varMonthlyPlanId
        cmd.Parameters.Add("@CashPlanID", SqlDbType.Int).Value = varCashPlanId

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            varTransactionId = reader.Item("trans_id")
        End If

        con.Close()

    End Sub

    Private Function GetAgentCreditLimit() As Decimal
        Dim creditLimit As Decimal = 0
        Dim con As SqlConnection = 
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim sql As String = "SELECT [CreditLimit] FROM [ASPNETDB].[dbo].[aspnet_Users] WHERE [UserName] = @UserName"

        con.Open()

        Dim cmd As SqlCommand = New SqlCommand(sql, con)
            
        cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = varAgent

        Dim reader As SqlDataReader = cmd.ExecuteReader()

        If reader.HasRows Then
            reader.Read()
            If reader.Item("CreditLimit") IsNot DBNull.Value Then
                creditLimit = reader.Item("CreditLimit")
            End If
        End If

        reader.Close()
        con.Close()

        Return creditLimit

    End Function

    Private Function GetAgentCreditUsed() As Decimal
        Dim con As SqlConnection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As String = "SELECT sum([total]) - (SELECT sum(commissionamount) from [commissions]  WHERE [agent] = @Agent) AS total FROM [authtrans] WHERE [agent] = @Agent AND charged = 1 AND trans_type in (2,3)"

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        cmd.Parameters.Add("@Agent", SqlDbType.VarChar).Value = varAgent

        Dim reader As SqlDataReader = cmd.ExecuteReader

        Dim total As Decimal = 0

        If reader.HasRows Then
            reader.Read()
            If reader.Item("total") IsNot DBNull.Value Then
                total = reader.Item("total")
            End If
        End If

        con.Close()

        Return total

    End Function

End Class