CREATE PROCEDURE [Security].[Usp_Insert_Basics]
AS
BEGIN
	
	CREATE TABLE #TempRows(
		[Key] NVARCHAR(50) NOT NULL,
		[Value] NVARCHAR(MAX) NOT NULL
	)
	
	INSERT INTO #TempRows
	VALUES
		('AddType', 'Boletos para agua'),
		('AddType', 'Boletos para comida'),
		('AddType', 'Boletos para bebida alcoholica'),
		('AccessAction', 'Ingreso'),
		('AccessAction', 'Salida')

		;

	---Insert admin if not exists
	IF NOT EXISTS (SELECT 1 FROM [Security].[User] WHERE Email = 'adminsiffa@multicodedev.com') 
	BEGIN
	
		INSERT INTO [Security].[User](UserId, FirstName, LastName, Email)
			VALUES (NEWID(), 'Main', 'Admin', 'adminsiffa@multicodedev.com')

	END

	--Insert add types
	INSERT INTO [Catalog].AddType(AddTypeName, AddTypeDescription)
	SELECT [Value], [Value] 
	FROM #TempRows 
	LEFT JOIN [Catalog].AddType [AT] ON [AT].AddTypeName COLLATE DATABASE_DEFAULT =  [Value]
	WHERE [Key] = 'AddType' AND [Value] IS NOT NULL AND [AT].AddTypeName IS NULL

	--Insert AccessAction
	INSERT INTO [Catalog].AccessAction(AccessActionName, AccessActionDescription)
	SELECT [Value], [Value]
	FROM #TempRows
	LEFT JOIN [Catalog].AccessAction [AA] ON [AA].AccessActionName COLLATE DATABASE_DEFAULT =  [Value]
	WHERE [Key] = 'AccessAction' AND [Value] IS NOT NULL AND [AA].AccessActionName IS NULL

END
