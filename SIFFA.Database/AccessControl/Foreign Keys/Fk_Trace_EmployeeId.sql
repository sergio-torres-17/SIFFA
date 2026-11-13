ALTER TABLE [AccessControl].[Trace]
	ADD CONSTRAINT [Fk_Trace_EmployeeId]
	FOREIGN KEY (EmployeeId)
	REFERENCES [Catalog].[Employee] (EmployeeId)
