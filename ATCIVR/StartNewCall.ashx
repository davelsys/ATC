<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient


Public Class Handler : Implements IHttpHandler
    
   
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
         LOG.WriteLog("------------------Start New Call----------------------")    
        
        Try
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_StartNewCall"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon
            
            
            Dim param as SqlParameter
            param = sqlcmd.Parameters.Add("@ivrid", SqlDbType.Int)
            param.Direction = ParameterDirection.Output          
            
            
            
            
            'sqlcmd.Parameters.AddWithValue("@ivrid", IvrID.ToString()).Direction = Data.ParameterDirection.Output

            sqlcmd.ExecuteNonQuery()
            
            Dim IvrID As Integer = -1
            IvrID = sqlcmd.Parameters("@ivrid").Value

            sqlcon.Close()

            Dim xmlres As String = IVR.XML_Result(IVR.XML("ivrid", IvrID))
            LOG.WriteLog(xmlres)
            context.Response.Write(xmlres)

        Catch ex As Exception
            context.Response.Write("Error :" + ex.Message)
        End Try
    End Sub
    
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class