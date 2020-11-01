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
        LOG.WriteLog("------------Validate CCCode")
        
    Dim ivrid = context.Request.QueryString("ivrid")
        Dim newcccode = context.Request.QueryString("newcccode")
        Dim xmlres As String = ""
        
        Try
            
          
            Dim ValidCCcode As Integer = 0
            Dim code As Integer = newcccode
         
            If code > 0 And code < 999 Then
                ValidCCcode = 1
            End If
        
            xmlres = XML_Result(XML("ncccodevalid", ValidCCcode.ToString) + XML("code", code.ToString()) )
           
        Catch ex As Exception
            xmlres = XML("msg", ex.Message)
        End Try
        
        LOG.WriteLog(context.Request.Url.ToString(), xmlres)
        context.Response.Write(xmlres)
        LOG.WriteLog("--End Validate CCCode--")
    End Sub
    
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
    
End Class