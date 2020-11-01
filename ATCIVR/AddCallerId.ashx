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
        Dim cid = context.Request.QueryString("cid")
        
         If cid.Length > 10 Then  'SG 08/09/17
            cid = cid.Remove(0, 1)
        End If
        
        Dim mid As String = ""
        LOG.WriteLog(context.Request.Url.ToString())
        LOG.WriteLog("----------Add Caller ID: IVRID = " + ivrid)     
        Dim xmlres As String = ""
        
        Try
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_AddCallerId"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon
            
            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            'sqlcmd.Parameters.AddWithValue("@cid", cid.Remove(0,1)).Direction = Data.ParameterDirection.Input 'SG 08/09/17 only if length > 10 truncate the 1
            sqlcmd.Parameters.AddWithValue("@cid", cid).Direction = Data.ParameterDirection.Input
            
            Dim midParam As New SqlParameter("@mid", SqlDbType.VarChar, 20)
            midParam.Direction = System.Data.ParameterDirection.Output
            sqlcmd.Parameters.Add(midParam)
                     
            LOG.WriteLog("Add Caller ID =" + cid)
            
            sqlcmd.ExecuteNonQuery()
                   

            sqlcon.Close()
          
            mid = sqlcmd.Parameters("@mid").Value.ToString()
           
            LOG.WriteLog("Add Caller ID: mid=" & mid)
            
            'If mid = "0" Then
                'xmlres = IVR.XML_EMPTY()
            'Else
                xmlres = XML_Result(XML("mid", mid))
                'xmlres = IVR.XML_OK
            'End If
            
                       
        Catch ex As Exception
            xmlres = IVR.XML_ERROR(ex.Message)
        End Try
        
        LOG.WriteLog(context.Request.Url.ToString(), xmlres)
        'LOG.WriteLog("mid=" & mid)
        context.Response.Write(xmlres)
        LOG.WriteLog("--END get callerID--")
    End Sub
    
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class