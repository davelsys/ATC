﻿Imports Microsoft.VisualBasic

Public Class CreditCardCharge
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
        Dim cmdstr As String

        Dim cclastfour As String = ""
        If varCCNumber.Length >= 4 Then
            varCCNumber.Substring(varCCNumber.Length() - 4)
        End If

        ' When no cash total - then no transaction - just update customer profile
        If (varTotal > 0) Then
            cmdstr = "insert authtrans "
            cmdstr = cmdstr + "(orderid, [cell_num], trans_type,paydate,monthly_amt,cash_amt,"
            cmdstr = cmdstr + "intl_amt, item_amt, total, agent, [User], AuthCode, "
            cmdstr = cmdstr + "authtransid,authmessage,charged,cc_last_four,cc_expiration,"
            cmdstr = cmdstr + "billing_fname,billing_lname,"
            cmdstr = cmdstr + "billing_address,billing_city,billing_state,"
            cmdstr = cmdstr + "billing_zip,billing_phone,billing_email,auth_resp, "
            cmdstr = cmdstr + "month_plan_id, cash_plan_id)"
            cmdstr = cmdstr + "values(" + varOrderId.ToString() + ", @CellNum, 1,"
            cmdstr = cmdstr + "getdate(),"
            cmdstr = cmdstr + varMonthlyAmnt.ToString() + ", " + varCashAmnt.ToString() + ", " + varIntlAmnt.ToString() + ", " + varItemAmnt.ToString() + ",  0, "
            cmdstr = cmdstr + "@Agent, '" & varUser.ToString() & "',"
            cmdstr = cmdstr + " '', '', '', 0, "
            cmdstr = cmdstr + "'" + cclastfour + "','" + varCCExpiration + "',"
            'cmdstr = cmdstr + "'" + payprof.billTo.firstName + "','" + payprof.billTo.lastName + "','"
            'cmdstr = cmdstr + "'" + payprof.billTo.address + "','" + payprof.billTo.city + "','" + payprof.billTo.state + "','"
            'cmdstr = cmdstr + "'" + payprof.billTo.zip + ",'" 
            cmdstr = cmdstr + "'','','','','','',"
            cmdstr = cmdstr + "'','', "  'phone/email
            cmdstr = cmdstr + " '', @MonthPlanID, @CashPlanID ); "
            cmdstr = cmdstr + "SELECT SCOPE_IDENTITY() AS trans_id; "

            Dim cmd0 As New SqlCommand(cmdstr, con)

            cmd0.Parameters.Add("@Agent", SqlDbType.VarChar).Value = varAgent

            cmd0.Parameters.Add("@MonthPlanID", SqlDbType.Int).Value = varMonthlyPlanId
            cmd0.Parameters.Add("@CashPlanID", SqlDbType.Int).Value = varCashPlanId
            cmd0.Parameters.Add("@CellNum", SqlDbType.VarChar).Value = varCellNum

            Dim insertTransReader = cmd0.ExecuteReader()

            If insertTransReader.HasRows Then
                insertTransReader.Read()
                varTransactionId = insertTransReader.Item("trans_id")
            End If

            ' Close the reader
            insertTransReader.Close()

        End If  'Vartotal  >  0

        Try
            cmdstr = "select * from orders o join customers c on o.customer_id = c.customer_id where order_id = " + varOrderId.ToString()

            Dim cmd As New SqlCommand(cmdstr, con)

            rdr = cmd.ExecuteReader()
            If Not rdr.Read() Or Not rdr.HasRows() Then
                rdr.Close()
                SetAuthErrorMessage(varAuthMsg = "Invalid Order", varTransactionId)
                Exit Sub
            End If

        Catch ex As Exception
            If Not rdr.IsClosed() Then
                rdr.Close()
            End If
            SetAuthErrorMessage(varAuthMsg = ex.Message, varTransactionId)
            Exit Sub
        End Try

        Dim merchant = New svc.MerchantAuthenticationType()
        merchant.name = ConfigurationManager.ConnectionStrings("authnet.Merchant").ConnectionString
        merchant.transactionKey = ConfigurationManager.ConnectionStrings("authnet.Key").ConnectionString

        Dim custid As Long = GetIntValue(rdr, "customer_id")
        Dim custprofid As Long = GetIntValue(rdr, "auth_custprof_id")
        Dim payprofid As Long = GetIntValue(rdr, "auth_payprof_id")

        '7/3/12 cmb added these 10 variables
        Dim custProfDescription As String = GetStrValue(rdr, "lname")
        Dim custProfEmail As String = GetStrValue(rdr, "billing_email")
        Dim custProfMerchCustId As String = GetStrValue(rdr, "customer_id")
        Dim paymProfFirstName As String = GetStrValue(rdr, "billing_fname")
        Dim paymProfLastName As String = GetStrValue(rdr, "billing_lname")
        Dim paymProfAddress As String = GetStrValue(rdr, "billing_address")
        Dim paymProfCity As String = GetStrValue(rdr, "billing_city")
        Dim paymProfState As String = GetStrValue(rdr, "billing_state")
        Dim paymProfZip As String = GetStrValue(rdr, "billing_zip")
        Dim paymProfPhoneNumber As String = GetStrValue(rdr, "billing_phone")

        rdr.Close()

        Dim custprof = New svc.CustomerProfileType()
        Dim custprofresp = New svc.CreateCustomerProfileResponseType()
        Dim payprof As New svc.CustomerPaymentProfileType()

        Dim isNewCust As Boolean = False 'cmb 7/5/12

        If custprofid = 0 Then

            isNewCust = True

            custprof.description = custProfDescription
            custprof.email = custProfEmail
            custprof.merchantCustomerId = custProfMerchCustId
            custprofresp = svc1.CreateCustomerProfile(merchant, custprof, svc.ValidationModeEnum.none)

            If Not custprofresp.messages(0).code = "I00001" Then
                If custprofresp.messages(0).code = "E00039" Then
                    varAuthMsg = "Duplicate Customer"
                Else
                    varAuthMsg = custprofresp.messages(0).text
                End If


                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #1")
                Exit Sub
            End If

            custprofid = custprofresp.customerProfileId

            'Update Db: custprof_id


            Dim cmd1 As New SqlCommand("update customers set auth_custprof_id = " + custprofid.ToString() + " where customer_id = " + custid.ToString(), con)
            Try
                cmd1.ExecuteNonQuery()
            Catch e As Exception
                SetAuthErrorMessage(varAuthMsg = "error - Could Not save custprof", varTransactionId)
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #1 update custprof")
                Exit Sub
            End Try

        End If 'custprofid = 0


        If payprofid = 0 Then

            isNewCust = True '????????????????

            payprof.billTo = New svc.CustomerAddressType
            payprof.billTo.firstName = paymProfFirstName
            payprof.billTo.lastName = paymProfLastName
            payprof.billTo.address = paymProfAddress
            payprof.billTo.city = paymProfCity
            payprof.billTo.state = paymProfState
            payprof.billTo.zip = paymProfZip
            payprof.billTo.phoneNumber = paymProfPhoneNumber

            payprof.payment = New svc.PaymentType
            payprof.payment.Item = New svc.CreditCardType


            payprof.payment.Item.cardNumber = varCCNumber
            payprof.payment.Item.expirationDate = varCCExpiration
            payprof.payment.Item.cardCode = varCCCode


            Dim payprofileresp = New svc.CreateCustomerPaymentProfileResponseType()

            payprofileresp = svc1.CreateCustomerPaymentProfile(merchant, custprofid, payprof, svc.ValidationModeEnum.none)
            payprofid = payprofileresp.customerPaymentProfileId

            If Not payprofileresp.resultCode = "0" Then
                SetAuthErrorMessage(varAuthMsg = payprofileresp.messages(0).text, varTransactionId)
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #2")
                Exit Sub
            End If


            'Update Db: payprof_id
            Dim cmd1 As New SqlCommand("update customers set auth_payprof_id = " + payprofid.ToString() + " where customer_id = " + custid.ToString(), con)
            Try
                cmd1.ExecuteNonQuery()

            Catch e As Exception
                SetAuthErrorMessage(varAuthMsg = "error - Could Not save payprof", varTransactionId)
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #2 update payprof")
                Exit Sub
            End Try

        End If 'payprofid = 0

        ' When no cash total - then no transaction 
        If (varTotal > 0) Then
            ' Set profile ids for transaction
            Dim setProfileIds As String = "UPDATE [authtrans] SET [auth_custprof_id] = @CusProfId, "
            setProfileIds &= "[auth_payprof_id] = @PayProfId WHERE [transId] = " & varTransactionId
            Dim profileIdsCmd = New SqlCommand(setProfileIds, con)

            profileIdsCmd.Parameters.Add("@CusProfId", SqlDbType.Int).Value = custprofid
            profileIdsCmd.Parameters.Add("@PayProfId", SqlDbType.Int).Value = payprofid

            profileIdsCmd.ExecuteNonQuery()
        End If  ' varTotal > 0



        '7/2/12 cmb - update the Auth.net profile only if a customer's data changed and a valid CC number was input. (need whole CC num to update profile)
        'If isNewCust = False And varBillingInfoChanged = "changed" And varCCNumber.IndexOf("*") = -1 Then
        If isNewCust = False And varCCNumber.IndexOf("*") = -1 Then ' And varCCNumber.Length > 10

            'update customer profile
            Dim updProfile = New svc.CustomerProfileExType
            updProfile.customerProfileId = custprofid
            updProfile.merchantCustomerId = custProfMerchCustId
            updProfile.description = custProfDescription
            updProfile.email = custProfEmail

            Dim updCustprofresp = New svc.UpdateCustomerProfileResponseType()
            updCustprofresp = svc1.UpdateCustomerProfile(merchant, updProfile)

            If (Not updCustprofresp.messages(0).code = "I00001") Then
                SetAuthErrorMessage(varAuthMsg = updCustprofresp.messages(0).text, varTransactionId)
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #3")
                Exit Sub
            End If

            'update payment profile
            Dim updPayProf As New svc.CustomerPaymentProfileExType()
            updPayProf.billTo = New svc.CustomerAddressType
            updPayProf.billTo.firstName = paymProfFirstName
            updPayProf.billTo.lastName = paymProfLastName
            updPayProf.billTo.address = paymProfAddress
            updPayProf.billTo.city = paymProfCity
            updPayProf.billTo.state = paymProfState
            updPayProf.billTo.zip = paymProfZip
            updPayProf.billTo.phoneNumber = paymProfPhoneNumber

            updPayProf.payment = New svc.PaymentType
            updPayProf.payment.Item = New svc.CreditCardType

            'If varCCNumber.IndexOf("*") = -1 Then
            updPayProf.payment.Item.cardNumber = varCCNumber
            'End If

            updPayProf.payment.Item.expirationDate = varCCExpiration
            updPayProf.payment.Item.cardCode = varCCCode

            updPayProf.customerPaymentProfileId = payprofid

            Dim updPayProfileResp = New svc.UpdateCustomerPaymentProfileResponseType()
            updPayProfileResp = svc1.UpdateCustomerPaymentProfile(merchant, custprofid, updPayProf, svc.ValidationModeEnum.none)

            If Not updPayProfileResp.messages(0).code = "I00001" Then
                varAuthMsg = updPayProfileResp.messages(0).text
                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #4")
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
            authpay.Item.customerProfileId = custprofid
            authpay.Item.customerPaymentProfileId = payprofid

            Dim authresp = New svc.CreateCustomerProfileTransactionResponseType
            authresp = svc1.CreateCustomerProfileTransaction(merchant, authpay, "") ', "x_duplicate_window=1" -- parameter how many minutes to lock account between transactions. default=2

            If Not authresp.messages(0).code = "I00001" Then
                varAuthMsg = authresp.messages(0).text
                SetAuthErrorMessage(varAuthMsg, varTransactionId)
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #5")
                Exit Sub
            End If


            '  Dim settings(1) As svc.SettingType

            ' settings(0) = New svc.SettingType()
            ' settings(0).settingName = "hostedprofilereturnurl"
            ' settings(0).settingValue = "http://localhost/authrun/credit.aspx"
            ' settings(1) = New svc.SettingType()
            ' settings(1).settingName = "hostedprofilepagebordervisible"
            ' settings(1).settingValue = "true"


            'Dim resp = svc1.GetHostedProfilePage(merchant, custprofid.customerProfileId, settings)
            'strToken = resp.token
            'token.value = strToken


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

                'cmdstr = "insert authtrans"
                'cmdstr = cmdstr + "(orderid,trans_type,paydate,monthly_amt,cash_amt,"
                'cmdstr = cmdstr + "intl_amt, item_amt, total, agent, [User], AuthCode, "
                'cmdstr = cmdstr + "authtransid,authmessage,charged,cc_last_four,cc_expiration,"
                'cmdstr = cmdstr + "billing_fname,billing_lname,"
                'cmdstr = cmdstr + "billing_address,billing_city,billing_state,"
                'cmdstr = cmdstr + "billing_zip,billing_phone,billing_email,auth_resp)"
                'cmdstr = cmdstr + "values(" + ordernum.ToString() + ", 1,"
                'cmdstr = cmdstr + "getdate(),"
                'cmdstr = cmdstr + monthly_amt.ToString() + ", " + cash_amt.ToString() + ", " + intl_amt.ToString() + ", " + item_amt.ToString() + "," + total.ToString() + ","
                'cmdstr = cmdstr + "@Agent, '" & Membership.GetUser.UserName & "',"
                'cmdstr = cmdstr + "'" + resparr(4).ToString() + "','" + resparr(6).ToString() + "','" + resparr(3).ToString() + "'," + charged + ","
                'cmdstr = cmdstr + "'" + cclastfour + "','" + ccexp + "',"
                ''cmdstr = cmdstr + "'" + payprof.billTo.firstName + "','" + payprof.billTo.lastName + "','"
                ''cmdstr = cmdstr + "'" + payprof.billTo.address + "','" + payprof.billTo.city + "','" + payprof.billTo.state + "','"
                ''cmdstr = cmdstr + "'" + payprof.billTo.zip + ",'" 
                'cmdstr = cmdstr + "'','','','','','',"
                'cmdstr = cmdstr + "'','',"  'phone/email
                'cmdstr = cmdstr + "'" + authresp.directResponse + " ')"

                Dim cmd3 As New SqlCommand(successSql.ToString(), con)

                cmd3.Parameters.Add("@Charged", SqlDbType.Bit).Value = varCharged
                cmd3.Parameters.Add("@Total", SqlDbType.Money).Value = varTotal
                cmd3.Parameters.Add("@TransID", SqlDbType.Int).Value = varTransactionId

                cmd3.Parameters.Add("@AuthCode", SqlDbType.VarChar).Value = resparr(4).ToString()
                cmd3.Parameters.Add("@AuthTransId", SqlDbType.VarChar).Value = resparr(6).ToString()
                cmd3.Parameters.Add("@AuthMsg", SqlDbType.VarChar).Value = resparr(3).ToString()

                cmd3.Parameters.Add("@AuthResponse", SqlDbType.VarChar).Value = authresp.directResponse

                cmd3.ExecuteNonQuery()

                'Try
                '    cmd3.ExecuteNonQuery()
                'Catch e As Exception
                '    AuthMsg = "error - Could Not save authtrans"
                '    Return False
                'End Try

            Else
                SetAuthErrorMessage(varAuthMsg = authresp.messages(0).text, varTransactionId)
                rdr.Close()
                insertErrorDetail(custid, varCCNumber, varCCExpiration, varCCCode, varTotal, varAuthMsg, "loc #6")
            End If
        else
            varAuthMsg = ""
        End If   'varTotal > 0
        con.Close()

    End Sub

    Private Sub insertErrorDetail(ByVal clientid As Integer, ByVal ccnum As String, ByVal ccexp As String, ByVal ccCode As String, _
                                  ByVal amount As String, ByVal err As String, ByVal errorLocation As String)

        Dim strSql As String = "INSERT ErrorLog (clientId, CCnum, CCexp, CCcode, amount, error, errorLocation) "
        strSql &= "VALUES(" & clientid & ", "
        strSql &= "'" & ccnum & "', "
        strSql &= "'" & ccexp & "', "
        strSql &= "'" & ccCode & "', "
        strSql &= "'" & amount & "', "
        strSql &= "'" & err.Replace("'", "''") & "', "
        strSql &= "'" & errorLocation & "')"

        Dim con As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim comm As SqlCommand = New SqlCommand(strSql, con)

        con.Open()
        comm.ExecuteNonQuery()
        con.Close()

    End Sub

End Class