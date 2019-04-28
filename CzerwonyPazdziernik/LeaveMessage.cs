using MPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik {
	[Serializable]
    class LeaveMessage : Message {
        public int Canal { get; set; }
	
        public LeaveMessage(int senderRank, int canal) {
            SenderRank = senderRank;
            Canal = canal;
        }
    }
}
