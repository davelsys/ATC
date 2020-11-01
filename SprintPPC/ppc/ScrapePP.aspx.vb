Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Net
Imports System.Web.HttpCookieCollection
Imports System.Web.HttpCookie





Partial Class ScrapePP
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        getCDR()

    End Sub

    Private Sub getCDR()

        Dim con As SqlConnection
        Dim cmd As SqlCommand
        Dim dtAccounts As New DataTable
        Dim sa As SqlDataAdapter

        con = New SqlConnection(ConfigurationManager.ConnectionStrings("PPC").ConnectionString)
        con.Open()

        sa = New SqlDataAdapter("select AccountName,password from accounts where isnull(active,0) = 1  ", con)
        sa.Fill(dtAccounts)


        Dim myCookieContainer4 As CookieContainer = New CookieContainer


        Dim dtblScrape As System.Data.DataTable

        Dim objRequest As HttpWebRequest
        Dim strRequest As String
        Dim arrRequest As Byte()
        Dim objUTF8Encoding As UTF8Encoding
        Dim strmRequest As Stream
        Dim objResponse As HttpWebResponse
        Dim srResponse As StreamReader
        Dim strTemp As String
        Dim SearchBy As String
        Dim strString, strFiltered As String

        Dim i, x, y As Integer
        Dim arrstrPhoneAccounts() As String

        Dim flag As Boolean = True
        Dim dictPhoneAccounts As New Collections.Specialized.ListDictionary

        Dim strKey As String = ""
        Dim strValue As String = ""


        Dim strB As New StringBuilder

        Dim blnNext As Boolean = False
        Dim nPage As Int32 = 0

        Dim viewState As String
        Dim eventValidation As String

        Dim strFilteredInner As String = ""
        Dim strFilteredInner2 As String = ""

        Dim arrFields() As String
        Dim strCallDate As String = ""

        Dim blnFirst As Boolean = True

        Dim strloc As String = "https://www.pagepluscellular.com/login.aspx"

        strB.Append("delete cdrTemp ")
        cmd = New SqlCommand(strB.ToString, con)
        cmd.ExecuteNonQuery()

        For Each row As DataRow In dtAccounts.Rows
            strloc = "https://www.pagepluscellular.com/login.aspx"

            myCookieContainer4 = New CookieContainer

            Response.Write("<br><br>*********** Account:" & row.Item("Accountname") & " ********************")

            objRequest = Nothing
            objResponse = Nothing

            objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
            objRequest.Method = "POST"
            objRequest.ContentType = "application/x-www-form-urlencoded"
            'objRequest.SendChunked = False
            'objRequest.CookieContainer = myCookieContainer
            objRequest.AllowAutoRedirect = False
            objRequest.CookieContainer = myCookieContainer4

            objRequest.KeepAlive = True

            'objRequest.UnsafeAuthenticatedConnectionSharing = True


            'CustomerID=42892&SearchBy=0&fKeyword=padlock&image1.x=24&image1.y=2


            'regEx = New Regex(" ")
            'strTemp = regEx.Replace(strTemp, "+")

            'strTemp = Server.UrlEncode(strTemp)
            ' strRequest = "username=9174164730&password=mendy1" '& strTemp  '& vbCrLf

            strRequest = "username=" & row.Item("AccountName") & "&password=" & row.Item("password") '& strTemp  '& vbCrLf


            'strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



            objUTF8Encoding = New UTF8Encoding
            arrRequest = objUTF8Encoding.GetBytes(strRequest)

            objRequest.ContentLength = arrRequest.Length
            strmRequest = objRequest.GetRequestStream()
            strmRequest.Write(arrRequest, 0, arrRequest.Length)
            strmRequest.Close()

            objResponse = objRequest.GetResponse()

            srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)


            'strString = srResponse.ReadToEnd()
            objResponse.Close()


            objRequest = CType(WebRequest.Create("https://www.pagepluscellular.com/My%20Account/My%20Account%20Summary.aspx"), HttpWebRequest)
            objRequest.Method = "GET"
            objRequest.CookieContainer = myCookieContainer4

            objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
            srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
            strString = srResponse.ReadToEnd()
            Trace.Warn("strString " & strString)
            srResponse.Close()

            objResponse.Close()


            'Response.Write(strString)



            'ctl01$GlobalMenu1$hdnIsUserLogged
            'ctl07$(ddlPhoneAccounts)
            'ctl07$(ddlSelectDateRange)
            'ctl07$(ddlPageSize



            strloc = "https://www.pagepluscellular.com/My%20Account/My%20Phone/Call%20Records.aspx"
            objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
            objRequest.Method = "POST"
            objRequest.ContentType = "application/x-www-form-urlencoded"
            'objRequest.SendChunked = False
            'objRequest.CookieContainer = myCookieContainer
            objRequest.AllowAutoRedirect = False
            objRequest.CookieContainer = myCookieContainer4

            objRequest.KeepAlive = True


            'strRequest = "ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts=504384&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40"
            strRequest = "ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40"

            'strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



            objUTF8Encoding = New UTF8Encoding
            arrRequest = objUTF8Encoding.GetBytes(strRequest)

            objRequest.ContentLength = arrRequest.Length
            strmRequest = objRequest.GetRequestStream()
            strmRequest.Write(arrRequest, 0, arrRequest.Length)
            strmRequest.Close()

            objResponse = objRequest.GetResponse()

            srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)


            objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
            srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
            strString = srResponse.ReadToEnd()
            Trace.Warn("strString " & strString)
            srResponse.Close()

            objResponse.Close()

            '    Response.Write(strString)




            i = strString.IndexOf("<select ")
            Try
                strFiltered = strString.Substring(i)
                i = strFiltered.IndexOf("ddlPhoneAccounts""")
                strFiltered = strFiltered.Substring(i)

                i = strFiltered.IndexOf("</select>")
                strFiltered = strFiltered.Substring(0, i)

                i = strFiltered.IndexOf("<option ")
                strFiltered = strFiltered.Substring(i)


                arrstrPhoneAccounts = strFiltered.Split(New String() {"</option>"}, StringSplitOptions.None)
            Catch
            End Try


            dictPhoneAccounts = New Collections.Specialized.ListDictionary

            If arrstrPhoneAccounts.Length > 0 Then
                For Each Str As String In arrstrPhoneAccounts
                    If Str.Trim.Length > 0 Then
                        i = Str.IndexOf("value=")
                        strFiltered = Str.Substring(i)
                        i = strFiltered.IndexOf(">")
                        strKey = strFiltered.Substring(0, i)
                        strKey = strKey.Replace("value=", "").Replace("""", "").Trim

                        strFiltered = strFiltered.Substring(i + 1)
                        strValue = strFiltered.Trim

                        dictPhoneAccounts.Add(strKey, strValue)
                    End If

                Next
            End If


            strB = New StringBuilder
            blnNext = False
            nPage = 0
            strFilteredInner = ""
            strFilteredInner2 = ""

            strCallDate = ""

            For Each strPhoneAccount As String In dictPhoneAccounts.Keys
                Response.Write("<br><br>*********** Account:" & row.Item("Accountname") & "  " & strPhoneAccount & " " & dictPhoneAccounts(strPhoneAccount).ToString & " *******************" & "<br>")

                blnNext = False
                nPage = 0

                Do
                    strB = New StringBuilder

                    nPage += 1

                    objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
                    objRequest.Method = "POST"
                    objRequest.ContentType = "application/x-www-form-urlencoded"
                    'objRequest.SendChunked = False
                    'objRequest.CookieContainer = myCookieContainer
                    objRequest.AllowAutoRedirect = False
                    objRequest.CookieContainer = myCookieContainer4

                    objRequest.KeepAlive = True


                    If nPage = 1 Then
                        viewState = ExtractViewState(strString)
                        eventValidation = ExtractEventValidation(strString)
                        '  Dim strPhoneAccount As String = "504384"
                        strRequest =
                              String.Format(
                                 "__EVENTTARGET=ctl07$ddlPhoneAccounts&__EVENTARGUMENT=&__LASTFOCUS=&__VIEWSTATE={0}&__EVENTVALIDATION={1}&ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts={2}&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40",
                                 viewState, eventValidation, strPhoneAccount
                              )
                    Else
                        viewState = ExtractViewState(strString)
                        eventValidation = ExtractEventValidation(strString)

                        strRequest =
                    String.Format(
                       "__EVENTTARGET=ctl07$DataListCallRecords$ctl00$lbtnNext&__EVENTARGUMENT=&__LASTFOCUS=&__VIEWSTATE={0}&__EVENTVALIDATION={1}&ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts={2}&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40",
                       viewState, eventValidation, strPhoneAccount
                    )
                    End If


                    ' strRequest = "ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts=504384&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40&__EVENTTARGET=ctl07$DataListCallRecords$ctl11$lbtNext"

                    'strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



                    '// write the form values into the request message
                    'StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream());
                    'requestWriter.Write(postData);
                    'requestWriter.Close();


                    objUTF8Encoding = New UTF8Encoding
                    arrRequest = objUTF8Encoding.GetBytes(strRequest)

                    objRequest.ContentLength = arrRequest.Length
                    strmRequest = objRequest.GetRequestStream()
                    strmRequest.Write(arrRequest, 0, arrRequest.Length)
                    strmRequest.Close()

                    objResponse = objRequest.GetResponse()

                    srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)


                    objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
                    srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
                    strString = srResponse.ReadToEnd()
                    Trace.Warn("strString " & strString)
                    srResponse.Close()

                    objResponse.Close()


                    '  ctl07$DataListCallRecords$ctl11$lbtNext

                    'Response.Write(strString)

                    If strString.IndexOf("lbtnNext") > 0 Then
                        blnNext = True
                    Else
                        blnNext = False
                    End If

                    i = strString.IndexOf("<table")
                    '  Try
                    strFiltered = strString.Substring(i)
                    i = strFiltered.IndexOf("DataListCallRecords""")
                    strFiltered = strFiltered.Substring(i)

                    i = strFiltered.IndexOf("</div>")
                    strFiltered = strFiltered.Substring(0, i)

                    i = strFiltered.IndexOf("<tr")
                    strFiltered = strFiltered.Substring(i)

                    blnFirst = True

                    strB = New StringBuilder

                    strB.Append("insert into cdrTemp(AccountName,PhoneAccountId,PhoneAccount ,PhoneNumber,Duration,CallDate,PreBalance,PostBalance,Description,DateRetrieved ) ")

                    While strFiltered.IndexOf("class=""maintext""") > 0
                        i = strFiltered.IndexOf("class=""maintext""")
                        strFiltered = strFiltered.Substring(i)

                        i = strFiltered.IndexOf("<table")
                        strFiltered = strFiltered.Substring(i)


                        i = strFiltered.IndexOf("</table>")
                        strFilteredInner = strFiltered.Substring(0, i)

                        strFilteredInner2 = ClearHTMLTags(strFilteredInner, 0)

                        arrFields = strFilteredInner2.Trim.Replace("        ", "~").Replace("~~~", "").Split("~")


                        strCallDate = Date.Parse(arrFields(3) & " " & arrFields(2).Substring(0, arrFields(2).IndexOf("M ") + 1))


                        If Not blnFirst Then
                            strB.Append("Union All ")
                        End If
                        strB.Append("select '" & row.Item("AccountName") & "', '" & strPhoneAccount & "','" & dictPhoneAccounts(strPhoneAccount).ToString & "' ")
                        strB.Append("," & arrFields(0) & " ")
                        strB.Append("," & arrFields(1) & " ")
                        strB.Append(",'" & strCallDate.ToString & "' ")
                        strB.Append("," & arrFields(4) & " ")
                        strB.Append("," & arrFields(5) & " ")
                        strB.Append(",'" & arrFields(6) & "' ")
                        strB.Append(",getdate() ")

                        'strB = New StringBuilder
                        'strB.Append("begin try ")
                        'strB.Append("begin ")
                        'strB.Append("insert into cdr(PhoneAccountId,PhoneAccount ,PhoneNumber,Duration,CallDate,PreBalance,PostBalance,Description ) ")
                        'strB.Append("select '" & strPhoneAccount & "','" & dictPhoneAccounts(strPhoneAccount).ToString & "' ")
                        'strB.Append("," & arrFields(0) & " ")
                        'strB.Append("," & arrFields(1) & " ")
                        'strB.Append(",'" & strCallDate.ToString & "' ")
                        'strB.Append("," & arrFields(4) & " ")
                        'strB.Append("," & arrFields(5) & " ")
                        'strB.Append(",'" & arrFields(6) & "' ")

                        'strB.Append("End ")
                        'strB.Append("end try ")
                        'strB.Append("begin catch ")
                        'strB.Append("DECLARE @ErrMsg nvarchar(4000), @ErrSeverity int ")
                        'strB.Append("SELECT @ErrMsg = ERROR_MESSAGE(),@ErrSeverity = ERROR_SEVERITY() ")
                        'strB.Append("if  ERROR_NUMBER() <> 2627 ")
                        'strB.Append("RAISERROR(@ErrMsg, @ErrSeverity, 1) ")
                        'strB.Append("end catch ")
                        'strB.Append(" ")


                        'cmd = New SqlCommand(strB.ToString, con)
                        'cmd.ExecuteNonQuery()

                        ' Response.Write(strFilteredInner2)

                        strFiltered = strFiltered.Substring(20)
                        blnFirst = False
                    End While



                    cmd = New SqlCommand(strB.ToString, con)
                    cmd.ExecuteNonQuery()

                    'Response.Write(strFiltered)
                    '    Catch
                    '  End Try
                    System.Threading.Thread.Sleep(200)

                Loop While blnNext


                ' '''' if lbtnNext

                ' ''''''''''''''''''''''''''''''''''''''''''''''''''''''next page'''''''''''''''''

                'objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
                'objRequest.Method = "POST"
                'objRequest.ContentType = "application/x-www-form-urlencoded"
                ''objRequest.SendChunked = False
                ''objRequest.CookieContainer = myCookieContainer
                'objRequest.AllowAutoRedirect = False
                'objRequest.CookieContainer = myCookieContainer4

                'objRequest.KeepAlive = True


                '' viewState = ExtractViewState(strString)
                '' eventValidation = ExtractEventValidation(strString)
                ''strPhoneAccount = "504384"
                'strRequest =
                '      String.Format(
                '         "__EVENTTARGET=ctl07$DataListCallRecords$ctl00$lbtnNext&__EVENTARGUMENT=&__LASTFOCUS=&__VIEWSTATE={0}&__EVENTVALIDATION={1}&ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts={2}&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40",
                '         viewState, eventValidation, strPhoneAccount
                '      )

                '' strRequest = "ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts=504384&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40&__EVENTTARGET=ctl07$DataListCallRecords$ctl11$lbtNext"

                ''strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



                ''// write the form values into the request message
                ''StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream());
                ''requestWriter.Write(postData);
                ''requestWriter.Close();


                'objUTF8Encoding = New UTF8Encoding
                'arrRequest = objUTF8Encoding.GetBytes(strRequest)

                'objRequest.ContentLength = arrRequest.Length
                'strmRequest = objRequest.GetRequestStream()
                'strmRequest.Write(arrRequest, 0, arrRequest.Length)
                'strmRequest.Close()

                'objResponse = objRequest.GetResponse()

                'srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)


                'objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
                'srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
                'strString = srResponse.ReadToEnd()
                'Trace.Warn("strString " & strString)
                'srResponse.Close()

                'objResponse.Close()


                ''  ctl07$DataListCallRecords$ctl11$lbtNext

                'Response.Write(strString)

                System.Threading.Thread.Sleep(1500)
            Next
        Next

        strB = New StringBuilder
        strB.Append("insert into cdr(AccountName,PhoneAccountId,PhoneAccount ,PhoneNumber,Duration,CallDate,PreBalance,PostBalance,Description,DateRetrieved ) ")
        strB.Append("select ct.AccountName,ct.PhoneAccountId,ct.PhoneAccount ,ct.PhoneNumber,ct.Duration,ct.CallDate,ct.PreBalance,ct.PostBalance,ct.Description,getdate()  ")
        strB.Append("from cdrTemp ct ")
        strB.Append("where ct.calldate > isnull((select max(c.calldate) from cdr c where  c.accountname=ct.accountname  and c.phoneAccountId = ct.phoneAccountid ),'1-1-2000') ")

        cmd = New SqlCommand(strB.ToString, con)
        cmd.ExecuteNonQuery()

        con.Close()

        Response.Write("<br><br>*******Successfully retrieved data*********")

    End Sub

    Private Sub getCDR_TEST()
        Dim myCookieContainer4 As CookieContainer = New CookieContainer


        Dim dtblScrape As System.Data.DataTable

        Dim objRequest As HttpWebRequest
        Dim strRequest As String
        Dim arrRequest As Byte()
        Dim objUTF8Encoding As UTF8Encoding
        Dim strmRequest As Stream
        Dim objResponse As HttpWebResponse
        Dim srResponse As StreamReader

        Dim strloc As String = "https://www.pagepluscellular.com/login.aspx"

        objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
        objRequest.Method = "POST"
        objRequest.ContentType = "application/x-www-form-urlencoded"
        'objRequest.SendChunked = False
        'objRequest.CookieContainer = myCookieContainer
        objRequest.AllowAutoRedirect = False
        objRequest.CookieContainer = myCookieContainer4

        objRequest.KeepAlive = True

        'objRequest.UnsafeAuthenticatedConnectionSharing = True


        'CustomerID=42892&SearchBy=0&fKeyword=padlock&image1.x=24&image1.y=2
        Dim strTemp As String
        Dim SearchBy As String

        'regEx = New Regex(" ")
        'strTemp = regEx.Replace(strTemp, "+")

        strTemp = Server.UrlEncode(strTemp)
        strRequest = "username=9174164730&password=mendy1" & strTemp  '& vbCrLf

        'strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



        objUTF8Encoding = New UTF8Encoding
        arrRequest = objUTF8Encoding.GetBytes(strRequest)

        objRequest.ContentLength = arrRequest.Length
        strmRequest = objRequest.GetRequestStream()
        strmRequest.Write(arrRequest, 0, arrRequest.Length)
        strmRequest.Close()

        objResponse = objRequest.GetResponse()

        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)

        Dim strString, strFiltered As String
        'strString = srResponse.ReadToEnd()
        objResponse.Close()


        objRequest = CType(WebRequest.Create("https://www.pagepluscellular.com/My%20Account/My%20Account%20Summary.aspx"), HttpWebRequest)
        objRequest.Method = "GET"
        objRequest.CookieContainer = myCookieContainer4

        objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
        strString = srResponse.ReadToEnd()
        Trace.Warn("strString " & strString)
        srResponse.Close()

        objResponse.Close()


        'Response.Write(strString)



        'ctl01$GlobalMenu1$hdnIsUserLogged
        'ctl07$(ddlPhoneAccounts)
        'ctl07$(ddlSelectDateRange)
        'ctl07$(ddlPageSize



        strloc = "https://www.pagepluscellular.com/My%20Account/My%20Phone/Call%20Records.aspx"
        objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
        objRequest.Method = "POST"
        objRequest.ContentType = "application/x-www-form-urlencoded"
        'objRequest.SendChunked = False
        'objRequest.CookieContainer = myCookieContainer
        objRequest.AllowAutoRedirect = False
        objRequest.CookieContainer = myCookieContainer4

        objRequest.KeepAlive = True


        strRequest = "ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts=504384&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40"

        'strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



        objUTF8Encoding = New UTF8Encoding
        arrRequest = objUTF8Encoding.GetBytes(strRequest)

        objRequest.ContentLength = arrRequest.Length
        strmRequest = objRequest.GetRequestStream()
        strmRequest.Write(arrRequest, 0, arrRequest.Length)
        strmRequest.Close()

        objResponse = objRequest.GetResponse()

        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)


        objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
        strString = srResponse.ReadToEnd()
        Trace.Warn("strString " & strString)
        srResponse.Close()

        objResponse.Close()

        '    Response.Write(strString)


        objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
        objRequest.Method = "POST"
        objRequest.ContentType = "application/x-www-form-urlencoded"
        'objRequest.SendChunked = False
        'objRequest.CookieContainer = myCookieContainer
        objRequest.AllowAutoRedirect = False
        objRequest.CookieContainer = myCookieContainer4

        objRequest.KeepAlive = True


        Dim viewState As String = ExtractViewState(strString)
        Dim eventValidation As String = ExtractEventValidation(strString)
        Dim strPhoneAccount As String = "504384"
        strRequest =
              String.Format(
                 "__EVENTTARGET=ctl07$ddlPhoneAccounts&__EVENTARGUMENT=&__LASTFOCUS=&__VIEWSTATE={0}&__EVENTVALIDATION={1}&ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts={2}&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40",
                 viewState, eventValidation, strPhoneAccount
              )

        ' strRequest = "ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts=504384&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40&__EVENTTARGET=ctl07$DataListCallRecords$ctl11$lbtNext"

        'strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



        '// write the form values into the request message
        'StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream());
        'requestWriter.Write(postData);
        'requestWriter.Close();


        objUTF8Encoding = New UTF8Encoding
        arrRequest = objUTF8Encoding.GetBytes(strRequest)

        objRequest.ContentLength = arrRequest.Length
        strmRequest = objRequest.GetRequestStream()
        strmRequest.Write(arrRequest, 0, arrRequest.Length)
        strmRequest.Close()

        objResponse = objRequest.GetResponse()

        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)


        objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
        strString = srResponse.ReadToEnd()
        Trace.Warn("strString " & strString)
        srResponse.Close()

        objResponse.Close()


        '  ctl07$DataListCallRecords$ctl11$lbtNext

        'Response.Write(strString)

        ''''''''''''''''''''''''''''''''''''''''''''''''''''''next page'''''''''''''''''
        objRequest = CType(WebRequest.Create(strloc), HttpWebRequest)
        objRequest.Method = "POST"
        objRequest.ContentType = "application/x-www-form-urlencoded"
        'objRequest.SendChunked = False
        'objRequest.CookieContainer = myCookieContainer
        objRequest.AllowAutoRedirect = False
        objRequest.CookieContainer = myCookieContainer4

        objRequest.KeepAlive = True


        ' viewState = ExtractViewState(strString)
        ' eventValidation = ExtractEventValidation(strString)
        'strPhoneAccount = "504384"
        strRequest =
              String.Format(
                 "__EVENTTARGET=ctl07$DataListCallRecords$ctl00$lbtnNext&__EVENTARGUMENT=&__LASTFOCUS=&__VIEWSTATE={0}&__EVENTVALIDATION={1}&ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts={2}&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40",
                 viewState, eventValidation, strPhoneAccount
              )

        ' strRequest = "ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts=504384&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40&__EVENTTARGET=ctl07$DataListCallRecords$ctl11$lbtNext"

        'strRequest = "CustomerID=42892&SearchBy=0&fKeyword=" & strTemp  '& vbCrLf



        '// write the form values into the request message
        'StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream());
        'requestWriter.Write(postData);
        'requestWriter.Close();


        objUTF8Encoding = New UTF8Encoding
        arrRequest = objUTF8Encoding.GetBytes(strRequest)

        objRequest.ContentLength = arrRequest.Length
        strmRequest = objRequest.GetRequestStream()
        strmRequest.Write(arrRequest, 0, arrRequest.Length)
        strmRequest.Close()

        objResponse = objRequest.GetResponse()

        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)


        objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
        strString = srResponse.ReadToEnd()
        Trace.Warn("strString " & strString)
        srResponse.Close()

        objResponse.Close()


        '  ctl07$DataListCallRecords$ctl11$lbtNext

        Response.Write(strString)

    End Sub


    Private Function ExtractViewState(ByVal s As String) As String
        Dim viewStateNameDelimiter As String = "__VIEWSTATE"
        Dim valueDelimiter As String = "value="""

        Dim viewStateNamePosition As Int32 = s.IndexOf(viewStateNameDelimiter)
        Dim viewStateValuePosition As Int32 = s.IndexOf(valueDelimiter, viewStateNamePosition)

        Dim viewStateStartPosition As Int32 = viewStateValuePosition + valueDelimiter.Length
        Dim viewStateEndPosition As Int32 = s.IndexOf("""", viewStateStartPosition)

        Return HttpUtility.UrlEncodeUnicode(
                 s.Substring(
                    viewStateStartPosition,
                    viewStateEndPosition - viewStateStartPosition
                 )
              )
    End Function

    Private Function ExtractEventValidation(ByVal s As String) As String
        Dim evenValidationNameDelimiter As String = "__EVENTVALIDATION"
        Dim valueDelimiter As String = "value="""

        Dim evenValidationNamePosition As Int32 = s.IndexOf(evenValidationNameDelimiter)
        Dim evenValidationValuePosition As Int32 = s.IndexOf(valueDelimiter, evenValidationNamePosition)

        Dim evenValidationStartPosition As Int32 = evenValidationValuePosition + valueDelimiter.Length
        Dim evenValidationEndPosition As Int32 = s.IndexOf("""", evenValidationStartPosition)

        Return HttpUtility.UrlEncodeUnicode(
                 s.Substring(
                    evenValidationStartPosition,
                    evenValidationEndPosition - evenValidationStartPosition
                 )
              )
    End Function

    Function ClearHTMLTags(ByVal strHTML, ByVal intWorkFlow)

        'Variables used in the function

        Dim regEx, regEx2, strTagLess

        '---------------------------------------
        strTagLess = strHTML
        'Move the string into a private variable
        'within the function
        '---------------------------------------

        'regEx initialization

        '---------------------------------------
        'Creates a regexp object		
        'regEx.IgnoreCase = True
        'Don't give frat about case sensitivity
        'regEx.Global = True
        'Global applicability
        '---------------------------------------


        'Phase I
        '	"bye bye html tags"


        If intWorkFlow <> 1 Then

            '---------------------------------------
            regEx = New Regex("<[^>]*>")
            'this pattern mathces any html tag
            strTagLess = regEx.Replace(strTagLess, "")
            ' regEx = New Regex("[|]{2,}")

            ' strTagLess = regEx.Replace(strTagLess, "")
            ' regEx = New Regex("[|][\s]*[|]")
            ' strTagLess = regEx.Replace(strTagLess, "|")
            'all html tags are stripped
            '---------------------------------------

        End If


        'Phase II
        '	"bye bye rouge leftovers"
        '	"or, I want to render the source"
        '	"as html."

        '---------------------------------------
        'We *might* still have rouge < and > 
        'let's be positive that those that remain
        'are changed into html characters
        '---------------------------------------	


        If intWorkFlow > 0 And intWorkFlow < 3 Then


            regEx = New Regex("[<]")
            'matches a single <
            strTagLess = regEx.Replace(strTagLess, "&lt;")

            regEx.Pattern = "[>]"
            'matches a single >
            strTagLess = regEx.Replace(strTagLess, "&gt;")
            '---------------------------------------

        End If


        'Clean up

        '---------------------------------------
        regEx = Nothing
        'Destroys the regExp object
        '---------------------------------------	

        '---------------------------------------
        ClearHTMLTags = strTagLess
        'The results are passed back
        '---------------------------------------

    End Function

End Class
