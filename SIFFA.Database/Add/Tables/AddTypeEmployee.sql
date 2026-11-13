CREATE TABLE [Add].[AddTypeEmployee]
(
	[AddTypeEmployeeId] INT NOT NULL PRIMARY KEY identity(1,1),
	[AddTypeId] INT NOT NULL,
	[EmployeeId] UNIQUEIDENTIFIER NOT NULL,
	[InsertionDate] DATETIME NOT NULL DEFAULT GETDATE()
)
