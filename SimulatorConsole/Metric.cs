using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulatorConsole
{
    public class Metric
    {
        public decimal CpuTime { get; set; }
        public decimal ElapsedTime { get; set; }
        public decimal LogicalReads { get; set; }
        public decimal PhysicalReads { get; set; }

        public decimal TotalElapsedTime { get; set; }
        public int TotalExecutionCount { get; set; }

    }
}
