<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient


Public Class Handler : Implements IHttpHandler
    
   
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
        
        LOG.WriteLog(context.Request.Url.ToString())
        LOG.WriteLog("------------------Validate CC EXP")
        
    Dim ivrid = context.Request.QueryString("ivrid")
        Dim newexp = context.Request.QueryString("newexp")
        Dim xmlres As String = ""
        
        Try
            
          
            Dim ValidExp As Integer = 0
            Dim mon As Integer = newexp.Substring(0, 2)
            Dim yr As Integer = newexp.Substring(2, 2)
            If mon > 0 And mon < 13 And yr > 14 And yr < 30 Then
                ValidExp = 1
            End If
        
            xmlres = XML_Result(XML("mon", mon.ToString()) + XML("yr", yr.ToString()) + XML("nexpvalid", ValidExp.ToString()))
         
        Catch ex As Exception
            xmlres = XML("msg", ex.Message)
        End Try
        
        LOG.WriteLog(context.Request.Url.ToString(), xmlres)
        context.Response.Write(xmlres)
        LOG.WriteLog("--END Validate CC EXP--")
    End Sub
    
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
    
End Class