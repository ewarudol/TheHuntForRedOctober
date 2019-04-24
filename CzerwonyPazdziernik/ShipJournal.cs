using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CzerwonyPazdziernik {
    class ShipJournal {
        /*$Flaga blokująca (FB): 0
        $Flaga ubiegania się (FU): 0
        $Wylosowany kanał (K) : null
        $Aktualna zajętość kanału (AKT_ZAJ): 0
        $Maksymalna zajętosć kanału (MAX_ZAJ) : 0
        $Licznik zmian mojego kanału (COUNT): 0
        $Lista oczekuących (LO) : empty
        $Liczba zgód (ZGODY) : 0
        $Liczba procesów: n
        $Mój priorytet (P) : null
        $Lista (LT) Tablic wyjść z kanału (LX)*/

        public readonly List<Canal> ExistingCanals;
        public readonly int ProcessesNumber;
        public readonly int ShipRank;

        public bool IsBlockingCanalEntry { get; set; }
        public bool IsDemandingEntry { get; set; }
        public Canal CanalOfInterest { get; set; }
        public List<DemandMessage> DemandsAwaitingAcceptance { get; set; }
        public int NumberOfReceivedAccepts { get; set; }
        
        public ShipJournal(int canalsNum, int processesNum, int shipRank) {

        }
    }
}
