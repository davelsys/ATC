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
        Dim planid = context.Request.QueryString("planid")
        
        LOG.WriteLog(context.Request.Url.ToString())
        LOG.WriteLog("----------Get Card On File: ivrid=" + ivrid + " mid= " + mid + " planId= " + planid)

        
        Try
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_GetCardOnFile"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon
            
            sqlcmd.Parameters.AddWithValue("@curplan", planid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@mid", mid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.Add("@lastfour", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            'sqlcmd.Parameters.Add("@custprof", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            'sqlcmd.Parameters.Add("@payprof", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.ExecuteNonQuery()

            Dim lastfour = sqlcmd.Parameters("@lastfour").Value
            'Dim custprof = sqlcmd.Parameters("@custprof").Value
            'Dim payprof = sqlcmd.Parameters("@payprof").Value
            
            'sqlcmd.CommandText = String.Format("Update ivr.dbo.IVR_CALLS set curcc='{0}',curplan={1} where iverid = {2}", lastfour, mid, ivrid)
            'sqlcmd.ExecuteNonQuery()
            
            sqlcon.Close()
            
            LOG.WriteLog("Get Card On File: lastfour: " & lastfour)

            Dim xmlres As String = IVR.XML_Result(IVR.XML("lastfour", lastfour))
            '+ IVR.XML("custprof", custprof) + IVR.XML("payprof", payprof))
            
            

            context.Response.Write(xmlres)

        Catch ex As Exception
            LOG.WriteLog("lastfour - Error: " & ex.Message)
            context.Response.Write("Error :" + ex.Message)
        End Try
        
        LOG.WriteLog("---End Get Card On File--")
    End Sub
    
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class