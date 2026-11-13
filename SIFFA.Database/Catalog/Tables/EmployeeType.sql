CREATE TABLE [Catalog].[EmployeeType]
(
	[EmployeeTypeId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[EmployeeTypeName] NVARCHAR(50) NOT NULL,
	[EmployeeTypeDescription] NVARCHAR(200) NULL,
	[InsertionDate] DATETIME NOT NULL DEFAULT GETDATE(),
)
