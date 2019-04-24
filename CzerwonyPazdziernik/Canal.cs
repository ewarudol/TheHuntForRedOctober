using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik {
    class Canal {
        public int ID { get; set; }
        public int MaxOccupancy { get; set; }
        public int CurrentOccupancy { get; set; }
        public int LogicalClock { get; set; }
        public List<int> LeavingCounters { get; set; }

        public Canal(int id, int capacity) {
            ID = id;
            MaxOccupancy = capacity;
            
            //LeavingCounters = new List<int>
        }
    }
}
