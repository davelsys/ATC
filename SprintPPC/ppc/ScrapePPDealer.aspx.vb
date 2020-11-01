Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Net
Imports System.Web.HttpCookieCollection
Imports System.Web.HttpCookie



Partial Class ScrapePPDealer
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        GetAccountInfo()
    End Sub


    Private Sub GetAccountInfo()
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

        Dim viewState As String
        Dim eventValidation As String

        Dim strFilteredInner As String = ""
        Dim strFilteredInner2 As String = ""

        Dim arrFields() As String

        Dim strloc As String = "https://www.pagepluscellular.com/dealerlogin.aspx "


        strloc = "http://www.pagepluscellular.com/dealerlogin.aspx "

        myCookieContainer4 = New CookieContainer

        'Response.Write("<br><br>*********** Account:" & row.Item("Accountname") & " ********************")

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
        strRequest = "username=admin@ppc25744&password=WjcZC5@p" '& strTemp  '& vbCrLf

        'strRequest = "username=" & row.Item("AccountName") & "&password=" & row.Item("password") '& strTemp  '& vbCrLf


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


        objRequest = CType(WebRequest.Create("http://www.pagepluscellular.com/dealers/my%20account/dealer%20account%20summary.aspx"), HttpWebRequest)
        objRequest.Method = "GET"
        objRequest.CookieContainer = myCookieContainer4

        objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
        strString = srResponse.ReadToEnd()
        Trace.Warn("strString " & strString)
        srResponse.Close()

        objResponse.Close()


        Response.Write(strString)





        objRequest = CType(WebRequest.Create("http://www.pagepluscellular.com/Dealers/My%20Account/Dealer%20Tools/Account%20Status.aspx"), HttpWebRequest)
        objRequest.Method = "GET"
        objRequest.CookieContainer = myCookieContainer4

        objResponse = CType(objRequest.GetResponse(), System.Net.HttpWebResponse)
        srResponse = New StreamReader(objResponse.GetResponseStream(), Encoding.ASCII)
        strString = srResponse.ReadToEnd()
        Trace.Warn("strString " & strString)
        srResponse.Close()

        objResponse.Close()


        Response.Write(strString)


        viewState = ExtractViewState(strString)
        eventValidation = ExtractEventValidation(strString)
        '  Dim strPhoneAccount As String = "504384"
        strRequest =
              String.Format(
                 "__EVENTTARGET=ctl07$ddlPhoneAccounts&__EVENTARGUMENT=&__LASTFOCUS=&__VIEWSTATE={0}&__EVENTVALIDATION={1}&ctl01$GlobalMenu1$hdnIsUserLogged=Yes&ctl07$ddlPhoneAccounts={2}&ctl07$ddlSelectDateRange=0&ctl07$ddlPageSize=40",
                 viewState, eventValidation, strPhoneAccount
              )



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



        Response.Write("<br><br>*******Successfully retrieved data*********")

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
