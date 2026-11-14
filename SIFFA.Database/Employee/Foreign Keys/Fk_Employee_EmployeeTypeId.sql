ALTER TABLE [Catalog].[Employee]
	ADD CONSTRAINT [Fk_Employee_EmployeeTypeId]
	FOREIGN KEY (EmployeeTypeId)
	REFERENCES [Catalog].[EmployeeType] (EmployeeTypeId)
