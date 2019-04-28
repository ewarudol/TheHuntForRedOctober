using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik
{
	static class Constants
	{
		public const int MSG_ACCEPT = 0;
		public const int MSG_DEMAND = 1;
		public const int MSG_LEAVE = 2;

		public enum MessageTypes
		{
			DEMAND,
			ACCEPT,
			LEAVE
		}

		public enum Directions
		{
			WEST,
			EAST
		}
	}
}
