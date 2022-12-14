--procedure used to query table data
ALTER PROCEDURE dbo.spUpdateRates
	@TITLE varchar(60),
	@CODE varchar(3),
	@VALUE numeric(18,2),
	@DATE date
AS
BEGIN
	SET NOCOUNT OFF;
	
	IF NOT EXISTS (SELECT * FROM dbo.R_CURRENCY WHERE CODE = @CODE AND A_DATE = @DATE)
	BEGIN
		INSERT INTO R_CURRENCY(TITLE, CODE, VALUE, A_DATE)
		VALUES (@TITLE, @CODE, @VALUE, @DATE);
	END
	
END
GO
