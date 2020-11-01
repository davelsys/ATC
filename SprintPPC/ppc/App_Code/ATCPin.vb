Imports Microsoft.VisualBasic

Imports System.Net
Imports System.IO
Imports System.Data.SqlClient

'**************IMPORTANT: CHANGES MADE TO THIS FILE MUST BE COPIED OVER TO CLASS1.VB FILE IN C:\ATCBACKGROUND PROJECT************************************

Public Class ATCPin
    '----------------------------ATC PIN FUNCTIONS to assign this cellnumber to a pin in pintable and on vendor side----------------------------------------
    Public Shared Sub AssignATCPin(CellNum As String, PinType As String, transID As Integer, ByRef statusMsg As String)
        Try
            Dim cmd As New SqlCommand
            Dim reader As SqlDataReader
            cmd.Connection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            cmd.CommandType = CommandType.Text
            'cmd.CommandText = "select * from atc_pins where ((Assigned is not NULL and applied is NULL) or (Assigned is not NULL and DATEADD(d,30,applied)> GETDATE())) and CellNum = @cellnum; "
            cmd.CommandText = "select p.* from atc_pins p join ATC_MDN m on p.CellNum = m.phonenumber where ((p.Assigned is not NULL and p.applied is NULL) or (p.Assigned is not NULL and m.planExpDate > GETDATE())) and p.CellNum = @cellnum; "
            cmd.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = CellNum
            cmd.Connection.Open()
            reader = cmd.ExecuteReader

            'only assign pin if applied date for this cell number (from pins table) is null or applied+30 days is later than today (basically only pull a pin if there is no active pin for this cell number already... active pin meaning pin assigned to this cell number but not applied yet (on vendor side), or not yet expired)
            If reader.HasRows Then
                statusMsg = "There is already an active pin for this cell number."
                addToStack(CellNum, statusMsg)
            Else
                Dim PinStr As String = GetPin(PinType, statusMsg)
                If PinStr = "" Then
                    'if no available pin, then add to stack
                    statusMsg = "No available pin."
                    'addToStack(CellNum, statusMsg)
                Else
                    Dim success As Boolean = AssignPinDB(CellNum, PinStr, PinType, transID, statusMsg)
                    'if row was updated in pins table- that the cell number was assigned to the pin (success=true), then we can go on and send that cell number and pin to vendor etc. Otherwise(success=false), add to stack.
                    If success Then
                        AssignPin(CellNum, PinStr, PinType, statusMsg)
                    Else
                        statusMsg = "Error: CellNum was not assigned to pin in Pins table."
                        'addToStack(CellNum, statusMsg)
                    End If
                End If
            End If

            cmd.Connection.Close()
        Catch ex As Exception
            statusMsg = "Error when checking for cell number in pins table. " & ex.ToString
        End Try
    End Sub
    Private Shared Sub UpdateAuthTrans(status As String, transid As Integer)
        Try
            Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
            Dim strsql As String = ""
            If status = "Failed" Then
                'strsql = String.Format("update authtrans set month_processed = case when isnull(month_plan_id, 0) > 0 then null else month_processed end, cash_processed = case when ISNULL(cash_plan_id, 0) > 0 then null else cash_processed end where transid = '{0}'", transid)
                strsql = String.Format("update authtrans set month_processed = null where transid = '{0}'", transid)
            ElseIf status = "Successful" Then
                'strsql = String.Format("update authtrans set month_processed = case when isnull(month_plan_id, 0) > 0 then getdate() else month_processed end, cash_processed = case when ISNULL(cash_plan_id, 0) > 0 then getdate() else cash_processed end where transid = '{0}'", transid)
                strsql = String.Format("update authtrans set month_processed = getdate() where transid = '{0}'", transid)
            End If

            conn.Open()
            Dim cmdAuthTrans As New SqlCommand(strsql, conn)
            cmdAuthTrans.ExecuteNonQuery()
            conn.Close()
        Catch ex As Exception

        End Try

    End Sub
    Public Shared Function AssignPinDB(CellNum As String, PinStr As String, PinType As String, transid As Integer, ByRef statusMsg As String) As Boolean
        Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim rowsAffected As Integer
        Dim success As Boolean
        Try
            Dim comm As String = ""
            conn.Open()
            comm = String.Format("Update ATC_Pins set CellNum='{0}',Assigned=getdate() where Pin='{1}'", CellNum, PinStr)
            Dim cmdDB As New SqlCommand(comm, conn)
            rowsAffected = cmdDB.ExecuteNonQuery() 'see if row is updated in pins table
            conn.Close()
        Catch ex As Exception
            statusMsg = "Error when assigning cell to pin in Pins table. " & ex.ToString
        End Try

        If rowsAffected > 0 Then
            success = True
            UpdateAuthTrans("Successful", transid)
        Else
            success = False
            UpdateAuthTrans("Failed", transid)
        End If

        Return success
    End Function
    Public Shared Sub AssignPin(CellNum As String, PinStr As String, PinType As String, ByRef statusMsg As String)
        Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmdstr As String = ""

        Try
            ' assign the pin on the vendor side and update pins table with the appropriate info
            Dim Pinnum As String
            Dim cmdstr2 As String = ""
            Dim ConfNum As String = ""
            Dim ConfStatus As String = ""
            Dim PinAmt As String = ""
            Dim applied As String = vbNullString

            Pinnum = PinStr.Replace(" ", "") 'get rid of spaces in the pin to use it in the url
            AssignVendorPin(CellNum, Pinnum, ConfNum, ConfStatus, PinAmt, statusMsg, applied)


            'update pins table, set all relevant fields
            conn.Open()
            cmdstr2 = String.Format("Update ATC_Pins set Status='{0}',Confirmation_Num='{1}',applied=nullif(convert(datetime,'{2}'), ''),Amount='{3}' where Pin='{4}' and CellNum='{5}'", ConfStatus, ConfNum, applied, PinAmt, PinStr, CellNum)
            Dim cmd2 As New SqlCommand(cmdstr2, conn)
            Dim rowsAffected2 As Integer = cmd2.ExecuteNonQuery()
            conn.Close()
            If Not rowsAffected2 > 0 Then
                statusMsg = "This pin and cell was not found in Pins table to update."
            End If

            ATCStatus(CellNum, statusMsg)

        Catch ex As Exception
            statusMsg = "Error when updating ATC_Pins table. " & ex.ToString
            Console.Write(ex.Message)

        End Try

    End Sub

    'this functions assigns the pin to the cell number on the vendor side
    Protected Shared Sub AssignVendorPin(ByVal Phonenum As String, ByVal PinNum As String, ByRef ConfNum As String, ByRef ConfStatus As String, ByRef ConfAmount As String, ByRef statusMsg As String, ByRef applied As String)

        Dim SampleResponseStr As String = "CHARGE_STATUS=SUCCESS,CONFIRMATION_NUMBER=64762,Charge_amount=2"
        Dim loginID As String = "Jack!12$34"

        Dim Str As String = String.Format("http://75.99.53.250/CVWebService/PhoneHandler.aspx?method=ChargePVC&phonenumber={0}&pvcnumber={1}&login={2}", Phonenum, PinNum, loginID)
        Dim request As WebRequest = WebRequest.Create(Str)
        request.Credentials = CredentialCache.DefaultCredentials

        Dim response As WebResponse = request.GetResponse()
        'Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)

        Dim dataStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim ResponseStr As String = reader.ReadToEnd()

        Dim res As String() = ResponseStr.Split(",")
        If res.Length > 1 Then
            Dim ConfStr As String() = res(0).Split("=")
            ConfStatus = ConfStr(1)
            ConfStr = res(1).Split("=")
            ConfNum = ConfStr(1)
            ConfStr = res(2).Split("=")
            ConfAmount = ConfStr(1)
        Else
            ConfStatus = res(0)
            statusMsg = res(0)
        End If

        reader.Close()
        response.Close()

        If ConfStatus = "SUCCESS" Then
            ConfStatus = "OK"
            applied = CStr(Date.Today)
            'updateExpDateAndStack(Phonenum, statusMsg)
        End If
    End Sub

    Public Shared Function GetPin(PinType As String, ByRef statusmsg As String) As String
        Dim pinStr As String = ""
        Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmdstr As String = ""

        Try
            'when assigning pin for first time, (not on retry triggered from pins page) then first have to get the pin to assign cell number to it
            conn.Open()
            cmdstr = String.Format("select top 1 pin from ATC_Pins where pintype='{0}' and cellnum is null and assigned is null order by id", PinType)
            Dim cmd As New SqlCommand(cmdstr, conn)
            Dim rdr As SqlDataReader
            rdr = cmd.ExecuteReader()

            If rdr.Read() And rdr.HasRows() Then
                pinStr = GetStrValue(rdr, "pin")
            End If

            rdr.Close()
            conn.Close()


        Catch ex As Exception
            statusmsg = "Error when retrieving pin." & ex.ToString
        End Try


        Return pinStr
    End Function
    Protected Shared Sub addToStack(cellNumber As String, ByRef statusmsg As String)
        'once response came back successful, minus one from pincount, and update expdate
        Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmdstr As String = ""
        Try
            conn.Open()    'HAVE TO UPDATE ATC_MDN SO THAT EXPDATE = ADD 31 DAYS!!!!
            cmdstr = "Update ATC_MDN set pincount = isnull([pincount], 0) + 1, lastModified = getdate() where phonenumber = @cellnum  "
            Dim cmd As New SqlCommand(cmdstr, conn)
            cmd.Parameters.Add("date", SqlDbType.DateTime).Value = Date.Today
            cmd.Parameters.Add("cellnum", SqlDbType.VarChar).Value = cellNumber
            Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
            conn.Close()
            '**************************************************should i check if any record was updated to tell if the cell number actually existed in Mdn table????????????
            If Not rowsAffected > 0 Then
                statusmsg = "This cell number does not exist in the mdn table."
            End If
        Catch ex As Exception
            statusmsg = "Error when updating stack in MDN table. " & ex.ToString
            Console.Write(ex.Message)
            Return
        End Try
    End Sub
    Public Shared Sub subtractFromStack(cellNumber As String, ByRef statusmsg As String)
        'once response came back successful, minus one from pincount, and update expdate
        Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmdstr As String = ""
        Try
            conn.Open()
            cmdstr = "Update ATC_MDN set pincount = isnull([pincount], 0) - 1, lastModified = getdate() where phonenumber = @cellnum  "
            Dim cmd As New SqlCommand(cmdstr, conn)
            cmd.Parameters.Add("date", SqlDbType.DateTime).Value = Date.Today
            cmd.Parameters.Add("cellnum", SqlDbType.VarChar).Value = cellNumber
            Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
            conn.Close()
            '**************************************************should i check if any record was updated to tell if the cell number actually existed in Mdn table????????????
            If Not rowsAffected > 0 Then
                statusmsg = "This cell number does not exist in the mdn table."
            End If
        Catch ex As Exception
            statusmsg = "Error when updating stack in MDN table. " & ex.ToString
            Console.Write(ex.Message)
            Return
        End Try
    End Sub

    Protected Shared Function GetStrValue(ByRef rdr As SqlDataReader, ByRef Field As String) As String
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            'statusmsg = "reader was null"
            Return ""
        Else
            'statusmsg = "reader was NOT null"
            Return rdr.GetValue(rdr.GetOrdinal(Field)).ToString()
        End If
    End Function
    Protected Shared Function GetIntValue(ByRef rdr As SqlDataReader, ByRef Field As String) As Int32
        If IsDBNull(rdr.GetValue(rdr.GetOrdinal(Field))) Then
            Return 0
        Else
            Return rdr.GetValue(rdr.GetOrdinal(Field))
        End If
    End Function


    '----------------------------ATC STATUS FUNCTIONS to check status of this phone number on vendor side----------------------------------------

    Public Shared Sub ATCStatus(CellNum As String, ByRef statusMsg As String)
        Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmdstr As String = ""
        Dim ConfStatus As String = ""
        Dim ratePlan As String = ""

        Try
            Dim Daymin, NightMin, WEmin, MobileMin As Integer
            Dim RemainingMin As String = ""
            Dim IsUnlimited As Boolean
            Dim planExp As String = vbNullString

            ATCQuery(CellNum, Daymin, NightMin, WEmin, MobileMin, RemainingMin, IsUnlimited, ConfStatus, planExp, ratePlan, statusMsg)
            If IsUnlimited Then
                ratePlan = "Unlimited"
                RemainingMin = "Unlimited"
            ElseIf RemainingMin <> "" Then
                ratePlan = "Minutes"
            End If

            If planExp = vbNullString Then
                planExp = ""
            End If

            conn.Open()
            cmdstr = String.Format("Update ATC_MDN set Status='{0}', lastModified=getdate(), ratePlan='{1}', planExpDate= case when '{2}' = '' then planExpDate else nullif(convert(datetime,'{2}'), '') end, PlanBalanceDetailsMin = '{3}' where PhoneNumber='{4}'", ConfStatus, ratePlan, planExp, RemainingMin, CellNum)
            Dim cmd As New SqlCommand(cmdstr, conn)
            cmd.ExecuteNonQuery()
            conn.Close()

        Catch ex As Exception
            statusMsg += " Error when updating ATC_MDN table. " & ex.ToString()
            Console.WriteLine(ex.Message)
            Return
        End Try

    End Sub


    Protected Shared Sub ATCQuery(ByVal Phonenum As String, ByRef DayMin As Integer, ByRef NightMin As Integer, ByRef WEmin As Integer, ByRef MobileMin As Integer, ByRef RemainingMin As String, ByRef IsUnlimited As Boolean, ByRef confStatus As String, ByRef planExp As String, ByRef rateplan As String, ByRef statusmsg As String)
        Try
            'CANCHECK_STATUS=eCanCheck,DAYTIME_MINUETS=1,NIGHTTIME_MINUETS=0,WEEKEND_MINUETS=0,MOBILE_MINUETS=0,IS_PREPAID=Yes,REMAINING_MINUTES=6,IS_UNLIMITED=No,IS_AUTOPAY=No,AUTOPAY_DAY=0,DUE_DATE=04/06/2014
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
                RemainingMin = ConfStr(1)
                ConfStr = res(7).Split("=")
                Dim strIsUnlimited As String = ConfStr(1)
                If strIsUnlimited.ToLower = "yes" Then
                    IsUnlimited = True
                Else
                    IsUnlimited = False
                End If
                'IsUnlimited = True
                ConfStr = res(10).Split("=")
                If Not ConfStr(1) = "0" Then
                    planExp = ConfStr(1)
                End If

            Else
                If res(0) = "CANCHECK_STATUS=eCantCheckNRP" Then
                    confStatus = "Active"
                    rateplan = "No Rate Plan"
                Else
                    confStatus = "Error"
                    statusmsg += " " & res(0)
                End If
            End If

            reader.Close()
            response.Close()
        Catch ex As Exception
            statusmsg += " " & ex.ToString
        End Try
    End Sub


    '----------------------------ATC VERIFY FUNCTION to see if phone is valid on verizon side ----------------------------------------
    Public Shared Sub ATCVerify(CellNum As String, ByRef statusmsg As String, ByRef varLastValidated As String, ByRef valid As String)
        'Dim valid As String = ""
        Dim SampleResponseStr As String = "CANCHECK_STATUS=eCanCheck,DAYTIME_MINUETS=9988,NIGHTTIME_MINUETS=0,WEEKEND_MINUETS=0,MOBILE_MINUETS=0,IS_PREPAID=Yes,REMAINING_MINUTES=100,IS_UNLIMITED=Yes,IS_AUTOPAY=No,AUTOPAY_DAY=0"
        Dim secretCode As String = "3479"
        Dim loginID As String = "Jack!12$34"

        Dim Str As String = String.Format("http://75.99.53.250/CVWebService/PhoneHandler.aspx?method=Verification&phonenumber={0}&secretcode={1}&login={2}", CellNum, secretCode, loginID)
        Dim request As WebRequest = WebRequest.Create(Str)
        request.Credentials = CredentialCache.DefaultCredentials

        Dim response As WebResponse = request.GetResponse()
        'Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)

        Dim dataStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim ResponseStr As String = reader.ReadToEnd()

        Dim res As String() = ResponseStr.Split(",")
        If res(0) <> "" Then
            If res(0) = "CODE_STATUS=Verified" Then
                valid = "Valid"
            Else
                statusmsg = res(0)
                valid = "Not Validated"
            End If
        Else
            statusmsg = "No result from Verification URL that was sent."
        End If
        reader.Close()
        response.Close()


        Dim conn As SqlConnection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        Dim cmdstr As String = ""
        Try
            conn.Open()
            'cmdstr = String.Format("Update Orders set ESNChecked=getdate() where cell_num='{0}'", CellNum)
            cmdstr = String.Format("Update ATC_MDN set Verify='{0}', VerifyDate=getdate() where PhoneNumber='{1}'", valid, CellNum)
            Dim cmd As New SqlCommand(cmdstr, conn)
            cmd.ExecuteNonQuery()
            conn.Close()
            varLastValidated = CStr(Date.Now)
        Catch ex As Exception
            'statusMsg = ex.ToString()
            statusmsg = "Error when updating ATC_MDN table. " & ex.ToString()
            'Console.WriteLine(ex.Message)
        End Try

        'Return valid

    End Sub

    '---------------------------- FUNCTION TO CHANGE ESN ON VENDOR SIDE ----------------------------------------
    Public Shared Function ATCChangeEsn(cellnum As String, oldEsn As String, newEsn As String) As Boolean
        Dim esnChanged As Boolean = False
        Dim loginID As String = "Jack!12$34"

        Dim sampleResult As String = "CANCHECK_STATUS=eCanCheck,DAYTIME_MINUETS=9508,NIGHTTIME_MINUETS=0,WEEKEND_MINUETS=0,MOBILE_MINUETS=0,IS_PREPAID=Yes,REMAINING_MINUTES=0,IS_UNLIMITED=Yes,IS_AUTOPAY=No,AUTOPAY_DAY=0,DUE_DATE=03/24/2014"
        Dim Str As String = String.Format("http://75.99.53.250/CVWebService/PhoneHandler.aspx?method=ChangeESN&phonenumber={0}&oldEsn={1}&newEsn={2}&login={3}", cellnum, oldEsn, newEsn, loginID)
        Dim request As WebRequest = WebRequest.Create(Str)
        request.Credentials = CredentialCache.DefaultCredentials

        Dim response As WebResponse = request.GetResponse()
        'Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)

        Dim dataStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim ResponseStr As String = reader.ReadToEnd()

        Dim res As String() = ResponseStr.Split(",")
        If res(0).ToLower = "success" Then
            esnChanged = True
        Else
            esnChanged = False
        End If


        'If res.Length > 1 Then
        '    Dim ConfStr As String() '= res(0).Split("=")
        '    ConfStr = res(1).Split("=")
        '    esnChanged = True
        'Else
        '    esnChanged = False
        'End If

        reader.Close()
        response.Close()
        Return esnChanged


    End Function

    '---------------------------- FUNCTION TO CONVERT TO ALL TALK ON VENDOR SIDE ----------------------------------------
    Public Shared Sub ConvertAT(cellNumber As String, esn As String, varOrderId As Integer, ByRef statusMsg As String)
        'Dim secretCode As String = "3479"

        Dim loginID As String = "Jack!12$34"
        Dim last4Digits As String = Right(cellNumber, 4)
        Dim vendorReceived As Boolean

        Dim Str As String = String.Format("http://75.99.53.250/CVWebService/PhoneHandler.aspx?method=port&phonenumber={0}&login={1}&esn={2}&account={3}&passCode={4}", cellNumber, loginID, esn, cellNumber, last4Digits)
        'statusMsg = Str

        Dim request As WebRequest = WebRequest.Create(Str)
        request.Credentials = CredentialCache.DefaultCredentials

        Dim response As WebResponse = request.GetResponse()
        'Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)

        Dim dataStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim ResponseStr As String = reader.ReadToEnd()

        Dim res As String() = ResponseStr.Split(",")
        If res(0).ToLower = "success" Then
            vendorReceived = True
            statusMsg = "vendorReceived=True res:" & res(0)
        Else
            vendorReceived = False
            statusMsg = "vendorReceived=False res:" & res(0)
        End If
        reader.Close()
        response.Close()


        'if phonenumber to be converted went through to the vendor, then update it in database too.
        If vendorReceived Then
            Try
                Dim cmd As New SqlCommand
                cmd.Connection = New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.CommandText = "ATC_ConvertAT"
                cmd.Parameters.Add("@orderid", SqlDbType.Int).Value = varOrderId
                cmd.Connection.Open()
                cmd.ExecuteNonQuery()
                cmd.Connection.Close()
                ATCStatus(cellNumber, statusMsg)
                'statusmsg = "successfully converted at database"
            Catch ex As Exception
                statusMsg = "error: " & ex.ToString
            End Try
        Else
            statusMsg = "Vendor not responding with success."
        End If
    End Sub

End Class
