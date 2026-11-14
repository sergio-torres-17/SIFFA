CREATE PROCEDURE [Security].[Usp_Insert_Basics]
AS
BEGIN

	


	---Insert admin if not exists
	IF NOT EXISTS (SELECT 1 FROM [Security].[User] WHERE Email = 'adminsiffa@multicodedev.com') 
		BEGIN
	
		INSERT INTO [Security].[User](UserId, FirstName, LastName, Email)
			VALUES (NEWID(), 'Main', 'Admin', 'adminsiffa@multicodedev.com')

		END

END
