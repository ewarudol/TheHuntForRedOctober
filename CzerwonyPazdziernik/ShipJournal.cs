using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CzerwonyPazdziernik.Constants;

namespace CzerwonyPazdziernik
{
	class ShipJournal
	{
		public readonly List<Canal> ExistingCanals;
		public readonly int processesNumber;
		public readonly int shipRank;
		public readonly List<DemandMessage> DemandsForSameDirection;
		public readonly List<DemandMessage> DemandsForOppositeDirection;
		public int Timestamp { get; private set; }
		public int MyDemandTimestamp { get; set; }
		public bool IsBlockingCanalEntry { get; set; }
		public bool IsDemandingEntry { get; set; }
		public Canal CanalOfInterest { get; set; }
		public List<AcceptMessage> Accepts { get; private set; }
		public List<int> RememberedLeavingCounters { get; set; }
		public int RememberedOccupancy { get; set; }
		public int RememberedClock { get; set; }
		public Directions DirectionOfInterest { get; private set; }

		public ShipJournal(List<int> canalCapacities, int processesNum, int shipRank)
		{
			ExistingCanals = new List<Canal>();
			for (int i = 0; i < canalCapacities.Count; i++)
				ExistingCanals.Add(new Canal(i, canalCapacities[i], processesNum));

			processesNumber = processesNum;
			this.shipRank = shipRank;
			IsBlockingCanalEntry = false;
			IsDemandingEntry = false;
			CanalOfInterest = null;
			DemandsForSameDirection = new List<DemandMessage>();
			DemandsForOppositeDirection = new List<DemandMessage>();
			Accepts = new List<AcceptMessage>();
			DirectionOfInterest = Directions.WEST;
			Timestamp = 0;
			MyDemandTimestamp = 0;
		}

		public Directions SwitchDirection()
		{
			if (DirectionOfInterest == Directions.WEST)
				DirectionOfInterest = Directions.EAST;
			else
				DirectionOfInterest = Directions.WEST;
			return DirectionOfInterest;
		}

		public int CompareTimestampsAndUpdate(int anotherTimestamp)
		{
			if (anotherTimestamp > Timestamp)
				Timestamp = anotherTimestamp;
			return Timestamp;
		}

		public int IncrementTimestamp()
		{
			Timestamp++;
			return Timestamp;
		}
	}
}
