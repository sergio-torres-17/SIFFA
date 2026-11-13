CREATE NONCLUSTERED INDEX [IX_Trace_EmployeeId_TraceDate]
	ON [AccessControl].[Trace] ([EmployeeId], [TraceDate] DESC)
