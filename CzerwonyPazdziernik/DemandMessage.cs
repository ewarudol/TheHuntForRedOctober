using MPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik {
    class DemandMessage : Message {
        public int SenderRank { get; set; }
        public int Canal { get; set; }

        public DemandMessage(Intercommunicator comm, int canal) {
            SenderRank = comm.Rank;
            Canal = canal;
        }
    }
}
