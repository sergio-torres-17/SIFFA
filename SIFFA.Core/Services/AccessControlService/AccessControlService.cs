using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.AccessControl;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Services.AccessControlService
{
    public class AccessControlService : IAccessControlService
    {
        public AccessControlService(SiffaContext db)
        {
            Db = db;
        }

        private SiffaContext Db { get; set; }
        public GenericReponse AddTrace(InsertTraceAccessControl request)
        {
            string message;



            return new GenericReponse(true,"Trace added successfully");
        }

        public bool IsExistsTrace(InsertTraceAccessControl request)
        {
            Trace? trace;

            trace = (from t in Db.Traces
                     join e in Db.Employees on t.Employee equals e
                     where e.InernalIdentifier.Equals(request.IdentifierNumber)
                     select t)
                     .ToList()
                     .FirstOrDefault();

            return trace != null;
        }
    }
}
