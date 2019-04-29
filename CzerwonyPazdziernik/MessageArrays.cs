using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik
{
	static class MessageArrays
	{
		public static int[] LeaveMsgToArray(LeaveMessage msg)
		{
			int[] array = new int[4];
			array[0] = msg.SenderRank;
			array[1] = msg.Timestamp;
			array[2] = msg.Canal;
			array[3] = msg.LeavingCounter;
			return array;
		}

		public static int[] DemandMsgToArray(DemandMessage msg)
		{
			int[] array = new int[4];
			array[0] = msg.SenderRank;
			array[1] = msg.Timestamp;
			array[2] = msg.Canal;
			array[3] = (int)msg.Direction;
			return array;
		}

		public static int[] AcceptMsgToArray(AcceptMessage msg)
		{
			int[] array = new int[4 + msg.LeavingCounters.Count];
			array[0] = msg.SenderRank;
			array[1] = msg.Timestamp;
			array[2] = msg.CurrentOccupancy;
			array[3] = msg.LogicalClock;

			for (int i = 0; i < msg.LeavingCounters.Count; i++)
				array[4 + i] = msg.LeavingCounters[i];
			return array;
		}

		public static LeaveMessage LeaveArrayToMsg(int[] array)
		{
			return new LeaveMessage(array[0], array[1], array[2], array[3]);
		}

		public static DemandMessage DemandArrayToMsg(int[] array)
		{
			Constants.Directions dir = array[3] == 0 ? Constants.Directions.WEST : Constants.Directions.EAST;
			return new DemandMessage(array[0], array[1], array[2], dir);
		}

		public static AcceptMessage AcceptArrayToMsg(int[] array)
		{
			List<int> counters = new List<int>();
			for (int i = 4; i < array.Length; i++)
				counters.Add(array[i]);
			return new AcceptMessage(array[0], array[1], array[2], array[3], counters);
		}
	}
}
