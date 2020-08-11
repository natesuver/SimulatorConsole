using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulatorConsole
{
    public class Requests : List<Request>
    {
    }

    public class Request
    {
        public Request(int id, List<long> items)
        {
            this.id = id;
            this.items = items;
        }
        public int id;
        public List<long> items;
    }
}
