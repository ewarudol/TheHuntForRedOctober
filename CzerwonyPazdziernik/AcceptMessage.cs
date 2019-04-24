using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik {
    class AcceptMessage : Message {
        public int CurrentOccupancy { get; set; }
        public int LogicalClock { get; set; }
    }
}
