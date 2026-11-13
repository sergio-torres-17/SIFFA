ALTER TABLE [Add].[AddEmployee]
	ADD CONSTRAINT [Fk_AddEmployee_AddTypeId]
	FOREIGN KEY (AddTypeId)
	REFERENCES [Catalog].[AddType] (AddTypeId)
