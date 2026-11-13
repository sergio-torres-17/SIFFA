CREATE NONCLUSTERED INDEX [IX_AddEmployee_EmployeeId_AddTypeId]
	ON [Add].[AddEmployee] ([EmployeeId], [AddTypeId])
	INCLUDE ([IsDeliveryAdd], [DeliveryDate])
