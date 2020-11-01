
/****** Object:  StoredProcedure [dbo].[CallDetailDates]    Script Date: 07/25/2012 11:31:29 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[CallDetailDates]
@PhoneNumber varchar(10),
@FromDate datetime output,
@EndDate datetime output,
@FromIndex int = 1,
@EndIndex int = 0
AS
DECLARE
	@MDNRatePlan varchar(50),
	@PlanType varchar(15)
BEGIN

	SET @MDNRatePlan = (SELECT [RatePlan] FROM MDN WHERE MDN.PhoneNumber = @PhoneNumber)

	SET @PlanType = (
						SELECT DISTINCT [plantype] FROM [Plans] WHERE [ppcplan] = @MDNRatePlan
					)
	
	IF @PlanType = 'Monthly'
		begin
			SELECT ROW_NUMBER() OVER ( ORDER BY CallDate DESC ) AS RowNumber,  CallDate into #monthly_temp
			FROM CDR JOIN MDN 
			ON CDR.PhoneAccountId = MDN.phoneAccountId AND  CDR.AccountName = MDN.Account
			WHERE MDN.PhoneNumber = @PhoneNumber  AND
			CDR.[Description] = 'Bundle Renewal Successfully'
			GROUP BY CallDate;
			
			SET @FromDate = (SELECT CallDate FROM #monthly_temp WHERE RowNumber = @FromIndex)
			SET @EndDate  = (SELECT CallDate FROM #monthly_temp WHERE RowNumber = @EndIndex)
			
			DROP TABLE #monthly_temp;
			
		end
	ELSE
		begin
			SELECT ROW_NUMBER() OVER ( ORDER BY CallDate DESC ) AS RowNumber,  CallDate into #cash_temp
			FROM CDR JOIN MDN 
			ON CDR.PhoneAccountId = MDN.phoneAccountId AND  CDR.AccountName = MDN.Account
			WHERE MDN.PhoneNumber = @PhoneNumber  AND
			CDR.[Description] = 'Credit/Replenishment'
			GROUP BY CallDate;
			
			SET @FromDate = (SELECT CallDate FROM #cash_temp WHERE RowNumber = @FromIndex)
			SET @EndDate  = (SELECT CallDate FROM #cash_temp WHERE RowNumber = @EndIndex)
			
			DROP TABLE #cash_temp;
		end
		
	IF @FromDate IS NULL
		begin
			SET @FromDate = '1-1-1900'
		end
	IF @EndDate IS NULL
		begin
			SET @EndDate = GETDATE()
		end
		
END
GO


