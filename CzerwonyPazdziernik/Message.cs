using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CzerwonyPazdziernik.Constants;

namespace CzerwonyPazdziernik {
	[Serializable]
    class Message {
		public int SenderRank { get; set; }
		public MessageTypes Type { get; set; }
	}
}
