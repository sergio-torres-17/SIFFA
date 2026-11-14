using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Models.Custom.Request.AccessControl
{
    public record InsertTraceAccessControl
    {
        public string? IdentifierNumber { get; set; }
        public AccessType AccessType { get; set; }
    }
}
