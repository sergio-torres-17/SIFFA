ALTER TABLE [Add].[AddTypeEmployee]
	ADD CONSTRAINT [Fk_AddTypeEmployee_AddTypeId]
	FOREIGN KEY (AddTypeId)
	REFERENCES [Catalog].[AddType] (AddTypeId)
