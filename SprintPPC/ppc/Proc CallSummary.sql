
/****** Object:  StoredProcedure [dbo].[CallSummary]    Script Date: 07/25/2012 11:33:51 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[CallSummary]
@pn varchar(50),
@from_index int = 1,
@end_index int = 0
AS
DECLARE
@from_date datetime,
@end_date datetime

BEGIN

			EXECUTE CallDetailDates
				 @PhoneNumber = @pn,
				 @FromDate = @from_date OUTPUT,
				 @EndDate = @end_date OUTPUT,
				 @FromIndex = @from_index,
				 @EndIndex = @end_index
	
SELECT
	 
	(
		SELECT SUM (duration)  FROM  dbo.CDR join mdn 
			ON cdr.phoneaccountid = mdn.phoneaccountid and cdr.AccountName = mdn.Account 
			WHERE description = 'Successful O/G calls'  AND dbo.mdn.PhoneNumber = @pn
			AND CallDate BETWEEN @from_date AND @end_date
					
	)  AS [Outgoing minutes],
			
	(
		SELECT SUM (duration)  FROM  dbo.CDR join mdn 
			ON cdr.phoneaccountid = mdn.phoneaccountid and cdr.AccountName = mdn.Account 
			WHERE description = 'Successful I/C calls'  AND dbo.mdn.PhoneNumber = @pn
			AND CallDate BETWEEN @from_date AND @end_date
			
	) AS [Incoming minutes],
			
	(
		SELECT SUM (duration)  FROM  dbo.CDR join mdn 
			ON cdr.phoneaccountid = mdn.phoneaccountid and cdr.AccountName = mdn.Account 
			WHERE description in ('Successful O/G calls','Successful I/C calls') AND dbo.mdn.PhoneNumber = @pn
			AND CallDate BETWEEN @from_date AND @end_date
			
	) AS [Total Minutes],
	
	@from_date AS from_date, @end_date AS end_date

END



GO


