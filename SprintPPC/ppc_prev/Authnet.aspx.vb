Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Net
Imports System.Data.SqlClient

Public Class Authnet
    Inherits System.Web.UI.Page

    Public strToken As String = "not set"

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then

            Dim orderid As Long = 22
            Dim ccnum As String = "4222222222222"
            Dim expdate As String = "2014-01"
            Dim cccode As String = "1234"
            Dim cashcharge As Decimal = 20
            Dim total As Decimal = 50
            Dim Authcode As String = ""
            Dim AuthMsg As String = ""

            RunCharge(orderid, ccnum, expdate, cccode, cashcharge, total - cashcharge, total, Authcode, AuthMsg)

        End If

    End Sub

    Protected Function RunCharge(ByVal ordernum As Int16, ByVal ccnum As String, ByVal ccexp As String, ByVal code As String, ByVal subsamt As Decimal, ByVal cashamt As Decimal, ByVal total As Decimal,
                           ByRef AuthCode As String, ByRef AuthMsg As String) As Boolean

        Dim svc1 = New svc.Service()
        Dim rdr As SqlDataReader
        Dim conn As New SqlConnection
        Dim cmdstr As String

        Try


            Dim connstr As String = "Server=(local);Database=chaimppc;User ID=sa;Password=davel;Asynchronous Processing=true"
            conn.ConnectionString = connstr
            conn.Open()


            cmdstr = "select * from orders_test o join customers_test c on o.customer_id = c.customer_id where order_id = " + ordernum.ToString()

            Dim cmd As New SqlCommand(cmdstr, conn)


            rdr = cmd.ExecuteReader()
            If Not rdr.Read() Or Not rdr.HasRows() Then
                AuthMsg = "Invalid Order"
                Return False
            End If


        Catch ex As Exception
            AuthMsg = ex.Message
            Return False
        End Try

        Dim merchant = New svc.MerchantAuthenticationType()
        merchant.name = "9awM4hGv3J"
        merchant.transactionKey = "9HAa9K96hQ2T38fG"

        Dim custid As Long = GetIntValue(rdr, "customer_id")
        Dim custprofid As Long = GetIntValue(rdr, "auth_custprof_id")
        Dim payprofid As Long = GetIntValue(rdr, "auth_payprof_id")
        Dim custprof = New svc.CustomerProfileType()
        Dim custprofresp = New svc.CreateCustomerProfileResponseType()
        Dim payprof As New svc.CustomerPaymentProfileType()

        If custprofid = 0 Then

            custprof.description = GetStrValue(rdr, "lname")
            custprof.email = GetStrValue(rdr, "billing_email")
            custprof.merchantCustomerId = GetStrValue(rdr, "customer_id")
            custprofresp = svc1.CreateCustomerProfile(merchant, custprof, svc.ValidationModeEnum.none)

            If Not custprofresp.messages(0).code = "I00001" Then
                If custprofresp.messages(0).code = "E00039" Then
                    AuthMsg = "Duplicate Customer"
                Else
                    AuthMsg = custprofresp.messages(0).text
                End If
                Return False
            End If

            custprofid = custprofresp.customerProfileId

            payprof.billTo = New svc.CustomerAddressType
            payprof.billTo.firstName = GetStrValue(rdr, "billing_fname")
            payprof.billTo.firstName = GetStrValue(rdr, "billing_lname")
            payprof.billTo.address = GetStrValue(rdr, "billing_address")
            payprof.billTo.city = GetStrValue(rdr, "billing_city")
            payprof.billTo.state = GetStrValue(rdr, "billing_state")
            payprof.billTo.zip = GetStrValue(rdr, "billing_zip")
            payprof.billTo.phoneNumber = GetStrValue(rdr, "billing_phone")

            payprof.payment = New svc.PaymentType
            payprof.payment.Item = New svc.CreditCardType
            payprof.payment.Item.cardNumber = ccnum
            payprof.payment.Item.expirationDate = ccexp
            payprof.payment.Item.cardCode = code

            Dim payprofileresp = New svc.CreateCustomerPaymentProfileResponseType()
            payprofileresp = svc1.CreateCustomerPaymentProfile(merchant, custprofid, payprof, svc.ValidationModeEnum.none)

            If Not payprofileresp.resultCode = "0" Then
                Dim msg As New svc.MessagesTypeMessage
                msg = payprofileresp.messages(0)
                AuthMsg = msg.text
                Return False
            End If

            payprofid = payprofileresp.customerPaymentProfileId

            'Update Db
            rdr.Close()

            cmdstr = "update customers_test set auth_custprof_id = " + custprofid.ToString() + ", auth_payprof_id = " + payprofid.ToString() + " where customer_id = " + custid.ToString()
            Dim cmd1 As New SqlCommand(cmdstr, conn)
            Try
                cmd1.ExecuteNonQuery()
            Catch e As Exception
                AuthMsg = "error - Could Not save payprof"
                Return False
            End Try
        Else
            rdr.Close()
        End If

        Dim authpay = New svc.ProfileTransactionType
        authpay.Item = New svc.ProfileTransAuthCaptureType
        authpay.Item.amount = total
        authpay.Item.cardCode = code
        authpay.Item.customerProfileId = custprofid
        authpay.Item.customerPaymentProfileId = payprofid

        Dim authresp = New svc.CreateCustomerProfileTransactionResponseType
        authresp = svc1.CreateCustomerProfileTransaction(merchant, authpay, "")


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

        AuthCode = resparr(4) ' authresp.messages(0).text 'resparr(3)
        AuthMsg = resparr(3)  'authresp.directResponse    'resparr(3)

        Dim cclastfour As String = ccnum.Substring(ccnum.Length() - 4)
        Dim charged As String
        If authresp.resultCode = 0 Then
            charged = "1"
        Else
            charged = "0"
        End If


        cmdstr = "insert authtrans"
        cmdstr = cmdstr + "(orderid,paydate,subs_amt,cash_amt,total,authcode,"
        cmdstr = cmdstr + "authtransid,authmessage,charged,cc_last_four,cc_expiration,"
        cmdstr = cmdstr + "billing_fname,billing_lname,"
        cmdstr = cmdstr + "billing_address,billing_city,billing_state,"
        cmdstr = cmdstr + "billing_zip,billing_phone,billing_email,auth_resp)"
        cmdstr = cmdstr + "values(" + ordernum.ToString() + ","
        cmdstr = cmdstr + "getdate(),"
        cmdstr = cmdstr + subsamt.ToString() + "," + cashamt.ToString() + "," + total.ToString() + ","
        cmdstr = cmdstr + "'" + resparr(4).ToString() + "','" + resparr(6).ToString() + "','" + resparr(3).ToString() + "'," + charged + ","
        cmdstr = cmdstr + "'" + cclastfour + "','" + ccexp + "',"
        'cmdstr = cmdstr + "'" + payprof.billTo.firstName + "','" + payprof.billTo.lastName + "','"
        'cmdstr = cmdstr + "'" + payprof.billTo.address + "','" + payprof.billTo.city + "','" + payprof.billTo.state + "','"
        'cmdstr = cmdstr + "'" + payprof.billTo.zip + ",'" 
        cmdstr = cmdstr + "'','','','','','',"
        cmdstr = cmdstr + "'','',"  'phone/email
        cmdstr = cmdstr + "'" + authresp.directResponse + " ')"

        Dim cmd3 As New SqlCommand(cmdstr, conn)
        Try
            cmd3.ExecuteNonQuery()
        Catch e As Exception
            AuthMsg = "error - Could Not save authtrans"
            Return False
        End Try

        Return True

    End Function
    Function GetStrValue(ByRef rdr As SqlDataReader, ByRef Field As String) As String
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            Return ""
        Else
            Return rdr.GetValue(rdr.GetOrdinal(Field)).ToString()
        End If
    End Function
    Function GetIntValue(ByRef rdr As SqlDataReader, ByRef Field As String) As Int32
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            Return 0
        Else
            Return rdr.GetValue(rdr.GetOrdinal(Field))
        End If
    End Function
End Class