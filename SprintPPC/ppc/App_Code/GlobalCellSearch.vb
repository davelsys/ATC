Imports Microsoft.VisualBasic

Public Class GlobalCellSearch

    Private _isPostBack As Boolean
    Private _sessionCell As String = Nothing
    Private _textBox As TextBox

    Public Sub New(ByVal isPostBack As Boolean, ByVal sessionCell As String, ByVal textBox As TextBox)

        ' Init properties
        Me._isPostBack = isPostBack
        Me._sessionCell = sessionCell
        Me._textBox = textBox

        CellTextBox()

    End Sub

    Public ReadOnly Property TextBox() As TextBox
        Get
            Return Me._textBox
        End Get
    End Property

    Public ReadOnly Property IsGSOn() As Boolean
        Get
            Return If(Me._textBox.Text.Length >= 10, True, False)
        End Get
    End Property

    Public Sub CellTextBox()
        If Not Me._isPostBack Then
            Me._textBox.Text = Me._sessionCell
        End If
    End Sub

End Class
