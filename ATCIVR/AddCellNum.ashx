<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient


Public Class Handler : Implements IHttpHandler
    
   
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
        
        Dim ivrid = context.Request.QueryString("ivrid")
        Dim mid = context.Request.QueryString("mid")
        
        Dim xmlres As String = IVR.XML_OK

        Try
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_AddCellNum"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon

            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@mid", mid).Direction = Data.ParameterDirection.InputOutput
        
            sqlcmd.ExecuteNonQuery()
            sqlcon.Close()

            mid = sqlcmd.Parameters("@mid").Value.ToString()
            
            If mid = "0" Then
                xmlres = IVR.XML_EMPTY()
            End If

        Catch ex As Exception
            xmlres = IVR.XML_ERROR(ex.Message)
        End Try
        
        LOG.WriteLog(xmlres)
        context.Response.Write(xmlres)
        
    End Sub
    
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class