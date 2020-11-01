<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient


Public Class Handler : Implements IHttpHandler
    
   
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
        
        Dim xmlres As String = ""
        Dim ccCharge As New CreditCardCharge()
        
        LOG.WriteLog(context.Request.Url.ToString()) 
        LOG.WriteLog("----------Charge Card On File")
        Try
            Dim ivrid = context.Request.QueryString("ivrid")
            Dim mid = context.Request.QueryString("mid")
            Dim newcc = ""
            Dim newexp = ""
            Dim newcode = ""
            'Dim newplan = context.Request.QueryString("planid")
            
            
    LOG.WriteLog("Charge Card On File: ivrid=" + ivrid + " mid= " + mid)
        
            'LOG.WriteLog("Here")
     
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_GetChargeInfo"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon

            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@mid", mid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@newcc", newcc).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@newexp", newexp).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@newcode", newcode).Direction = Data.ParameterDirection.Input
            
            sqlcmd.Parameters.Add("@orderid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            'sqlcmd.Parameters.AddWithValue("@monthplanid", newplan).Direction = Data.ParameterDirection.Input
            'sqlcmd.Parameters.Add("@monthplanid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@monthamt", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@monthplanid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashplanid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashamt", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@agent", SqlDbType.Varchar, 20).Direction = Data.ParameterDirection.Output    'SG 11/30/2015
            
            sqlcmd.ExecuteNonQuery()

                   
            ccCharge.CellNumber = mid
            ccCharge.OrderId = sqlcmd.Parameters("@orderid").Value
            ccCharge.CCNumber = "" 'sqlcmd.Parameters("@ccnum").Value
            'ccCharge.CCNumber = sqlcmd.Parameters("@ccnum").Value
            ccCharge.CCExpiration = "" ' sqlcmd.Parameters("@ccexp").Value
            ccCharge.CCCode = "" 'sqlcmd.Parameters("@cccode").Value
            
            LOG.WriteLog("Charge Card On File: " + sqlcmd.Parameters("@orderid").Value + "#" + sqlcmd.Parameters("@monthplanid").Value + "#" + sqlcmd.Parameters("@monthamt").Value + "#" + sqlcmd.Parameters("@cashplanid").Value + "#" + sqlcmd.Parameters("@cashamt").Value)
            
            
            If Not IsDBNull(sqlcmd.Parameters("@monthplanid").Value) Then
                    ccCharge.MonthlyPlanId = sqlcmd.Parameters("@monthplanid").Value
            End If
            If Not IsDBNull(sqlcmd.Parameters("@monthamt").Value) Then
                ccCharge.MonthlyAmnt = Convert.ToDecimal(sqlcmd.Parameters("@monthamt").Value)
                'ccCharge.MonthlyAmnt = Convert.ToDecimal(1.0)
            Else
                ccCharge.MonthlyAmnt = 0
            End If
            If Not IsDBNull(sqlcmd.Parameters("@cashplanid").Value) Then
                ccCharge.CashPlanId = sqlcmd.Parameters("@cashplanid").Value
            End If
            If Not IsDBNull(sqlcmd.Parameters("@agent").Value) Then
                ccCharge.Agent = sqlcmd.Parameters("@agent").Value
            End If
            If Not IsDBNull(sqlcmd.Parameters("@cashamt").Value) Then
                ccCharge.CashAmnt = Convert.ToDecimal(sqlcmd.Parameters("@cashamt").Value)
            Else
                ccCharge.CashAmnt = 0
            End If
       
            'If IsDBNull(sqlcmd.Parameters("@monthplanid").Value) Then
            'ccCharge.MonthlyPlanId = 0
          ' End If
            
            ''ccCharge.MonthlyPlanId = sqlcmd.Parameters("@monthplanid").Value
            ''ccCharge.MonthlyAmnt = sqlcmd.Parameters("@monthamt").Value
            ''ccCharge.CashPlanId = sqlcmd.Parameters("@cashplanid").Value
            ''ccCharge.CashAmnt = sqlcmd.Parameters("@cashamt").Value
            ccCharge.IntlAmnt = 0
            ccCharge.ItemAmnt = 0

            ccCharge.MiscellaneousName = ""
            ccCharge.MiscellaneousCost = 0

            ccCharge.User = "IVR" '+ ivrid.ToString()
            'ccCharge.Agent = ""

            sqlcon.Close()
            
            LOG.WriteLog("Charge Card On File: " + ccCharge.CellNumber + "#" + ccCharge.OrderId.ToString() + "#" + ccCharge.CCNumber + "#" + ccCharge.CCExpiration + "#" + ccCharge.CCCode + "#" + ccCharge.Agent)
        
            LOG.WriteLog("Charge Card On File: " + ccCharge.MonthlyPlanId.ToString() + ":" + ccCharge.MonthlyAmnt.ToString() + ":" + ccCharge.CashPlanId.ToString() + ":" + ccCharge.CashAmnt.ToString())
                  
            ''xmlres = XML_Result(XML("montplanid", ccCharge.MonthlyPlanId) + XML("cashplanid", ccCharge.CashPlanId))
            
            
            Dim sqlcon2 As New SqlConnection()
            sqlcon2.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;database=ppc"
            
            Dim sqlcmd2 As New SqlCommand()
            sqlcmd2.Connection = sqlcon2
            Dim Charged As Integer = 0
            sqlcon2.Open()
            Try
                ccCharge.RunCharge()
                
                If ccCharge.hasCharged Then
                
                    Charged = 1
                    
                    Try

                   
                        sqlcmd2.CommandText = "VerNewPayment"
                        sqlcmd2.CommandType = CommandType.StoredProcedure
                        sqlcmd2.Parameters.Add("@cellnum ", SqlDbType.VarChar, 50).Value = ccCharge.CellNumber
                        sqlcmd2.Parameters.Add("@monthplanid ", SqlDbType.Int).Value = ccCharge.MonthlyPlanId
                        sqlcmd2.Parameters.Add("@cashplanid ", SqlDbType.Int).Value = ccCharge.CashPlanId
                    
                        sqlcmd2.ExecuteNonQuery()
                        
                        sqlcmd2.Parameters.Clear()
                        sqlcmd2 = New SqlCommand()
                        sqlcmd2.Connection = sqlcon2
                        sqlcmd2.CommandText = "InsertTransCommission"
                        sqlcmd2.CommandType = CommandType.StoredProcedure
                        sqlcmd2.Parameters.Add("@TransId", SqlDbType.Int).Value = ccCharge.TransactionId
                        
                        sqlcmd2.ExecuteNonQuery()
                        
                        sqlcon2.Close()
                  
                        sqlcmd2 = New SqlCommand()
                        sqlcmd2.CommandText = "IVR_EarlyRenewal"
                        sqlcmd2.CommandType = CommandType.StoredProcedure
                        sqlcmd2.Connection = ATCDB.OpenDB()
                        'sqlcmd2.Connection.Open()
            
                        sqlcmd2.Parameters.Add("@cellnum ", SqlDbType.VarChar, 50).Value = ccCharge.CellNumber
                        sqlcmd2.ExecuteNonQuery()
                        
                        
                        sqlcmd2.Connection.Close()
                        
                    Catch ex3 As Exception
                        LOG.WriteLog("Charge Card On File: ChargeStatus - CleanUp Error: " & ex3.Message)
                    End Try
                    
                End If
                
                
                Dim newcmd As New SqlCommand()
                newcmd.Connection = sqlcon
                newcmd.CommandText = "UPDATE IVR_Calls SET renewresult = case when @charged = 1 then 'Successful' else 'Error' end where ivrid=@ivrid"
                newcmd.Parameters.AddWithValue("@ivrid", ivrid)
                newcmd.Parameters.AddWithValue("@charged", Charged)
                sqlcon.Open()
                newcmd.ExecuteNonQuery()
                
                'xmlres = XML_Result(XML("chargestatus", ccCharge.hasCharged.ToString))
                xmlres = XML_Result(XML("chargestatus", Charged.ToString()))
                ' xmlres = XML_Result(XML("chargestatus", "Authmsg = " + ccCharge.AuthMessage))
            Catch ex As Exception
                xmlres = XML_Result(XML("chargestatus", "RunCharge ERROR") + XML("msg", ex.Message))
             
            End Try
        Catch ex As Exception
            xmlres = XML_Result(XML("chargestatus", "ERROR") + XML("msg", ex.Message))
        End Try
        
        LOG.WriteLog(xmlres)
        context.Response.Write(xmlres)
        Log.WriteLog("---End Charge Card On File--")
    End Sub
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class