ALTER TABLE [Add].[AddTypeEmployee]
	ADD CONSTRAINT [Fk_AddTypeEmployee_AreaId]
	FOREIGN KEY (AreaId)
	REFERENCES [Catalog].[Area] (AreaId)
