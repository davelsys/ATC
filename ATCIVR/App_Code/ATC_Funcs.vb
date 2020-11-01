Imports Microsoft.VisualBasic
Imports System
Imports System.Web
Imports System.Data

Imports System.Data.SqlClient

Public Module IVR

    Function XML(ByVal Arg As String, ByVal Val As String) As String
        Try
            My.Computer.FileSystem.WriteAllText("C:\LOG\IVR_LOG.txt", Now().ToString() + ":" + Arg + ":" + Val + vbNewLine, True)
        Catch ex As Exception
            Threading.Thread.Sleep(1000)
            My.Computer.FileSystem.WriteAllText("C:\LOG\IVR_LOG.txt", Now().ToString() + ":" + Arg + ":" + Val + vbNewLine, True)
        End Try
        Return String.Format("<variable><name>{0}</name><value>{1}</value></variable>", Arg, Val)
    End Function
    Function XML_Result(ByVal Str As String) As String
        Dim begxml As String = "<response><result><ivr_info><variables>"
        Dim endxml As String = "</variables></ivr_info></result></response>"
        Dim xmlres As String = begxml + Str + endxml
        Return xmlres
    End Function
    Function XML_OK() As String
        Return XML_Result(XML("Status", "OK"))
    End Function
    Function XML_EMPTY() As String
        Return XML_Result(XML("Status", "EMPTY"))
    End Function
    Function XML_ERROR(ErrMsg As String) As String
        Return XML_Result(XML("Status", "ERROR") + XML("ErrMsg", ErrMsg))
    End Function
End Module
Public Module ATCDB
    Function OpenDB() As SqlConnection
        Dim sqlcon As New SqlConnection()
        sqlcon.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;MultipleActiveResultSets=True;Initial Catalog=IVR"
        sqlcon.Open()
        Return sqlcon
    End Function

    Sub CloseDB(ByRef sqlcon As SqlConnection)
        sqlcon.Close()
    End Sub
End Module
Public Module LOG
    Sub WriteLog(ByVal Str As String)
        Try
            My.Computer.FileSystem.WriteAllText("C:\LOG\IVR_LOG.txt", Now().ToString() + ":" + Str + vbNewLine, True)
        Catch ex As Exception
            Threading.Thread.Sleep(1000)
            My.Computer.FileSystem.WriteAllText("C:\LOG\IVR_LOG.txt", Now().ToString() + ":" + Str + vbNewLine, True)
        End Try
    End Sub
    Sub WriteLog(ByVal Title As String, ByVal Msg As String)
        Try
            My.Computer.FileSystem.WriteAllText("C:\LOG\IVR_LOG.txt", Now().ToString() + ":" + Title + ":" + Msg + vbNewLine, True)
        Catch ex As Exception
            Threading.Thread.Sleep(1000)
            My.Computer.FileSystem.WriteAllText("C:\LOG\IVR_LOG.txt", Now().ToString() + ":" + Title + ":" + Msg + vbNewLine, True)
        End Try
    End Sub
End Module