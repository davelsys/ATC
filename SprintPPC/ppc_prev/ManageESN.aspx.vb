Imports System.Globalization

Partial Class ManageESN
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If Not System.Web.HttpContext.Current.User.Identity.IsAuthenticated Then
            Response.Redirect(FormsAuthentication.LoginUrl)
        End If

        If Session.Item("UserLevel") Is Nothing Then
            Response.Redirect(FormsAuthentication.LoginUrl)
        End If

        If Session.Item("UserLevel") <> 1 Then
            Response.Redirect(FormsAuthentication.LoginUrl)
        End If

        ' Clear upload messages on page load
        uploadStatusLbl.Text = ""

        BindEsnGrid()

    End Sub

    Private Sub BindEsnGrid()

        Dim sql As StringBuilder = New StringBuilder
        sql.Append("SELECT * FROM [SerialESN]  ")

        ' See if there is any filtering
        Dim isFilteredAlready As Boolean = False

        If serialNumSearchFld.Text.Length > 0 Then
            sql.Append("WHERE [Serial#] LIKE '%' + @SerialNum + '%' ")
            isFilteredAlready = True
        End If

        If esnSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND [ESN] LIKE '%' + @ESN + '%' ")
            Else
                sql.Append("WHERE [ESN] LIKE '%' + @ESN + '%' ")
                isFilteredAlready = True
            End If
        End If

        If intlSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND cast( cast([International] as bigINT) as varchar(12)) LIKE '%' + @Intl + '%' ")
            Else
                sql.Append("WHERE cast( cast([International] as bigINT) as varchar(12)) LIKE '%' + @Intl + '%' ")
                isFilteredAlready = True
            End If
        End If

        If cusPinSearchFld.Text.Length > 0 Then
            If isFilteredAlready Then
                sql.Append("AND cast( cast([CustomerPin] as bigINT) as varchar(12)) LIKE '%' + @CustPin + '%' ")
            Else
                sql.Append("WHERE cast( cast([CustomerPin] as bigINT) as varchar(12)) LIKE '%' + @CustPin + '%' ")
                isFilteredAlready = True
            End If
        End If

        If dateCreatedSearchFld.Text.Length > 0 Then

            Dim date1 As Date

            If dateCreatedSearchFld.Text.Length >= 8 Then
                Try
                    date1 = dateCreatedSearchFld.Text
                    esnDataSource.SelectParameters.Item("DateCreated").DefaultValue =
                        date1.ToString("d", DateTimeFormatInfo.InvariantInfo)
                Catch ex As InvalidCastException
                    esnDataSource.SelectParameters.Item("DateCreated").DefaultValue = dateCreatedSearchFld.Text
                End Try
            Else
                esnDataSource.SelectParameters.Item("DateCreated").DefaultValue = dateCreatedSearchFld.Text
            End If

            If isFilteredAlready Then
                sql.Append("AND CONVERT(VARCHAR(20), [InsertedDate], 101) LIKE '%' + @DateCreated + '%' ")
            Else
                sql.Append("WHERE CONVERT(VARCHAR(20), [InsertedDate], 101) LIKE '%' + @DateCreated + '%' ")
                isFilteredAlready = True
            End If

        End If

        sql.Append("ORDER BY [InsertedDate] DESC ")

        esnDataSource.ConnectionString = ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString
        esnDataSource.SelectCommandType = SqlDataSourceCommandType.Text

        esnDataSource.SelectCommand = sql.ToString()
        esnDataSource.CancelSelectOnNullParameter = False

        esnGridView.DataBind()

    End Sub

    Protected Sub uploadEsnFile_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        Dim con As SqlConnection = Nothing
        Dim cmd As SqlCommand = Nothing

        If esnUploadControl.HasFile Then

            'Dim excelGenericContentType As String = "application/vnd.ms-excel"
            'Dim excel2010ContentType As String = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"

            'If esnUploadControl.PostedFile.ContentType = excelGenericContentType _
            '            And esnUploadControl.PostedFile.ContentType = excel2010ContentType Then
            Dim fileName As String = LCase(esnUploadControl.PostedFile.FileName)
            If Right(fileName, 4) = (".xls") Or Right(fileName, 5) = ".xlsx" Then

                Dim path As String = Server.MapPath("~/uploads/") & "UploadedESN.xlsx"

                Try
                    esnUploadControl.SaveAs(path)

                    con = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
                    con.Open()

                    'take data from file to DB table
                    cmd = New SqlCommand("ImportSerialEsn", con)
                    cmd.CommandType = CommandType.StoredProcedure

                    cmd.ExecuteNonQuery()

                    uploadStatusLbl.Text = "ESN imported successfully."

                Catch ex As Exception
                    uploadStatusLbl.Text = ex.Message + ":::::Upload failed::::. Please check the data in the spreadsheet."
                Finally
                    con.Close()
                End Try

            Else    ' The file isn't an excel sheet
                uploadStatusLbl.Text = "The uploaded file wasn't an excel spreadsheet."
            End If

        Else    ' No file was uploaded.
            uploadStatusLbl.Text = "No file was uploaded."
        End If
    End Sub

End Class
