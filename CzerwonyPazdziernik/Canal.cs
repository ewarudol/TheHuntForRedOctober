using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik
{
	class Canal
	{
		public readonly List<int> LeavingCounters;
		public readonly int ID;
		public readonly int maxOccupancy;
		public int CurrentOccupancy { get; set; }
		public int LogicalClock { get; private set; } //z zewnątrz zegar powinien być tylko do odczytu, do modyfikacji są odpowiednie metody

		public Canal(int id, int capacity, int numberOfProcesses)
		{
			ID = id;
			maxOccupancy = capacity;

			CurrentOccupancy = 0;
			LogicalClock = 0;
			LeavingCounters = new List<int>(new int[numberOfProcesses]);
		}

		//Porównaj wartość zegara z innym i ustaw na jego wartość, jeśli jest nowszy
		public bool CompareClocksAndUpdate(int anotherClock)
		{
			if (anotherClock < LogicalClock)
				return false;
			LogicalClock = anotherClock;
			return true;
		}

		//Zwiększ wartość zegara
		public int IncrementClock()
		{
			LogicalClock++;
			return LogicalClock;
		}

		//Porównaj wartość licznika wyjść z innym i ustaw na jego wartość, jeśli jest nowszy
		public void CompareLeavingCountersAndUpdate(List<int> otherLeavingCounters)
		{
			for (int i = 0; i < LeavingCounters.Count; i++)
			{
				if (LeavingCounters[i] < otherLeavingCounters[i])
					LeavingCounters[i] = otherLeavingCounters[i];
			}
		}

		//Zwiększ wartość licznika wyjść
		public int IncrementLeavingCounter(int processRank)
		{
			LeavingCounters[processRank]++;
			return LeavingCounters[processRank];
		}
	}
}
