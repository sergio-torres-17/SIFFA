CREATE TABLE [Add].[AddEmployee]
(
	[AddEmployeeId] INT NOT NULL PRIMARY KEY identity(1,1),
	[AddTypeId] INT NOT NULL,
	[EmployeeId] UNIQUEIDENTIFIER NOT NULL,
	[IsDeliveryAdd] BIT NOT NULL DEFAULT 0,
	[DeliveryDate] DATETIME NULL,
	[InsertionDate] DATETIME NOT NULL DEFAULT GETDATE()
)
