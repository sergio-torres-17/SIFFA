ALTER TABLE [Add].[AddTypeEmployee]
	ADD CONSTRAINT [Fk_AddTypeEmployee_EmployeeTypeId]
	FOREIGN KEY (EmployeeTypeId)
	REFERENCES [Catalog].[EmployeeType] (EmployeeTypeId)
