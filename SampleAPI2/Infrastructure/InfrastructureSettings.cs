using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleAPI2.Infrastructure
{
    public class InfrastructureSettings
    {
        public string ApiUrl { get; set; }

        public bool Mocks { get; set; } = false;
    }
}
