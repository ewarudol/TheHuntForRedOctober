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
			int[] array = new int[2];
			array[0] = msg.SenderRank;
			array[1] = msg.Canal;
			return array;
		}

		public static int[] DemandMsgToArray(DemandMessage msg)
		{
			int[] array = new int[3];
			array[0] = msg.SenderRank;
			array[1] = msg.Canal;
			array[2] = (int)msg.Direction;
			return array;
		}

		public static int[] AcceptMsgToArray(AcceptMessage msg)
		{
			int[] array = new int[3 + msg.LeavingCounters.Count];
			array[0] = msg.SenderRank;
			array[1] = msg.CurrentOccupancy;
			array[2] = msg.LogicalClock;

			for (int i = 0; i < msg.LeavingCounters.Count; i++)
				array[3 + i] = msg.LeavingCounters[i];
			return array;
		}

		public static LeaveMessage LeaveArrayToMsg(int[] array)
		{
			return new LeaveMessage(array[0], array[1]);
		}

		public static DemandMessage DemandArrayToMsg(int[] array)
		{
			Constants.Directions dir = array[2] == 0 ? Constants.Directions.WEST : Constants.Directions.EAST;
			return new DemandMessage(array[0], array[1], dir);
		}

		public static AcceptMessage AcceptArrayToMsg(int[] array)
		{
			List<int> counters = new List<int>();
			for (int i = 3; i < array.Length; i++)
				counters.Add(array[i]);
			return new AcceptMessage(array[0], array[1], array[2], counters);
		}
	}
}
