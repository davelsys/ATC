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
        Dim mid = context.Request.QueryString("ivrid")
        Dim planid = context.Request.QueryString("planid")
        
        Try
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "GetCCFields"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon

            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@mid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@planid", planid).Direction = Data.ParameterDirection.Input
            
            sqlcmd.Parameters.Add("@orderid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@ccnumber", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@ccexp", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cccode", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@ccamt", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@monthplanid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashplanid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@monthcharge", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashcharge", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@intlcharge", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@agent", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            
            sqlcmd.ExecuteNonQuery()
            sqlcon.Close()
            
            Dim ccCharge As New CreditCardCharge()
            

            ccCharge.OrderId = sqlcmd.Parameters("@orderid").Value
            ccCharge.CellNumber = sqlcmd.Parameters("@mid").Value
            ccCharge.CCNumber = sqlcmd.Parameters("@ccnumber").Value
            ccCharge.CCExpiration = sqlcmd.Parameters("@ccexp").Value
            ccCharge.CCCode = sqlcmd.Parameters("@cccode").Value
            ccCharge.MonthlyPlanId = sqlcmd.Parameters("@monthplanid").Value
            ccCharge.CashPlanId = sqlcmd.Parameters("@cashplanid").Value

            ccCharge.MonthlyAmnt = sqlcmd.Parameters("monthcharge").Value
            ccCharge.CashAmnt = sqlcmd.Parameters("@cashcarge").Value
            ccCharge.IntlAmnt = sqlcmd.Parameters("@intlcharge").Value
            ccCharge.ItemAmnt = "0"

            ccCharge.MiscellaneousName = ""
            ccCharge.MiscellaneousCost = "0"
       
            ccCharge.User = "IVR"
            ccCharge.Agent = sqlcmd.Parameters("@agent").Value
            ccCharge.RunCharge()
    

            Dim xmlres As String = IVR.XML_Result(IVR.XML("MID", mid))

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