ALTER TABLE [Add].[AddTypeEmployee]
	ADD CONSTRAINT [Fk_AddTypeEmployee_EmployeeId]
	FOREIGN KEY (EmployeeId)
	REFERENCES [Catalog].[Employee] (EmployeeId)
