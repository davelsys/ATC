Imports Microsoft.VisualBasic

Public Class Customer

    Private cusId As Integer = 0

    Private varCusPin As String = ""
    Private varInitialAgent As String = ""
    Private varPrefix As String = ""
    Private varFirstName As String = ""
    Private varLastName As String = ""
    Private varAddress As String = ""
    Private varCity As String = ""
    Private varState As String = ""
    Private varZip As String = ""
    Private varPhone As String = ""
    Private varEmail As String = ""

    Private varBillingFirstName As String = ""
    Private varBillingLastName As String = ""
    Private varBillingAddress As String = ""
    Private varBillingCity As String = ""
    Private varBillingState As String = ""
    Private varBillingZip As String = ""
    Private varBillingPhone As String = ""
    Private varBillingEmail As String = ""

    Private varCCLastDigits As String = ""
    Private varCCExpiration As String = ""
    Private varAuthCusProfileID As Integer = 0
    Private varAuthPayProfID As Integer = 0

    Public Sub New(ByVal cusId As Integer)
        Me.cusId = cusId
        Init()
    End Sub

    Private Sub Init()
        Dim con As SqlConnection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim sql As String = "SELECT * FROM [customers] WHERE [customer_id] = @CustomerId "

        Dim cmd As SqlCommand = New SqlCommand(sql, con)

        cmd.Parameters.Add("CustomerId", SqlDbType.Int).Value = cusId

        Dim reader As SqlDataReader = cmd.ExecuteReader

        If Not reader.HasRows Then
            Exit Sub
        End If

        reader.Read()

        If reader.Item("cus_pin") IsNot DBNull.Value Then
            varCusPin = reader.Item("cus_pin")
        End If

        If reader.Item("initial_agent") IsNot DBNull.Value Then
            varInitialAgent = reader.Item("initial_agent")
        End If

        If reader.Item("prefix") IsNot DBNull.Value Then
            varPrefix = reader.Item("prefix")
        End If

        If reader.Item("fname") IsNot DBNull.Value Then
            varFirstName = reader.Item("fname")
        End If

        If reader.Item("lname") IsNot DBNull.Value Then
            varLastName = reader.Item("lname")
        End If

        If reader.Item("address") IsNot DBNull.Value Then
            varAddress = reader.Item("address")
        End If

        If reader.Item("city") IsNot DBNull.Value Then
            varCity = reader.Item("city")
        End If

        If reader.Item("state") IsNot DBNull.Value Then
            varState = reader.Item("state")
        End If

        If reader.Item("zip") IsNot DBNull.Value Then
            varZip = reader.Item("zip")
        End If

        If reader.Item("phone") IsNot DBNull.Value Then
            varPhone = reader.Item("phone")
        End If

        If reader.Item("email") IsNot DBNull.Value Then
            varEmail = reader.Item("email")
        End If

        If reader.Item("auth_custprof_id") IsNot DBNull.Value Then
            If reader.Item("billing_fname") IsNot DBNull.Value Then
                varBillingFirstName = reader.Item("billing_fname")
            End If

            If reader.Item("billing_lname") IsNot DBNull.Value Then
                varBillingLastName = reader.Item("billing_lname")
            End If

            If reader.Item("billing_address") IsNot DBNull.Value Then
                varBillingAddress = reader.Item("billing_address")
            End If

            If reader.Item("billing_phone") IsNot DBNull.Value Then
                varBillingPhone = reader.Item("billing_phone")
            End If

            If reader.Item("billing_city") IsNot DBNull.Value Then
                varBillingCity = reader.Item("billing_city")
            End If

            If reader.Item("billing_state") IsNot DBNull.Value Then
                varBillingState = reader.Item("billing_state")
            End If

            If reader.Item("billing_zip") IsNot DBNull.Value Then
                varBillingZip = reader.Item("billing_zip")
            End If

            If reader.Item("billing_email") IsNot DBNull.Value Then
                varBillingEmail = reader.Item("billing_email")
            End If

            If reader.Item("cc_last_four") IsNot DBNull.Value Then
                varCCLastDigits = reader.Item("cc_last_four")
            End If

            If reader.Item("cc_expiration_date") IsNot DBNull.Value Then
                varCCExpiration = reader.Item("cc_expiration_date")
            End If

            If reader.Item("auth_custprof_id") IsNot DBNull.Value Then
                varAuthCusProfileID = reader.Item("auth_custprof_id")
            End If

            If reader.Item("auth_payprof_id") IsNot DBNull.Value Then
                varAuthPayProfID = reader.Item("auth_payprof_id")
            End If

        Else
            varBillingFirstName = varFirstName
            varBillingLastName = varLastName
            varBillingAddress = varAddress
            varBillingCity = varCity
            varBillingState = varState
            varBillingZip = varZip
            varBillingPhone = varPhone
            varBillingEmail = varEmail
        End If

        con.Close()
    End Sub

    ReadOnly Property CusPin() As String
        Get
            Return varCusPin
        End Get
    End Property

    ReadOnly Property InitialAgent() As String
        Get
            Return varInitialAgent
        End Get
    End Property

    ReadOnly Property Prefix() As String
        Get
            Return varPrefix
        End Get
    End Property

    ReadOnly Property FirstName() As String
        Get
            Return varFirstName
        End Get
    End Property

    ReadOnly Property LastName() As String
        Get
            Return varLastName
        End Get
    End Property

    ReadOnly Property Address() As String
        Get
            Return varAddress
        End Get
    End Property

    ReadOnly Property City() As String
        Get
            Return varCity
        End Get
    End Property

    ReadOnly Property State() As String
        Get
            Return varState
        End Get
    End Property

    ReadOnly Property Zip() As String
        Get
            Return varZip
        End Get
    End Property

    ReadOnly Property Phone() As String
        Get
            Return varPhone
        End Get
    End Property

    ReadOnly Property Email() As String
        Get
            Return varEmail
        End Get
    End Property

    ReadOnly Property BillingFName() As String
        Get
            Return varBillingFirstName
        End Get
    End Property

    ReadOnly Property BillingLName() As String
        Get
            Return varBillingLastName
        End Get
    End Property

    ReadOnly Property BillingAddress() As String
        Get
            Return varBillingAddress
        End Get
    End Property

    ReadOnly Property BillingCity() As String
        Get
            Return varBillingCity
        End Get
    End Property

    ReadOnly Property BillingState() As String
        Get
            Return varBillingState
        End Get
    End Property

    ReadOnly Property BillingZip() As String
        Get
            Return varBillingZip
        End Get
    End Property

    ReadOnly Property BillingPhone() As String
        Get
            Return varBillingPhone
        End Get
    End Property

    ReadOnly Property BillingEmail() As String
        Get
            Return varBillingEmail
        End Get
    End Property

    ReadOnly Property CCLastFour() As String
        Get
            Return varCCLastDigits
        End Get
    End Property

    ReadOnly Property CCExpiration() As String
        Get
            Return varCCExpiration
        End Get
    End Property

    ReadOnly Property AuthCusID() As Integer
        Get
            Return varAuthCusProfileID
        End Get
    End Property

    ReadOnly Property AuthPayID() As Integer
        Get
            Return varAuthPayProfID
        End Get
    End Property


    ' Helper functions
    Public Function GetFullName() As String

        Dim fullName As String = varPrefix & " " & varFirstName & " " & varLastName

        If fullName.Length > 20 Then
            fullName = varFirstName & " " & varLastName
            If fullName.Length > 20 Then
                fullName = fullName.Substring(0, 20)
            End If
        End If

        Return StrConv(fullName, VbStrConv.ProperCase)

    End Function

    Public Sub RefreshCustomerInfo()
        Init()
    End Sub

End Class
