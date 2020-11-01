<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient


Public Class Handler : Implements IHttpHandler
    
   
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
      
        Dim ivrid As String = context.Request.QueryString("ivrid")
        Dim mid As String = context.Request.QueryString("mid").ToString
               
       LOG.WriteLog(context.Request.Url.ToString())
        
        LOG.WriteLog("----------Get Balance: ivrid = " + ivrid)
        
        Dim xmlres = IVR.XML_EMPTY
        
              
        Try
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_GetBalance"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon

            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@mid", mid).Direction = Data.ParameterDirection.Input
            'sqlcmd.Parameters.Add("@mid", SqlDbType.VarChar, 20).Direction = Data.ParameterDirection.Input
            'sqlcmd.Parameters("@mid").Value = "3474157046"
            
            sqlcmd.Parameters.Add("@planid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@planname", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@plantype", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@valid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@expdate", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashbal", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@minavail", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@asof", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@stacked", SqlDbType.Int).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@renew", SqlDbType.Int).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashstacked", SqlDbType.Int).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@carrier", SqlDbType.Varchar, 30).Direction = Data.ParameterDirection.Output

           
            sqlcmd.ExecuteNonQuery()

            Dim planid = sqlcmd.Parameters("@planid").Value
            Dim planname = sqlcmd.Parameters("@planname").Value
            Dim plantype = sqlcmd.Parameters("@plantype").Value
            Dim inservice = sqlcmd.Parameters("@valid").Value
            Dim dt As DateTime = sqlcmd.Parameters("@expdate").Value
          
            Dim expdate = GetEpochTime(dt)
            Dim cashbal = sqlcmd.Parameters("@cashbal").Value
            Dim minavail = sqlcmd.Parameters("@minavail").Value
            dt = sqlcmd.Parameters("@asof").Value
            Dim asof = GetEpochTime(dt)
            Dim stacked = sqlcmd.Parameters("@stacked").Value
            Dim cashstacked = sqlcmd.Parameters("@cashstacked").Value
            Dim renew = sqlcmd.Parameters("@renew").Value
            Dim carrier = sqlcmd.Parameters("@carrier").Value
                    

            sqlcon.Close()
            
            xmlres = XML_Result(XML("planid", planid) + XML("planname", planname) + XML("plantype", plantype) + XML("inservice", inservice) +
                                   XML("expdate", expdate) + XML("cashbal", cashbal) + XML("minavail", minavail) + XML("asof", asof) + XML("stacked", stacked) + XML("renew", renew) + XML("carrier", carrier)+ XML("cashstacked", cashstacked))
           
        Catch ex As Exception
            xmlres = XML_Result(XML("status", "ERROR") + XML("msg", ex.Message))
        End Try
        
        LOG.WriteLog("Get Balance: " + context.Request.Url.ToString(), xmlres)
        context.Response.Write(xmlres)
        LOG.WriteLog("--End Get Balance--")
    End Sub
    
    Public Function GetEpochTime(tm As DateTime) As String
        LOG.WriteLog("tm=" + tm.ToString())
        Static Dim dtEpochStartTime As DateTime = Convert.ToDateTime("1/1/1970 0:00:00 AM")
        tm = tm.ToUniversalTime()
        LOG.WriteLog("univ tm=" + tm.ToString())
        Dim ts As TimeSpan = tm.Subtract(dtEpochStartTime)
        Dim epochtime As Double = ((((((ts.Days * 24) + ts.Hours) * 60) + ts.Minutes) * 60) + ts.Seconds)
        LOG.WriteLog("epoch tm=", Convert.ToInt64(epochtime).ToString)
        Return Convert.ToInt64(epochtime).ToString
        
    End Function
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class


