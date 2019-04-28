using MPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik {
	[Serializable]
    class DemandMessage : Message {
        public int Canal { get; set; }
		public Constants.Directions Direction { get; set; }
		
        public DemandMessage(int senderRank, int canal, Constants.Directions direction)
		{
			SenderRank = senderRank;
			Canal = canal;
			Direction = direction;
			Type = Constants.MessageTypes.DEMAND;
        }
    }
}
