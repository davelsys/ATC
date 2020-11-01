USE [ppc]
GO
/****** Object:  StoredProcedure [dbo].[CallDetailDates]    Script Date: 11/28/2012 15:29:04 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--------------- Create PPCData Database and CDR table and also -----------------
--------------- add the CDR indexes.                           -----------------
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------


CREATE DATABASE [PPCData];

GO

SELECT * INTO [PPCData].[dbo].[CDR] FROM [ppc].[dbo].[CDR];


GO


/****** Object:  Index [IX_CallDate]    Script Date: 11/28/2012 15:43:50 ******/
CREATE NONCLUSTERED INDEX [IX_CallDate] ON [PPCData].[dbo].[CDR] 
(
	[CallDate] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO



/****** Object:  Index [IX_Description]    Script Date: 11/28/2012 15:44:09 ******/
CREATE NONCLUSTERED INDEX [IX_Description] ON [PPCData].[dbo].[CDR] 
(
	[PhoneAccountId] ASC,
	[CallDate] ASC,
	[Description] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

ALTER INDEX [IX_Description] ON [PPCData].[dbo].[CDR] DISABLE
GO



/****** Object:  Index [IX_PhoneAccountId]    Script Date: 11/28/2012 15:44:27 ******/
CREATE NONCLUSTERED INDEX [IX_PhoneAccountId] ON [PPCData].[dbo].[CDR] 
(
	[PhoneAccountId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO



--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
------------------------- Alter ppc procs to reference the CDR in the ----------------
------------------------- PPCData database.                           ----------------
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------



ALTER PROCEDURE [dbo].[CallDetailDates]
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

	SET @MDNRatePlan = (SELECT [RatePlan] FROM [MDN] mdn WHERE MDN.PhoneNumber = @PhoneNumber)

	SET @PlanType = (
						SELECT DISTINCT [plantype] FROM [Plans] WHERE [ppcplan] = @MDNRatePlan
					)
	
	IF @PlanType = 'Monthly'
		begin
			SELECT ROW_NUMBER() OVER ( ORDER BY CallDate DESC ) AS RowNumber, CallDate into #monthly_temp
			FROM [PPCData].[dbo].[CDR] c JOIN [MDN] mdn 
			ON c.PhoneAccountId = MDN.phoneAccountId AND c.AccountName = MDN.Account
			WHERE MDN.PhoneNumber = @PhoneNumber  AND
			c.[Description] IN ('Bundle Renewal Successfully', 'Bundle Subscription Successful')
			GROUP BY CallDate;
			
			SET @FromDate = (SELECT CallDate FROM #monthly_temp WHERE RowNumber = @FromIndex)
			SET @EndDate  = (SELECT CallDate FROM #monthly_temp WHERE RowNumber = @EndIndex)
			
			DROP TABLE #monthly_temp;
			
		end
	ELSE
		begin
			SELECT ROW_NUMBER() OVER ( ORDER BY CallDate DESC ) AS RowNumber,  CallDate into #cash_temp
			FROM [PPCData].[dbo].[CDR] c JOIN [MDN] mdn 
			ON c.PhoneAccountId = MDN.phoneAccountId AND  c.AccountName = MDN.Account
			WHERE MDN.PhoneNumber = @PhoneNumber  AND
			c.[Description] = 'Credit/Replenishment'
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
	
	
	SELECT c.[PhoneNumber]
		,c.[Duration]
		,c.[CallDate]
		,c.[PreBalance]
		,c.[PostBalance]
        ,c.[Description]
        ,c.[DateRetrieved]
        FROM [PPCData].[dbo].[CDR] c
        join [MDN] mdn 
        on c.phoneaccountid = mdn.phoneaccountid
        WHERE mdn.[PhoneNumber] = @phone_number
        AND CallDate BETWEEN @from_date AND dateadd(second, -1, @end_date) 
        order by c.calldate desc
        
END


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
		SELECT SUM (duration) FROM [PPCData].[dbo].[CDR] c join [MDN] mdn 
			ON c.phoneaccountid = mdn.phoneaccountid and c.AccountName = mdn.Account 
			WHERE description = 'Successful O/G calls'  AND mdn.PhoneNumber = @pn
			AND CallDate BETWEEN @from_date AND @end_date
					
	)  AS [Outgoing minutes],
			
	(
		SELECT SUM (duration)  FROM [PPCData].[dbo].[CDR] c join [MDN] mdn 
			ON c.phoneaccountid = mdn.phoneaccountid and c.AccountName = mdn.Account 
			WHERE description = 'Successful I/C calls'  AND mdn.PhoneNumber = @pn
			AND CallDate BETWEEN @from_date AND @end_date
			
	) AS [Incoming minutes],
			
	(
		SELECT SUM (duration)  FROM [PPCData].[dbo].[CDR] c join [MDN] mdn 
			ON c.phoneaccountid = mdn.phoneaccountid and c.AccountName = mdn.Account 
			WHERE description in ('Successful O/G calls','Successful I/C calls') AND mdn.PhoneNumber = @pn
			AND CallDate BETWEEN @from_date AND @end_date
			
	) AS [Total Minutes],
	
	@from_date AS from_date, @end_date AS end_date
END



GO






ALTER PROCEDURE [dbo].[GetCallDetailIntervals]
	@PhoneNumber varchar(15) = ''
AS
DECLARE
	@MDNRatePlan varchar(50),
	@PlanType varchar(15)
BEGIN

	SET @MDNRatePlan = (SELECT [RatePlan] FROM [ppc].[dbo].[MDN] mdn WHERE MDN.PhoneNumber = @PhoneNumber)

	SET @PlanType = (
						SELECT DISTINCT [plantype] FROM [Plans] WHERE [ppcplan] = @MDNRatePlan
					)

	SELECT cd_intervals =
		CASE @PlanType
			WHEN 'Monthly'
				THEN
					(SELECT COUNT(DISTINCT CallDate) 
					FROM [PPCData].[dbo].[CDR] c JOIN [MDN] mdn 
					ON c.PhoneAccountId = MDN.phoneAccountId AND  c.AccountName = MDN.Account
					WHERE MDN.PhoneNumber = @PhoneNumber  AND
					c.[Description] IN ('Bundle Renewal Successfully', 'Bundle Subscription Successful'))
			ELSE 
				(SELECT COUNT(DISTINCT CallDate) 
				FROM [PPCData].[dbo].[CDR] c JOIN [MDN] mdn 
				ON c.PhoneAccountId = MDN.phoneAccountId AND  c.AccountName = MDN.Account
				WHERE MDN.PhoneNumber = @PhoneNumber  AND
				c.[Description] = 'Credit/Replenishment')
			END
	
END
			




GO






ALTER proc [dbo].[GetCellUsageByPeriod] as

declare @x int;

select top 1 @x = isnull(COUNT(*),0)
FROM [orders] INNER JOIN [MDN] mdn
ON [orders].[cell_num] = [MDN].[PhoneNumber]
INNER JOIN (SELECT [ppcplan], [plantype] FROM [Plans] GROUP BY [ppcplan], [plantype]) AS pt1
ON [MDN].[RatePlan] = pt1.[ppcplan]
INNER JOIN [PPCData].[dbo].[CDR] c
ON c.PhoneAccountId = MDN.phoneAccountId AND c.AccountName = MDN.Account
WHERE (pt1.[plantype] = 'monthly' AND c.[Description] IN 
		('Bundle Renewal Successfully', 'Bundle Subscription Successful'))
        OR (pt1.[plantype] = 'standard' AND c.[Description] = 'Credit/Replenishment')
group by  [orders].[cell_num]
order by 1 desc

declare @str varchar(max);
set @str = ''
    
set @str += 'SELECT DISTINCT [orders].[order_id]
     ,pt1.[plantype]
     ,[orders].[cell_num]
     ,[orders].[serial_num]
     ,c.[CallDate]
     ,c.[Duration]
	 ,pl.[planname]
     ,IDENTITY(INT, 1, 1) AS [id]
     ,ROW_NUMBER() OVER(PARTITION BY [orders].cell_num ORDER BY c.[CallDate] DESC) AS Period
	 INTO #plan_type_temp
	 FROM [orders] INNER JOIN [MDN] mdn
	 ON [orders].[cell_num] = [MDN].[PhoneNumber]
	 INNER JOIN (SELECT [ppcplan], [plantype] FROM [Plans] GROUP BY [ppcplan], [plantype]) AS pt1
	 ON [MDN].[RatePlan] = pt1.[ppcplan]
	 LEFT OUTER JOIN [Plans] pl
	 ON [orders].[plan_id] = pl.[planid]
	 INNER JOIN [PPCData].[dbo].[CDR] c
	 ON c.PhoneAccountId = MDN.phoneAccountId AND c.AccountName = MDN.Account
	 WHERE (pt1.[plantype] = ''monthly'' AND c.[Description] IN (''Bundle Renewal Successfully'', ''Bundle Subscription Successful''))
	 OR (pt1.[plantype] = ''standard'' AND c.[Description] = ''Credit/Replenishment'')
	 ORDER BY order_id, [CallDate];'
	 
	 
set @str += 'SELECT md.[PhoneNumber] AS [cell_num], MAX(cd.[CallDate]) AS [max_date]
INTO #max_calldate
FROM [MDN] md
join [PPCData].[dbo].[CDR] cd
on cd.PhoneAccountId = md.phoneAccountId AND cd.AccountName = md.Account
WHERE cd.[description] IN (''Successful O/G calls'',''Successful I/C calls'')
GROUP BY md.[PhoneNumber];'
	 
declare @n int
set @n = 1;

set @str += 'select [cell] AS Cell,[serial_num] AS Serial,[Plan Type],MAX([planname]) AS [Plan Name] '
set @str += ',MAX([last_call]) AS [Last Call]'
while @n <= @x
begin
	set @str += ',max([d' + convert(varchar,@n) + ']) as [Start Cycle ' + convert(varchar,@n) + '] ,max([' + convert(varchar,@n) + ']) as [Total Minutes ' + convert(varchar,@n) + ']'
	set @n += 1
end

set @str += 'from ( '
set @str += 'SELECT temp1.[cell_num] AS ''Cell'' '
set @str += ',temp1.[serial_num] AS [serial_num]'
set @str += ',temp1.[planname]'
set @str += ',CONVERT(VARCHAR(12),MAX(mdate.[max_date]), 1) AS [last_call]'
set @str += ',CONVERT(VARCHAR(12), temp1.[CallDate], 1) AS ''Start Cycle'' '
set @str += ',UPPER(LEFT(temp1.[plantype], 1)) + RIGHT(temp1.[plantype], LEN(temp1.[plantype]) - 1) AS ''Plan Type'' '
set @str += ',sum(ISNULL(c.[Duration],0)) AS ''Total Minutes'' '
set @str += ',temp1.Period '
set @str += ',''D'' + convert(varchar,temp1.Period) as calldate '

set @str += 'FROM #plan_type_temp temp1
			left join #plan_type_temp temp2 on temp2.[id] = temp1.[id] + 1 AND temp2.[order_id] = temp1.[order_id]
			INNER JOIN [MDN] mdn on [MDN].[PhoneNumber] = temp1.[cell_num]
			join [PPCData].[dbo].[CDR] c on c.PhoneAccountId = MDN.phoneAccountId
			AND c.AccountName = MDN.Account '

set @str += 'AND c.[description] IN (''Successful O/G calls'',''Successful I/C calls'') '

set @str += 'and c.[CallDate] BETWEEN temp1.[CallDate] and isnull(temp2.[CallDate],getdate())
			INNER JOIN #max_calldate mdate
			ON temp1.[cell_num] = mdate.[cell_num]
			group by temp1.cell_num,temp1.[serial_num],temp1.[planname],temp1.plantype,temp1.Period ,temp1.[CallDate]
			) ps
			PIVOT (
			sum([total minutes])
			FOR period IN
			( '

set @n = 1;
while @n <= @x
begin
	if @n>1
		set @str += ','
	set @str += '[' + convert(varchar,@n) + '] '
	set @n += 1
end

set @str += ')
			) AS pvt
			pivot(
			max([Start Cycle])
			for calldate in
			('
set @n = 1;
while @n <= @x
begin
	if @n > 1
		set @str += ','
	set @str += '[D' + convert(varchar,@n) + '] '
	set @n += 1
end

set @str += ')
			) AS pvt2
			GROUP BY Cell,[serial_num],[Plan Type]
			order by Cell
			DROP TABLE #plan_type_temp;DROP TABLE #max_calldate;'
			
exec(@str)









GO






