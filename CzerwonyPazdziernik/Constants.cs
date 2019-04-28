using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik
{
	static class Constants
	{
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
