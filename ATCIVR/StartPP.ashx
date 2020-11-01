<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient
Imports System.ComponentModel


Public Class Handler : Implements IHttpHandler

    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
              
        Dim mid As String = context.Request.QueryString("mid").ToString
        Dim xmlres = IVR.XML_EMPTY
        
        LOG.WriteLog("Start PP ", mid)
        
       If (CheckCurrentStatus(mid) Or CheckLastHour(mid)) Then
            xmlres = IVR.XML_OK
       Else
            Dim bw As BackgroundWorker = New BackgroundWorker()
            AddHandler bw.DoWork, AddressOf bw_DoWork
            bw.RunWorkerAsync(mid)
            xmlres = IVR.XML_EMPTY
       End If
        
        LOG.WriteLog(context.Request.Url.ToString(), xmlres)
        context.Response.Write(xmlres)
    End Sub
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
    
    Private Sub bw_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        
        
        Dim mid As String = DirectCast(e.Argument, String)
        
        'LOG.WriteLog("in do work function", mid)
        
        Dim sqlcon As New SqlConnection()
        sqlcon.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;database=ppc"
        Dim sqlcmd As New SqlCommand()
        sqlcmd.CommandText = "run_mdn_query"
        sqlcmd.CommandType = CommandType.StoredProcedure
        sqlcmd.Connection = sqlcon
        sqlcon.Open()

        sqlcmd.Parameters.AddWithValue("@cellnum", mid).Direction = Data.ParameterDirection.Input

        sqlcmd.ExecuteNonQuery()
        
        sqlcon.Close()
    End Sub
    
     Private Function CheckCurrentStatus(mid As String) As Boolean
     
        Dim sqlcon As New SqlConnection()
        sqlcon.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;database=ppc"
        Dim sqlcmd As New SqlCommand()
        sqlcmd.CommandText = "declare @updated bit select @updated = case when ISNULL(expdate,planexpdate) > Getdate() and RatePlan like '%unlimited%' then 1 else 0 end from MDN where Phonenumber = @mid select @updated"
        sqlcmd.Parameters.AddWithValue("@mid", mid)
        sqlcmd.Connection = sqlcon
        sqlcon.Open()
        Return CBool(sqlcmd.ExecuteScalar)
        sqlcon.Close()
    End Function
    
    Private Function CheckLastHour(mid As String) As Boolean
        Dim sqlcon As New SqlConnection()
        sqlcon.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;database=ppc"
        Dim sqlcmd As New SqlCommand()
        sqlcmd.CommandText = "declare @updated bit select @updated = case when lastModified >= dateadd(hh,-1,getdate()) then 1 else 0 end from MDN where Phonenumber = @mid select @updated"
        sqlcmd.Parameters.AddWithValue("@mid", mid)
        sqlcmd.Connection = sqlcon
        sqlcon.Open()
        Return CBool(sqlcmd.ExecuteScalar)
        sqlcon.Close()
    End Function
    
End Class