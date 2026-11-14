CREATE UNIQUE NONCLUSTERED INDEX [UX_Credential_UsernameHash]
	ON [Security].[Credential] ([UsernameHash])
	INCLUDE ([UserId], [PasswordHash])
