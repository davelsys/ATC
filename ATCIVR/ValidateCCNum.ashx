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
        LOG.WriteLog("------------------Validate CC Num")
        
    Dim ivrid = context.Request.QueryString("ivrid")
        Dim newcc = context.Request.QueryString("newcc")
        Dim planid = context.Request.QueryString("planid")
        Dim xmlres As String = ""
        
        Try
            
            ' The Luhn Formula:
            'Drop the last digit from the number. The last digit is what we want to check against
            'Reverse the numbers
            'Multiply the digits in odd positions (1, 3, 5, etc.) by 2 and subtract 9 to all any result higher than 9
            'Add all the numbers together
            'The check digit (the last number of the card) is the amount that you would need to add to get a multiple of 10 (Modulo 10)
            '        Luhn Example
            'Step																	Total
            'Original Number:	4	5	5	6	7	3	7	5	8	6	8	9	9	8	5	5	
            'Drop the last digit:	4	5	5	6	7	3	7	5	8	6	8	9	9	8	5		
            'Reverse the digits:	5	8	9	9	8	6	8	5	7	3	7	6	5	5	4		
            'Multiple odd digits by 2:	10	8	18	9	16	6	16	5	14	3	14	6	10	5	8		
            'Subtract 9 to numbers over 9:	1	8	9	9	7	6	7	5	5	3	5	6	1	5	8		
            'Add all numbers:	1	8	9	9	7	6	7	5	5	3	5	6	1	5	8		85
            'Mod 10:	85 modulo 10 = 5 (last digit of card) 
 
            Dim ValidCCNum As Integer = 0
            If Len(newcc) > 15 Then
                ValidCCNum = 1
            End If
            
            Dim sqlcon As SqlConnection = ATCDB.OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.Connection = sqlcon
            sqlcmd.CommandText = "UPDATE IVR_Calls SET curplan = @planid"
            sqlcmd.Parameters.AddWithValue("@planid", planid)
            sqlcmd.ExecuteNonQuery()            
        
            xmlres = XML_Result(XML("nccvalid", ValidCCNum.ToString))
           
        Catch ex As Exception
            xmlres = XML("msg", ex.Message)
        End Try
        
        
        context.Response.Write(xmlres)
        LOG.WriteLog("-- End Validate CC Num--")
    End Sub
    
   
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
    
End Class