Imports Microsoft.VisualBasic

Public Class CreditAgentAccountCharge
    Inherits Charge

    Private varCCNumber As String = ""
    Private varCCExpiration As String = ""
    Private varCCCode As String = ""

    Public Property CCNumber() As String
        Get
            Return varCCNumber
        End Get
        Set(value As String)
            varCCNumber = value
        End Set
    End Property

    Public Property CCExpiration() As String
        Get
            Return varCCExpiration
        End Get
        Set(value As String)
            varCCExpiration = value
        End Set
    End Property

    Public Property CCCode() As String
        Get
            Return varCCCode
        End Get
        Set(value As String)
            varCCCode = value
        End Set
    End Property

    Public Sub RunCharge()


        ' This needs to be called for the total to be accurate
        SetTotal()

        Dim svc1 = New svc.Service()

        Dim con As SqlConnection =
            New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim rdr As SqlDataReader = Nothing
        Dim cmdstr As New StringBuilder

        ' When no cash total - then no transaction - just update customer profile
        If (varTotal > 0) Then
            cmdstr.Append("insert into authtrans ")
            cmdstr.Append("( trans_type, paydate, total, ")
            cmdstr.Append(" agent, [User], AuthCode, ")
            cmdstr.Append("authtransid, authmessage, charged, cc_last_four, cc_expiration, ")
            cmdstr.Append("billing_fname, billing_lname, ")
            cmdstr.Append("billing_address,billing_city,billing_state, ")
            cmdstr.Append("billing_zip,billing_phone,billing_email,auth_resp ) ")
            cmdstr.Append("OUTPUT inserted.transid ")
            cmdstr.Append("values ( 3, getdate(), 0, @Agent, @User, @AuthCode, ")
            cmdstr.Append("@AuthTransId, @AuthMsg, @Charged, @CCLastFour, @CCExp, ")
            cmdstr.Append("@BillFName, @BillLName, @BillAddress, @BillCity, @BillState, @BillZip, ")
            cmdstr.Append("@BillPhone, @BillEmail, @AuthResponse ); ")

            Dim cmd0 As New SqlCommand(cmdstr.ToString, con)

            cmd0.Parameters.Add("@Agent", SqlDbType.VarChar).Value = varAgent
            cmd0.Parameters.Add("@User", SqlDbType.VarChar).Value = varUser
            cmd0.Parameters.Add("@AuthCode", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@AuthTransId", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@AuthMsg", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@Charged", SqlDbType.Bit).Value = 0
            cmd0.Parameters.Add("@CCLastFour", SqlDbType.VarChar).Value = varCCNumber.Substring(varCCNumber.Length() - 4)
            cmd0.Parameters.Add("@CCExp", SqlDbType.VarChar).Value = varCCExpiration
            cmd0.Parameters.Add("@BillFName", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@BillLName", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@BillAddress", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@BillCity", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@BillState", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@BillZip", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@BillPhone", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@BillEmail", SqlDbType.VarChar).Value = ""
            cmd0.Parameters.Add("@AuthResponse", SqlDbType.VarChar).Value = ""

            Dim insertTransReader = cmd0.ExecuteReader()

            If insertTransReader.HasRows Then
                insertTransReader.Read()
                varTransactionId = insertTransReader.Item("transid")
            End If

            ' Close the reader
            insertTransReader.Close()

        End If  'Vartotal  >  0

        Try
            cmdstr.Clear()
            cmdstr.Append("SELECT * FROM AgentsCCInfo WHERE UserId = @UserId; ")

            Dim cmd As New SqlCommand(cmdstr.ToString, con)
            cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = New Guid(Membership.GetUser(varAgent).ProviderUserKey.ToString())

            rdr = cmd.ExecuteReader()
            If Not rdr.Read() Or Not rdr.HasRows() Then
                rdr.Close()
                varAuthMsg = "Invalid Agent Info"
                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                Exit Sub
            End If

        Catch ex As Exception
            If Not IsNothing(rdr) AndAlso Not rdr.IsClosed() Then
                rdr.Close()
            End If
            varAuthMsg = ex.Message
            SetAuthErrorMessage(varAuthMsg, varTransactionId)
            Exit Sub
        End Try

        Dim merchant = New svc.MerchantAuthenticationType()
        merchant.name = ConfigurationManager.ConnectionStrings("authnet.Merchant").ConnectionString
        merchant.transactionKey = ConfigurationManager.ConnectionStrings("authnet.Key").ConnectionString

        Dim id As Integer = GetIntValue(rdr, "Id")
        Dim profId As Integer = GetIntValue(rdr, "AuthCustProfId")
        Dim payprofid As Integer = GetIntValue(rdr, "AuthPayProfId")

        '7/3/12 cmb added these 10 variables
        Dim profDescription As String = GetStrValue(rdr, "BillingLName")
        Dim profEmail As String = GetStrValue(rdr, "BillingEmail")
        Dim profMerchCustId As String = GetStrValue(rdr, "Id")
        Dim profFirstName As String = GetStrValue(rdr, "BillingFName")
        Dim profLastName As String = GetStrValue(rdr, "BillingLName")
        Dim profAddress As String = GetStrValue(rdr, "BillingAddress")
        Dim profCity As String = GetStrValue(rdr, "BillingCity")
        Dim profState As String = GetStrValue(rdr, "BillingState")
        Dim profZip As String = GetStrValue(rdr, "BillingZip")
        Dim profPhoneNumber As String = GetStrValue(rdr, "BillingPhone")

        rdr.Close()

        Dim custprof = New svc.CustomerProfileType()
        Dim custprofresp = New svc.CreateCustomerProfileResponseType()
        Dim payprof As New svc.CustomerPaymentProfileType()

        Dim isNewCust As Boolean = False 'cmb 7/5/12

        If profId = 0 Then

            isNewCust = True

            custprof.description = profDescription
            custprof.email = profEmail
            custprof.merchantCustomerId = profMerchCustId
            custprofresp = svc1.CreateCustomerProfile(merchant, custprof, svc.ValidationModeEnum.none)

            If Not custprofresp.messages(0).code = "I00001" Then

                If custprofresp.messages(0).code = "E00039" Then

                    varAuthMsg = "Duplicate Customer"

                    'if the error returned is "Duplicate Customer", try with "1" added to last name
                    custprof.description = profDescription & "1"
                    custprofresp = svc1.CreateCustomerProfile(merchant, custprof, svc.ValidationModeEnum.none)

                    If Not custprofresp.messages(0).code = "I00001" Then

                        If custprofresp.messages(0).code = "E00039" Then
                            varAuthMsg = "Duplicate Customer"
                        Else
                            varAuthMsg = custprofresp.messages(0).text
                        End If

                        SetAuthErrorMessage(varAuthMsg, varTransactionId)
                        insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #1a")

                        Exit Sub

                    End If

                Else

                    varAuthMsg = custprofresp.messages(0).text
                    SetAuthErrorMessage(varAuthMsg, varTransactionId)
                    insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #1b")

                End If

            End If

            profId = custprofresp.customerProfileId

            'Update Db: custprof_id
            Dim cmd1 As New SqlCommand("update AgentsCCInfo set AuthCustProfId = " + profId.ToString() + " where id = " + id.ToString(), con)
            Try
                cmd1.ExecuteNonQuery()
            Catch e As Exception
                varAuthMsg = "error - Could Not save custprof"
                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #1 update custprof")
                Exit Sub
            End Try

        End If 'profId = 0

        If payprofid = 0 Then

            isNewCust = True '????????????????

            payprof.billTo = New svc.CustomerAddressType
            payprof.billTo.firstName = profFirstName
            payprof.billTo.lastName = profLastName
            payprof.billTo.address = profAddress
            payprof.billTo.city = profCity
            payprof.billTo.state = profState
            payprof.billTo.zip = profState
            payprof.billTo.phoneNumber = profPhoneNumber

            payprof.payment = New svc.PaymentType
            payprof.payment.Item = New svc.CreditCardType

            payprof.payment.Item.cardNumber = varCCNumber
            payprof.payment.Item.expirationDate = varCCExpiration
            payprof.payment.Item.cardCode = varCCCode

            Dim payprofileresp = New svc.CreateCustomerPaymentProfileResponseType()

            payprofileresp = svc1.CreateCustomerPaymentProfile(merchant, profId, payprof, svc.ValidationModeEnum.none)
            payprofid = payprofileresp.customerPaymentProfileId

            If Not payprofileresp.resultCode = "0" Then
                varAuthMsg = payprofileresp.messages(0).text
                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #2")
                Exit Sub
            End If


            'Update Db: payprof_id
            Dim cmd1 As New SqlCommand("update AgentsCCInfo set AuthPayProfId = " + payprofid.ToString() + " where id = " + id.ToString(), con)
            Try
                cmd1.ExecuteNonQuery()

            Catch e As Exception
                SetAuthErrorMessage(varAuthMsg = "error - Could Not save payprof", varTransactionId)
                insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #2 update payprof")
                Exit Sub
            End Try

        End If 'payprofid = 0

        ' When no cash total - then no transaction 
        If (varTotal > 0) Then
            ' Set profile ids for transaction
            Dim setProfileIds As String = "UPDATE [authtrans] SET [auth_custprof_id] = @CusProfId, "
            setProfileIds &= "[auth_payprof_id] = @PayProfId WHERE [transId] = " & varTransactionId
            Dim profileIdsCmd = New SqlCommand(setProfileIds, con)

            profileIdsCmd.Parameters.Add("@CusProfId", SqlDbType.Int).Value = profId
            profileIdsCmd.Parameters.Add("@PayProfId", SqlDbType.Int).Value = payprofid

            profileIdsCmd.ExecuteNonQuery()
        End If  ' varTotal > 0

        '7/2/12 cmb - update the Auth.net profile only if a customer's data changed and a valid CC number was input. (need whole CC num to update profile)
        'If isNewCust = False And varBillingInfoChanged = "changed" And varCCNumber.IndexOf("*") = -1 Then
        If isNewCust = False And varCCNumber.IndexOf("*") = -1 Then ' And varCCNumber.Length > 10

            'update customer profile
            Dim updProfile = New svc.CustomerProfileExType
            updProfile.customerProfileId = profId
            updProfile.merchantCustomerId = profId
            updProfile.description = profDescription
            updProfile.email = profEmail

            Dim updCustprofresp = New svc.UpdateCustomerProfileResponseType()
            updCustprofresp = svc1.UpdateCustomerProfile(merchant, updProfile)

            If (Not updCustprofresp.messages(0).code = "I00001") Then
                SetAuthErrorMessage(varAuthMsg = updCustprofresp.messages(0).text, varTransactionId)
                insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #3")
                Exit Sub
            End If

            'update payment profile
            Dim updPayProf As New svc.CustomerPaymentProfileExType()
            updPayProf.billTo = New svc.CustomerAddressType
            updPayProf.billTo.firstName = profFirstName
            updPayProf.billTo.lastName = profLastName
            updPayProf.billTo.address = profAddress
            updPayProf.billTo.city = profCity
            updPayProf.billTo.state = profState
            updPayProf.billTo.zip = profState
            updPayProf.billTo.phoneNumber = profPhoneNumber

            updPayProf.payment = New svc.PaymentType
            updPayProf.payment.Item = New svc.CreditCardType

            'If varCCNumber.IndexOf("*") = -1 Then
            updPayProf.payment.Item.cardNumber = varCCNumber
            'End If

            updPayProf.payment.Item.expirationDate = varCCExpiration
            updPayProf.payment.Item.cardCode = varCCCode

            updPayProf.customerPaymentProfileId = payprofid

            Dim updPayProfileResp = New svc.UpdateCustomerPaymentProfileResponseType()
            updPayProfileResp = svc1.UpdateCustomerPaymentProfile(merchant, profId, updPayProf, svc.ValidationModeEnum.none)

            If Not updPayProfileResp.messages(0).code = "I00001" Then
                varAuthMsg = updPayProfileResp.messages(0).text
                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #4")
                Exit Sub
            End If

            varBillingInfoChanged = ""

        End If

        ' When no cash total - then no transaction - just update payment profile
        If (varTotal > 0) Then
            Dim authpay = New svc.ProfileTransactionType
            authpay.Item = New svc.ProfileTransAuthCaptureType
            authpay.Item.amount = varTotal
            authpay.Item.cardCode = varCCCode
            authpay.Item.customerProfileId = profId
            authpay.Item.customerPaymentProfileId = payprofid

            Dim authresp = New svc.CreateCustomerProfileTransactionResponseType
            authresp = svc1.CreateCustomerProfileTransaction(merchant, authpay, "") ', "x_duplicate_window=1" -- parameter how many minutes to lock account between transactions. default=2

            If Not authresp.messages(0).code = "I00001" Then
                varAuthMsg = authresp.messages(0).text
                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #5")
                Exit Sub
            End If

            Dim resparr() As String
            resparr = Split(authresp.directResponse, ",")

            If resparr.Length() > 6 Then

                varAuthCode = resparr(4).ToString() ' authresp.messages(0).text 'resparr(3)
                varAuthMsg = resparr(3)  'authresp.directResponse    'resparr(3)

                If authresp.resultCode = 0 Then
                    varCharged = True
                Else
                    varCharged = False
                    varTotal = 0
                End If

                Dim successSql As StringBuilder = New StringBuilder
                successSql.Append("UPDATE [authtrans] SET [charged] = @Charged, ")
                successSql.Append("[total] = @Total, ")
                successSql.Append("[authcode] = @AuthCode, ")
                successSql.Append("[authtransid] = @AuthTransId, ")
                successSql.Append("[authmessage] = @AuthMsg, ")
                successSql.Append("[auth_resp] = @AuthResponse ")

                successSql.Append("WHERE [transid] = @TransID ")

                Dim cmd3 As New SqlCommand(successSql.ToString(), con)

                cmd3.Parameters.Add("@Charged", SqlDbType.Bit).Value = varCharged
                ' Important: Agent credits are determined by their negative value
                cmd3.Parameters.Add("@Total", SqlDbType.Money).Value = (0 - varTotal)
                cmd3.Parameters.Add("@TransID", SqlDbType.Int).Value = varTransactionId

                cmd3.Parameters.Add("@AuthCode", SqlDbType.VarChar).Value = resparr(4).ToString()
                cmd3.Parameters.Add("@AuthTransId", SqlDbType.VarChar).Value = resparr(6).ToString()
                cmd3.Parameters.Add("@AuthMsg", SqlDbType.VarChar).Value = resparr(3).ToString()

                cmd3.Parameters.Add("@AuthResponse", SqlDbType.VarChar).Value = authresp.directResponse

                cmd3.ExecuteNonQuery()

            Else
                SetAuthErrorMessage(varAuthMsg = authresp.messages(0).text, varTransactionId)
                rdr.Close()
                insertErrorDetail(id, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #6")
            End If
        Else
            varAuthMsg = ""
        End If   'varTotal > 0

        con.Close()

    End Sub

End Class
