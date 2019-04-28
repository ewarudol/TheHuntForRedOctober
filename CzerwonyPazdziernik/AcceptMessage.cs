using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik
{
	[Serializable]
	class AcceptMessage : Message
	{
		public int CurrentOccupancy { get; set; }
		public int LogicalClock { get; set; }
		public List<int> LeavingCounters { get; set; }

		public AcceptMessage(int senderRank, int currentOccupancy, int logicalClock, List<int> leavingCounters)
		{
			SenderRank = senderRank;
			CurrentOccupancy = currentOccupancy;
			LogicalClock = logicalClock;
			LeavingCounters = leavingCounters;
		}
	}
}
