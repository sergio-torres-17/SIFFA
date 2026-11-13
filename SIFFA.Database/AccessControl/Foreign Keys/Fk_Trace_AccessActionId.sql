ALTER TABLE [AccessControl].[Trace]
	ADD CONSTRAINT [Fk_Trace_AccessActionId]
	FOREIGN KEY (AccessActionId)
	REFERENCES [Catalog].[AccessAction] (AccessActionId)
