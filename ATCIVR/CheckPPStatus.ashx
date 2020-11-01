<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient
Imports System.Threading



Public Class Handler : Implements IHttpHandler
    
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
      'Try
        Dim mid As String = context.Request.QueryString("mid").ToString  
        Dim xmlres = IVR.XML_EMPTY
        
        LOG.WriteLog("Check PP Status: mid ", mid)
       
        Dim checkTime As DateTime = Now
        Dim refreshed As Integer = 0
      
        If Not CheckProgress(mid, checkTime) Then
            xmlres = XML_Result(XML("refreshed", refreshed.ToString()))
        Else
            refreshed = 1
            xmlres = XML_Result(XML("refreshed", refreshed.ToString()))
        End If
        
       'Catch ex As Exception
            'xmlres = XML_Result(XML("status", "ERROR") + XML("msg", ex.Message))
       'End Try
        
        'context.Response.Write(xmlres)
       
        LOG.WriteLog(context.Request.Url.ToString(), xmlres)
        context.Response.Write(xmlres)
    End Sub
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
    
    
    Public Function CheckProgress(mid As String, ByVal checkTime As DateTime) As Boolean
        
        Dim sqlcon As New SqlConnection()
        sqlcon.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;database=ppc"
        Dim sqlcmd As New SqlCommand()
        sqlcmd.Connection = sqlcon
        sqlcmd.CommandText = "Select [lastModified] from MDN where [phonenumber] = @mid"
        'sqlcmd.CommandText = "Select getdate()"
        sqlcmd.Parameters.AddWithValue("@mid", mid)
        'Dim rdr As SqlDataReader
        
        sqlcon.Open()
        
        Dim LastModified As DateTime = New DateTime()
        
        LastModified = DirectCast(sqlcmd.executeScalar(), DateTime)
        
        sqlcon.Close()
        
        Return LastModified.toShortDateString() = checkTime.toShortDateString() And LastModified.Hour >= checkTime.Hour
    End Function
End Class


