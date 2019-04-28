using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MPI;
using static CzerwonyPazdziernik.Constants;

namespace CzerwonyPazdziernik
{
	class Program
	{

		//Określenie liczby i rozmiarów kanałów, szybkości wykonywania
		const int speed = 2500;
		readonly static List<int> canalCapacities = new List<int>
			{ 1 };

		//Dane procesu
		static ShipJournal journal;
		static Random rand;
		static EventWaitHandle waitForAcceptsHandle = new AutoResetEvent(false);
		static EventWaitHandle waitForCommunicationHandle = new AutoResetEvent(true);
		static Intracommunicator comm;

		public static void CommunicatorThread()
		{
			while (true)
			{
				var probeResult = comm.Probe(Communicator.anySource, Communicator.anyTag);

				waitForCommunicationHandle.WaitOne();

				Console.WriteLine($"Proces {journal.shipRank}: zamierzam odebrać wiadomość o tagu {probeResult.Tag}.");

				//Obsłużenie otrzymania zgody
				if (probeResult.Tag == MSG_ACCEPT)
				{
					int[] msgArray = new int[3+journal.processesNumber];
					comm.Receive(Communicator.anySource, MSG_ACCEPT, ref msgArray);
					AcceptMessage msg = MessageArrays.AcceptArrayToMsg(msgArray);

					Console.WriteLine($"Proces {journal.shipRank}: dostałem ACCEPT od procesu {msg.SenderRank}.");

					journal.Accepts.Add(msg);
					if (journal.CanalOfInterest.CompareClocksAndUpdate(msg.LogicalClock)) //sprawdzenie zegara i aktualizacja zajętości, jeśli nowszy
					{
						journal.CanalOfInterest.CurrentOccupancy = msg.CurrentOccupancy;
						journal.RememberedLeavingCounters = msg.LeavingCounters;
					}

					if (journal.Accepts.Count == journal.processesNumber - 1) //wszystkie zgody otrzymane
					{
						journal.CanalOfInterest.CurrentOccupancy++;

						//oznaczamy miejsca zwolnione przez procesy, o których wiemy, że wyszły z kanału, a wysyłający nam najświeższą zgodę nie zdążył się o tym dowiedzieć przed wysłaniem
						for (int i = 0; i < msg.LeavingCounters.Count; i++)
						{
							if (journal.CanalOfInterest.LeavingCounters[i] > journal.RememberedLeavingCounters[i])
							{
								Console.WriteLine($"Proces {journal.shipRank}: muszę zaktualizować current occupancy! Według mnie leaving counter dla procesu {i} wynosi {journal.CanalOfInterest.LeavingCounters[i]}, a według drugiego procesu {journal.RememberedLeavingCounters[i]}.");
								journal.CanalOfInterest.CurrentOccupancy -= journal.CanalOfInterest.LeavingCounters[i] - journal.RememberedLeavingCounters[i];
							}
						}

						if (journal.CanalOfInterest.CurrentOccupancy == journal.CanalOfInterest.maxOccupancy)
							journal.IsBlockingCanalEntry = true;

						//TUTAJ DOPISAĆ WYSYŁANIE ZGÓD Z LISTY ŻADAŃ W TYM SAMYM KIERUNKU JEŚLI NIE BLOKUJEMY KANAŁU

						journal.IsDemandingEntry = false;
						journal.CanalOfInterest.IncrementClock();

						journal.Accepts.Clear();
						waitForAcceptsHandle.Set(); //zwolnienie blokady
					}
				}

				//Obsłużenie otrzymania żądania
				if (probeResult.Tag == MSG_DEMAND)
				{
					int[] msgArray = new int[3];
					comm.Receive(Communicator.anySource, MSG_DEMAND, ref msgArray);
					DemandMessage msg = MessageArrays.DemandArrayToMsg(msgArray);

					Console.WriteLine($"Proces {journal.shipRank}: dostałem DEMAND od procesu {msg.SenderRank}.");

					bool accept = false;
					if (msg.Direction != journal.DirectionOfInterest) //jeśli żądany jest przeciwny kierunek do naszego
					{
						if (journal.CanalOfInterest == null || msg.Canal != journal.CanalOfInterest.ID) //jeśli żądany jest kanał, który nas nie obchodzi
							accept = true;
						else if (journal.IsDemandingEntry && msg.SenderRank > journal.shipRank && journal.Accepts.Where(x => x.SenderRank == msg.SenderRank).FirstOrDefault() == null) //jeśli nie jesteśmy jeszcze w kanale, a konkurujący ma wyższą rangę i nie dał nam jeszcze zgody samemu
							accept = true;
					}
					else //jeśli żądany jest ten sam kierunek
					{
						if (journal.CanalOfInterest == null || msg.Canal != journal.CanalOfInterest.ID) //jeśli żądany jest kanał, który nas nie obchodzi
							accept = true;
						else if (journal.IsDemandingEntry && msg.SenderRank > journal.shipRank && journal.Accepts.Where(x => x.SenderRank == msg.SenderRank).FirstOrDefault() == null) //jeśli nie jesteśmy jeszcze w kanale, a konkurujący ma wyższą rangę i nie dał nam jeszcze zgody samemu
							accept = true;
						else if (!journal.IsDemandingEntry && !journal.IsBlockingCanalEntry) //jeśli jesteśmy w kanale (nie ubiegamy się o dostęp), ale nie blokujemy dostępu do kanału
							accept = true;
					}

					if (accept)
					{
						Canal desiredCanal = journal.ExistingCanals[msg.Canal];
						int currentOccupancy = desiredCanal.CurrentOccupancy;
						int logicalClock = desiredCanal.LogicalClock;
						List<int> leavingCounters = desiredCanal.LeavingCounters;
						Console.WriteLine($"Proces {journal.shipRank}: Wysyłam zgodę, według mnie leaving counter żądającego w żądanym kanale to {leavingCounters[msg.SenderRank]}.");
						AcceptMessage acceptMessage = new AcceptMessage(journal.shipRank, currentOccupancy, logicalClock, leavingCounters);
						int[] acceptArray = MessageArrays.AcceptMsgToArray(acceptMessage);
						string logString = $"Proces {journal.shipRank}: wysyłana zgoda ma następujące wartości pól - Ranga: {acceptMessage.SenderRank}, Zajętość: {acceptMessage.CurrentOccupancy}, Zegar: {acceptMessage.LogicalClock}, Leaving counters: ";
						foreach (int process in acceptMessage.LeavingCounters)
							logString += $"{process} ";
						Console.WriteLine(logString);
						comm.Send(acceptArray, msg.SenderRank, MSG_ACCEPT);
					}
					else

					{
						if (msg.Direction == journal.DirectionOfInterest)
							journal.DemandsForSameDirection.Add(msg);
						else
							journal.DemandsForOppositeDirection.Add(msg);
					}
				}

				//Obsłużenie otrzymania wiadomości o wyjściu z kanału
				if (probeResult.Tag == MSG_LEAVE)
				{
					int[] msgArray = new int[2];
					comm.Receive(Communicator.anySource, MSG_LEAVE, ref msgArray);
					LeaveMessage msg = MessageArrays.LeaveArrayToMsg(msgArray);

					//Inkrementacja licznika wyjść procesu z kanału
					journal.ExistingCanals[msg.Canal].LeavingCounters[msg.SenderRank]++;

					Console.WriteLine($"Proces {journal.shipRank}: dostałem LEAVE od procesu {msg.SenderRank}.");

					//Jeśli jesteśmy blokującym procesem w tym kanale, zwalniamy miejsce i wysyłamy zgodę wszystkim na naszej liście oczekujących w tym samym kierunku
					if (journal.CanalOfInterest != null && journal.CanalOfInterest.ID == msg.Canal && journal.IsBlockingCanalEntry)
					{
						journal.CanalOfInterest.CurrentOccupancy--;

						int currentOccupancy = journal.CanalOfInterest.CurrentOccupancy;
						int logicalClock = journal.CanalOfInterest.LogicalClock;
						List<int> leavingCounters = journal.CanalOfInterest.LeavingCounters;
						AcceptMessage accept = new AcceptMessage(journal.shipRank, currentOccupancy, logicalClock, leavingCounters);
						int[] acceptArray = MessageArrays.AcceptMsgToArray(accept);

						foreach (DemandMessage demand in journal.DemandsForSameDirection)
							comm.Send(acceptArray, demand.SenderRank, MSG_ACCEPT);
						journal.DemandsForSameDirection.Clear();

						journal.IsBlockingCanalEntry = false;
						journal.DemandsForSameDirection.Clear();
					}
				}

				waitForCommunicationHandle.Set();
			}
		}

		//mpiexec -np 4 CzerwonyPazdziernik.exe
		static void Main(string[] args)
		{
			MPI.Environment.Run(ref args, Threading.Multiple, receivedComm =>
			{
				//Inicjalizacja
				comm = receivedComm;
				journal = new ShipJournal(canalCapacities, comm.Size, comm.Rank);

				//Inicjalizacja losowości
				Thread.Sleep(comm.Rank);
				rand = new Random();

				//Uruchomienie wątku komunikacyjnego
				Thread communicator = new Thread(new ThreadStart(CommunicatorThread));
				communicator.Start();

				Console.WriteLine($"Proces {journal.shipRank}: jest łącznie {journal.processesNumber} procesów.");

				while (true)
				{
					Console.WriteLine($"Proces {journal.shipRank}: rozpoczynam lokalne przetwarzanie.");

					//Symulacja lokalnego przetwarzania (magazynowanie/ameryka)
					Thread.Sleep(rand.Next(4) * speed);

					waitForCommunicationHandle.WaitOne();
					{
						//Wylosuj obiekt pożądania (kanał)
						journal.CanalOfInterest = journal.ExistingCanals[rand.Next(journal.ExistingCanals.Count)];

						Console.WriteLine($"Proces {journal.shipRank}: ubiegam się o kanał o numerze {journal.CanalOfInterest.ID}.");

						//Wyślij żądania i ustaw remembered leaving counters na własne (jesteśmy "swoją pierwszą zgodą")
						journal.IsDemandingEntry = true;
						journal.RememberedLeavingCounters = journal.CanalOfInterest.LeavingCounters;
						DemandMessage demand = new DemandMessage(journal.shipRank, journal.CanalOfInterest.ID, journal.DirectionOfInterest);
						int[] demandArray = MessageArrays.DemandMsgToArray(demand);
						for (int i = 0; i < journal.processesNumber; i++)
							if (i != journal.shipRank)
								comm.Send(demandArray, i, MSG_DEMAND);
					}
					waitForCommunicationHandle.Set();

					//Czekaj na sygnał od drugiego wątku, że mamy już wszystkie zgody
					waitForAcceptsHandle.WaitOne();

					Console.WriteLine($"Proces {journal.shipRank}: wchodzę do kanału o numerze {journal.CanalOfInterest.ID} i rozpoczynam podróż. Według mnie zajętość kanału to {journal.CanalOfInterest.CurrentOccupancy}/{journal.CanalOfInterest.maxOccupancy}");

					//Symulacja lokalnego przetwarzania (podróż kanałem)
					Thread.Sleep(rand.Next(4) * speed);

					string printDirection = journal.DirectionOfInterest == Directions.WEST ? "zachodzie" : "wschodzie";
					Console.WriteLine($"Proces {journal.shipRank}: opuszczam kanał o numerze {journal.CanalOfInterest.ID}. Znajduję się teraz na {printDirection}.");

					waitForCommunicationHandle.WaitOne();
					{
						//Zinkrementuj licznik wyjść z kanału i wyślij informację o wyjściu
						journal.CanalOfInterest.LeavingCounters[journal.shipRank]++;
						Console.WriteLine($"Proces {journal.shipRank}: mój leaving counter kanału to: {journal.CanalOfInterest.LeavingCounters[journal.shipRank]}.");
						LeaveMessage leave = new LeaveMessage(journal.shipRank, journal.CanalOfInterest.ID);
						int[] leaveArray = MessageArrays.LeaveMsgToArray(leave);
						for (int i = 0; i < journal.processesNumber; i++)
							if (i != journal.shipRank)
								comm.Send(leaveArray, i, MSG_LEAVE);

						//Obniż zajętość i wyślij naszą zgodę na wejście tych, którzy chcieli wejść z przeciwnej strony (niech drugi wątek w tym nie przeszkadza, np. przez dodawanie nowych żądań do listy)
						journal.CanalOfInterest.CurrentOccupancy--;
						int currentOccupancy = journal.CanalOfInterest.CurrentOccupancy;
						int logicalClock = journal.CanalOfInterest.LogicalClock;
						List<int> leavingCounters = journal.CanalOfInterest.LeavingCounters;
						AcceptMessage accept = new AcceptMessage(journal.shipRank, currentOccupancy, logicalClock, leavingCounters);
						int[] acceptArray = MessageArrays.AcceptMsgToArray(accept);
						foreach (DemandMessage demand in journal.DemandsForOppositeDirection)
						{
							comm.Send(acceptArray, demand.SenderRank, MSG_ACCEPT);
							Console.WriteLine($"Proces {journal.shipRank}: wysłałem procesowi {demand.SenderRank} ACCEPT.");
						}
						foreach (DemandMessage demand in journal.DemandsForSameDirection)
						{
							comm.Send(acceptArray, demand.SenderRank, MSG_ACCEPT);
							Console.WriteLine($"Proces {journal.shipRank}: wysłałem procesowi {demand.SenderRank} ACCEPT.");
						}
						journal.DemandsForOppositeDirection.Clear();
						journal.DemandsForSameDirection.Clear();

						//Oznaczamy wyjście z kanału
						journal.CanalOfInterest = null;

						//Zmień kierunek
						journal.SwitchDirection();
					}
					waitForCommunicationHandle.Set();
				}
			});
		}
	}
}
