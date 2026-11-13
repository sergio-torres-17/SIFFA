ALTER TABLE [Catalog].[Employee]
	ADD CONSTRAINT [Fk_Employee_AreaId]
	FOREIGN KEY (AreaId)
	REFERENCES [Catalog].[Area] (AreaId)
