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
        Dim cccode = context.Request.QueryString("cccode")
        
        Try
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_AddCCCode"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon

            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@cccode", cccode).Direction = Data.ParameterDirection.Input
        
            sqlcmd.ExecuteNonQuery()

       
            sqlcon.Close()

            Dim xmlres As String = IVR.XML_OK()

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