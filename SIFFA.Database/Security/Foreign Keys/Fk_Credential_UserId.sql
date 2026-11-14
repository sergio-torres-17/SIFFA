ALTER TABLE [Security].[Credential]
	ADD CONSTRAINT [Fk_Credential_UserId]
	FOREIGN KEY (UserId)
	REFERENCES [Security].[User] (UserId)
