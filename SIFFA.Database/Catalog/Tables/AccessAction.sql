CREATE TABLE [Catalog].[AccessAction]
(
	[AccessActionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[AccessActionName] NVARCHAR(50) NOT NULL,
	[AccessActionDescription] NVARCHAR(200) NULL,
	[InsertionDate] DATETIME NOT NULL DEFAULT GETDATE()
)
