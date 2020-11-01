Imports Microsoft.VisualBasic

Imports System.Net
Imports System.IO
Imports System.Data.SqlClient
Public Class ATC_Status
    
    Public Shared Sub ATCStatus(CellNum As String)

        Dim conn As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmdstr As String = ""

        ' Dim ConfNum As String = ""
        Dim ConfStatus As String = ""
        'Dim PinAmt As String = ""
        Dim ratePlan As String = ""

        Try
            
            'Dim connstr As String = "Server=(local);Database=ppc;User ID=sa3;Password=davel;Asynchronous Processing=true"
            'conn.ConnectionString = connstr


            Dim Daymin, NightMin, WEmin, MobileMin, RemainingMin As Integer
            Dim IsUnlimited As Boolean
            Dim planExp As Nullable(Of DateTime) = Nothing

            ATCQuery(CellNum, Daymin, NightMin, WEmin, MobileMin, RemainingMin, IsUnlimited, ConfStatus, planExp)

            conn.Open()
            If IsUnlimited Then
                ratePlan = "Unlimited"
            End If

            cmdstr = String.Format("Update ATC_MDN set Status='{0}', lastModified=getdate(), ratePlan='{1}', planExpDate='{2}' where PhoneNumber='{3}'", ConfStatus, ratePlan, planExp, CellNum)
            Dim cmd As New SqlCommand(cmdstr, conn)
            cmd.ExecuteNonQuery()

        Catch ex As Exception
            Console.WriteLine(ex.Message)

            Return
        End Try

    End Sub

    Public Shared Sub ATCQuery(ByVal Phonenum As String, ByRef DayMin As Integer, ByRef NightMin As Integer, ByRef WEmin As Integer, ByRef MobileMin As Integer, ByRef RemainingMin As Integer, ByRef IsUnlimited As Boolean, ByRef confStatus As String, ByRef planExp As String)

        Dim SampleResponseStr As String = "CANCHECK_STATUS=eCanCheck,DAYTIME_MINUETS=9988,NIGHTTIME_MINUETS=0,WEEKEND_MINUETS=0,MOBILE_MINUETS=0,IS_PREPAID=Yes,REMAINING_MINUTES=100,IS_UNLIMITED=Yes,IS_AUTOPAY=No,AUTOPAY_DAY=0"
        Dim loginID As String = "Jack!12$34"

        Dim Str As String = String.Format("http://75.99.53.250/CVWebService/PhoneHandler.aspx?method=CanCheckBalance&phonenumber={0}&login={1}", Phonenum, loginID)
        Dim request As WebRequest = WebRequest.Create(Str)
        request.Credentials = CredentialCache.DefaultCredentials

        Dim response As WebResponse = request.GetResponse()
        'Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)

        Dim dataStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim ResponseStr As String = reader.ReadToEnd()

        Dim res As String() = ResponseStr.Split(",")
        If res.Length > 1 Then
            Dim ConfStr As String() '= res(0).Split("=")
            confStatus = "Active"
            ConfStr = res(1).Split("=")
            DayMin = Convert.ToInt32(ConfStr(1))
            ConfStr = res(2).Split("=")
            NightMin = Convert.ToInt32(ConfStr(1))
            ConfStr = res(3).Split("=")
            WEmin = Convert.ToInt32(ConfStr(1))
            ConfStr = res(4).Split("=")
            MobileMin = Convert.ToInt32(ConfStr(1))
            ConfStr = res(6).Split("=")
            RemainingMin = Convert.ToInt32(ConfStr(1))
            ConfStr = res(7).Split("=")
            Dim strIsUnlimited As String = ConfStr(1)
            If strIsUnlimited.ToLower = "yes" Then
                IsUnlimited = True
            Else
                IsUnlimited = False
            End If
            ConfStr = res(8).Split("=")
            If Not ConfStr(1) = "0" Then
                planExp = Convert.ToDateTime(ConfStr(1))
            End If
        Else
            confStatus = "Error"
        End If


        reader.Close()
        response.Close()
    End Sub

   
End Class
