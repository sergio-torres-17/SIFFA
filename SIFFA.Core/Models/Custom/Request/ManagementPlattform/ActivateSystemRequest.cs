using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Models.Custom.Request.ManagementPlattform
{
    public record ActivateSystemRequest
    {
        public string SeedActivation { get; set; }
        public string PasswordAdmin { get; set; }
    }
}
