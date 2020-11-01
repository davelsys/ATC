<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Data
Imports System.Data.SqlClient

Public Class Handler : Implements IHttpHandler
    
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        '   context.Response.Write("Hello World")
        
        Dim cid = context.Request.QueryString("cid")
        
        Try
            Dim sqlcon As SqlConnection = OpenDB()
            Dim sqlcmd As New SqlCommand()
            sqlcmd.CommandText = "QueryRemaining"
            sqlcmd.CommandType = CommandType.StoredProcedure
            sqlcmd.Connection = sqlcon

            sqlcmd.Parameters.AddWithValue("@mid", cid).Direction = Data.ParameterDirection.Input
            sqlcmd.Parameters.Add("@type", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.Parameters.Add("@avail", SqlDbType.VarChar, 50).Direction = Data.ParameterDirection.Output
            sqlcmd.ExecuteNonQuery()

            Dim type = sqlcmd.Parameters("@type").Value
            Dim avail = sqlcmd.Parameters("@avail").Value

            sqlcon.Close()

            Dim begxml As String = "<response><result><ivr_info><variables>"
            Dim endxml As String = "  </variables></ivr_info></result></response>"
            Dim xmlres As String = begxml + XMLOutput("CID", cid) + XMLOutput("Type", type) + XMLOutput("Avail", avail) + endxml

            context.Response.Write(xmlres)

        Catch ex As Exception
            context.Response.Write("Error :" + ex.Message)
        End Try
    End Sub
    
    Function XMLOutput(ByVal Arg As String, ByVal Val As String) As String
        Return String.Format("<variable><name>{0}</name><value>{1}</value></variable> ", Arg, Val)
    End Function
    
    Function OpenDB() As SqlConnection
        Dim sqlcon As New SqlConnection()
        sqlcon.ConnectionString = "Data Source=(local);User ID=sa3;Password=davel;MultipleActiveResultSets=True;Initial Catalog=ppc"
        sqlcon.Open()
        Return sqlcon
    End Function
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class