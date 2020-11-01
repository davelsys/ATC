Imports Microsoft.VisualBasic

Public Class DataTableToExcel

    Private _dt As DataTable
    Private _fileName As String = "Report"

    Public Property DataTable() As DataTable
        Get
            Return _dt
        End Get
        Set(value As DataTable)
            _dt = value
        End Set
    End Property

    Public Property FileName() As String
        Get
            Return _fileName
        End Get
        Set(value As String)
            _fileName = value
        End Set
    End Property

    Public Sub Export()

        Dim sw As System.IO.StringWriter = New System.IO.StringWriter
        sw.Write(GenerateHeaders & GenerateRows)

        HttpContext.Current.Response.Clear()
        HttpContext.Current.Response.AddHeader("content-disposition", "attachment; filename=" & _fileName & ".xls")
        HttpContext.Current.Response.ContentType = "application/ms-excel"
        HttpContext.Current.Response.Charset = ""
        HttpContext.Current.Response.Write(sw.ToString())
        HttpContext.Current.Response.End()
    
    End Sub

    Private Function GenerateHeaders() As String

        Dim str As StringBuilder = New StringBuilder
        For Each column As DataColumn In _dt.Columns
            str.Append(column.ToString & ControlChars.Tab)
        Next
        str.Append(ControlChars.NewLine)

        Return str.ToString

    End Function

    Private Function GenerateRows() As String

        Dim str As StringBuilder = New StringBuilder
    
        For Each row As DataRow In _dt.Rows
            For Each column As DataColumn In _dt.Columns
                str.Append(row(column) & ControlChars.Tab)
            Next
            str.Append(ControlChars.NewLine)
        Next

        Return str.ToString

    End Function
  
End Class
