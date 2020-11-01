<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient


Public Class Handler : Implements IHttpHandler
    
   
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        context.Response.Write("Hello World")
        
        Dim xmlres As String = ""
        Dim ccCharge As New CreditCardCharge()

        LOG.WriteLog("Begin")
        LOG.WriteLog("Hello world")
    
        
        Try
            Dim ivrid = context.Request.QueryString("ivrid")
            Dim mid = context.Request.QueryString("mid")
            Dim newcc = context.Request.QueryString("newcc")
            Dim newexp = context.Request.QueryString("newexp")
            Dim newcode = context.Request.QueryString("newcccode")
            Dim newplan = context.Request.QueryString("planid")
    
        
            LOG.WriteLog("ivrid=" + ivrid + " mid=" + mid + " newcc=" + newcc + " newexp=" + newexp + " newcccode=" + newcode + " newplanid=" + newplan)
        
            LOG.WriteLog("Here")
        
     
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "IVR_GetChargeInfo"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon

            sqlcmd.Parameters.AddWithValue("@ivrid", ivrid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@mid", mid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@newcc", newcc.Substring(newcc.Length() - 4)).Direction = Data.ParameterDirection.Input
            'sqlcmd.Parameters.AddWithValue("@newcc", newcc).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@newexp", newexp).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.AddWithValue("@newcode", newcode).Direction = Data.ParameterDirection.Input
                    
            sqlcmd.Parameters.Add("@orderid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@monthplanid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@monthamt", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashplanid", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@cashamt", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@agent", SqlDbType.Varchar, 20).Direction = Data.ParameterDirection.Output    'SG 11/30/2015
            
            'sqlcmd.Parameters.Add("@orderid", SqlDbType.Int).Direction = Data.ParameterDirection.Output
            'sqlcmd.Parameters.Add("@monthplanid", SqlDbType.Int).Direction = Data.ParameterDirection.Output
            'sqlcmd.Parameters.Add("@monthamt", SqlDbType.Money).Direction = Data.ParameterDirection.Output
            'sqlcmd.Parameters.Add("@cashplanid", SqlDbType.Int, 50).Direction = Data.ParameterDirection.Output
            'sqlcmd.Parameters.Add("@cashamt", SqlDbType.Money, 50).Direction = Data.ParameterDirection.Output
            
            
            sqlcmd.ExecuteNonQuery()

            
            ccCharge.CellNumber = mid
            ccCharge.OrderId = sqlcmd.Parameters("@orderid").Value
            ccCharge.CCNumber = newcc
            ccCharge.CCExpiration = newexp
            ccCharge.CCCode = newcode
            ccCharge.User = "IVR"
            'ccCharge.Agent = "IVR"
            
            
            LOG.WriteLog(sqlcmd.Parameters("@monthplanid").Value + "#" + sqlcmd.Parameters("@monthamt").Value + "#" + sqlcmd.Parameters("@cashplanid").Value + "#" + sqlcmd.Parameters("@cashamt").Value)
            
            If Not IsDBNull(sqlcmd.Parameters("@monthplanid").Value) Then
                     If sqlcmd.Parameters("@monthplanid").Value <> newplan Then
                        ccCharge.MonthlyPlanId = newplan
                Else
                    ccCharge.MonthlyPlanId = sqlcmd.Parameters("@monthplanid").Value
                End If
            End If
            If Not IsDBNull(sqlcmd.Parameters("@monthamt").Value) Then
                ccCharge.MonthlyAmnt = Convert.ToDecimal(sqlcmd.Parameters("@monthamt").Value)
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
           
            ccCharge.IntlAmnt = 0
            ccCharge.ItemAmnt = 0

            ccCharge.MiscellaneousName = ""
            ccCharge.MiscellaneousCost = 0

            ccCharge.User = "IVR"
            'ccCharge.Agent = ""
           sqlcon.Close()
            
            ' LOG.WriteLog(sqlcmd.Parameters("@monthplanid").Value + "#" + sqlcmd.Parameters("@monthamt").Value + "#" + sqlcmd.Parameters("@cashplanid").Value + "#" + sqlcmd.Parameters("@cashamt").Value)
            ''xmlres = XML_Result(XML("montplanid", ccCharge.MonthlyPlanId) + XML("cashplanid", ccCharge.CashPlanId))
            LOG.WriteLog(ccCharge.CellNumber + "#" + ccCharge.OrderId.ToString() + "#" + ccCharge.CCNumber + "#" + ccCharge.CCExpiration + "#" + ccCharge.CCCode + "#" + ccCharge.Agent)
        
            LOG.WriteLog(ccCharge.MonthlyPlanId.ToString() + ":" + ccCharge.MonthlyAmnt.ToString() + ":" + ccCharge.CashPlanId.ToString() + ":" + ccCharge.CashAmnt.ToString())
            
            Try
                Dim sqlcon2 As New SqlConnection()
                sqlcon2.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;database=ppc"
                sqlcon2.Open()
                Dim sqlcmd2 As New SqlCommand()
                Dim Charged as Integer = 0
                ccCharge.RunCharge()
                If ccCharge.hasCharged  'if charge was succesful, save new card info and stack payment SG 11/30/2015
                    Charged = 1
                    
                    sqlcmd2.CommandText = "VerNewPayment"
                    sqlcmd2.CommandType = CommandType.StoredProcedure
                    sqlcmd2.Connection = sqlcon2
            
                    sqlcmd2.Parameters.Add("@cellnum ", SqlDbType.VarChar, 50).Value = ccCharge.CellNumber
                    sqlcmd2.Parameters.Add("@monthplanid ", SqlDbType.int).Value = ccCharge.MonthlyPlanId
                    sqlcmd2.Parameters.Add("@cashplanid ", SqlDbType.int).Value = ccCharge.CashPlanId
                    
                    sqlcmd2.ExecuteNonQuery()
                    
                    Dim sql As StringBuilder = New StringBuilder
                    sql.Append("UPDATE customers SET [cc_last_four] = @ccLastFour,")
                    sql.Append("[cc_expiration_date] = @ccExpDate ")
                    sql.Append("WHERE customer_id = @customer_id;")
                    
                    Dim cmd As SqlCommand = New SqlCommand(sql.ToString, sqlcon2)
                    
                    cmd.Parameters.Add("@customer_id", SqlDbType.Int).Value = ccCharge.OrderId
                    
                   If ccCharge.CCNumber.Length > 4 Then
                      Dim ccStr As String = ccCharge.CCNumber.Trim()
                        cmd.Parameters.Add("@ccLastFour", SqlDbType.VarChar).Value = ccStr.Substring(ccStr.Length - 4)
                   Else
                        cmd.Parameters.Add("@ccLastFour", SqlDbType.VarChar).Value = ""
                   End If

                   cmd.Parameters.Add("@ccExpDate", SqlDbType.VarChar).Value = ccCharge.CCExpiration
                   cmd.ExecuteNonQuery()
        
                    sqlcon2.Close()
                End If
                
                
                'xmlres = XML_Result(XML("chargestatus", ccCharge.hasCharged.ToString()))
                xmlres = XML_Result(XML("chargestatus", Charged.ToString()))
                ' xmlres = XML_Result(XML("chargestatus", "Authmsg = " + ccCharge.AuthMessage))
                
                Try
                    sqlcon.Open()
                    Dim sqlcmd3 As New SqlCommand()
                    sqlcmd3.CommandText = "UpdateIVR"
                    sqlcmd3.CommandType = CommandType.StoredProcedure
                
                    sqlcmd3.Parameters.AddWithValue("@id", ivrid).Direction = Data.ParameterDirection.Input
                    sqlcmd3.Parameters.AddWithValue("@activity", "Renew Plan").Direction = Data.ParameterDirection.Input
                    sqlcmd3.Parameters.AddWithValue("@result", IIf(Charged = 1, "Successful", "Failed")).Direction = Data.ParameterDirection.Input
                Catch ex3 As Exception
                    xmlres = XML_Result(XML("chargestatus", "Update ERROR") + XML("msg", ex3.Message))
                End Try

            Catch ex2 As Exception
                xmlres = XML_Result(XML("chargestatus", "RunCharge ERROR") + XML("msg", ex2.Message))
             
            End Try
        
        Catch ex As Exception
            xmlres = XML_Result(XML("chargestatus", "ERROR") + XML("msg", ex.Message))
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