using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.ManagementPlattform;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Services.ManagementPlattformService
{
    public interface IManagementPlattform
    {

        public Task<GenericReponse> ActivateSystem(ActivateSystemRequest request);
    }
}
