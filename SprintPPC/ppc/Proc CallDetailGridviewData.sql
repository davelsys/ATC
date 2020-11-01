
/****** Object:  StoredProcedure [dbo].[CallDetailGridviewData]    Script Date: 07/25/2012 11:32:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



ALTER PROCEDURE [dbo].[CallDetailGridviewData]
@phone_number varchar(15),
@from_index int,
@end_index int
AS
DECLARE
@from_date datetime,
@end_date datetime
BEGIN
	
	EXECUTE CallDetailDates
		 @PhoneNumber = @phone_number,
		 @FromDate = @from_date OUTPUT,
		 @EndDate = @end_date OUTPUT,
		 @FromIndex = @from_index,
		 @EndIndex = @end_index
	
	
	SELECT cdr.[PhoneNumber] ,cdr.[Duration] ,cdr.[CallDate] ,cdr.[PreBalance] ,cdr.[PostBalance],
        cdr.[Description], cdr.[DateRetrieved]
        FROM [CDR] join mdn on cdr.phoneaccountid = mdn.phoneaccountid
        WHERE mdn.[PhoneNumber] = @phone_number
        AND CallDate BETWEEN @from_date AND dateadd(second, -1, @end_date) 
        order by cdr.calldate desc
        
END




GO


