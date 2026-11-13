CREATE TABLE [Catalog].[Area]
(
	[AreaId] INT NOT NULL PRIMARY KEY identity(1,1),
	[description] NVARCHAR(100) NOT NULL,
	[InsertionDate] DATETIME NOT NULL DEFAULT GETDATE()
)
