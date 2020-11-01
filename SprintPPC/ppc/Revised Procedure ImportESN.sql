
/****** Object:  StoredProcedure [dbo].[ImportSerialEsn]    Script Date: 07/04/2012 13:53:11 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




create proc [dbo].[ImportSerialEsn]
as

INSERT INTO SerialESN ([Serial#]
      ,[ESN]
      ,[International]
      ,[CustomerPin]
      ,[InsertedDate])
      
SELECT CONVERT(bigINT, CONVERT(FLOAT, [Serial#]))AS Serial#
      ,[ESN]
      ,[International]
      ,[CustomerPin]
      ,GETDATE()
FROM OpenRowSet('Microsoft.ace.OLEDB.12.0', 'Excel 12.0; Database=C:\ppc\uploads\UploadedESN.xlsx; HDR=YES;',
	'SELECT * from [Sheet1$]')

WHERE [Serial#] not in (select Serial# from SerialESN)


GO