CREATE TABLE [Add].[AddTypeEmployee]
(
	[AddTypeEmployeeId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
	[AddTypeId] UNIQUEIDENTIFIER NOT NULL,
	-- Solo uno de los dos viene lleno: AreaId si la asignación es por área, EmployeeTypeId si es por tipo de empleado.
	[EmployeeTypeId] UNIQUEIDENTIFIER NULL,
	[AreaId] UNIQUEIDENTIFIER NULL,
	-- Adicionales de este tipo que recibe cada empleado del rubro.
	[Quantity] INT NOT NULL DEFAULT 0,
	[InsertionDate] DATETIME NOT NULL DEFAULT GETDATE()
)
