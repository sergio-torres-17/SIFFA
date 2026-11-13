ALTER TABLE [Add].[AddEmployee]
	ADD CONSTRAINT [Fk_AddEmployee_EmployeeId]
	FOREIGN KEY (EmployeeId)
	REFERENCES [Catalog].[Employee] (EmployeeId)
