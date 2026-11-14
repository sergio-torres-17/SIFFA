using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.AccessControl;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Services.AccessControlService
{
    public interface IAccessControlService
    {
        public GenericReponse AddTrace(InsertTraceAccessControl request);
        public bool ValidateTrace(InsertTraceAccessControl request);
    }
}
