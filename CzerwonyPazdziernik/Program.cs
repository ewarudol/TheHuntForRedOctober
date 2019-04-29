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
		const int speed = 5;
		readonly static List<int> canalCapacities = new List<int>
			{ 1, 2, 3, 4, 5 };

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
				waitForCommunicationHandle.WaitOne();
				int[] msgArray = new int[4 + journal.processesNumber];
				var request = comm.ImmediateReceive(Communicator.anySource, Communicator.anyTag, msgArray);
				waitForCommunicationHandle.Set();

				CompletedStatus probeResult = null;
				while (probeResult == null)
				{
					waitForCommunicationHandle.WaitOne();
					probeResult = request.Test();
					waitForCommunicationHandle.Set();
					Thread.Sleep(20);
				}

				waitForCommunicationHandle.WaitOne();

				//Obsłużenie otrzymania zgody
				if (probeResult.Tag == MSG_ACCEPT)
				{
					AcceptMessage msg = MessageArrays.AcceptArrayToMsg(msgArray);
					journal.CompareTimestampsAndUpdate(msg.Timestamp);
					journal.IncrementTimestamp();

					Console.WriteLine($"Proces {journal.shipRank}: dostałem ACCEPT od procesu {msg.SenderRank}.");

					journal.Accepts.Add(msg);
					if (journal.RememberedClock < msg.LogicalClock) //sprawdzenie zegara i zapamiętanie danych z wiadomości, jeśli nowszy
					{
						journal.RememberedClock = msg.LogicalClock;
						journal.RememberedOccupancy = msg.CurrentOccupancy;
						journal.RememberedLeavingCounters = msg.LeavingCounters;

						Console.WriteLine($"Proces {journal.shipRank}: Dostałem zgodę ze świeższym statusem, od procesu {msg.SenderRank}, według niego zajętość to {msg.CurrentOccupancy}.");
					}

					if (journal.Accepts.Count == journal.processesNumber - 1) //wszystkie zgody otrzymane
					{
						journal.CanalOfInterest.CompareClocksAndUpdate(journal.RememberedClock);
						journal.CanalOfInterest.CurrentOccupancy = journal.RememberedOccupancy;
						journal.CanalOfInterest.CurrentOccupancy++;

						//oznaczamy miejsca zwolnione przez procesy, o których wiemy, że wyszły z kanału, a wysyłający nam najświeższą zgodę nie zdążył się o tym dowiedzieć przed wysłaniem
						for (int i = 0; i < msg.LeavingCounters.Count; i++)
						{
							if (journal.CanalOfInterest.LeavingCounters[i] > journal.RememberedLeavingCounters[i])
							{
								Console.WriteLine($"Proces {journal.shipRank}: MUSZĘ ZAKTUALIZOWAĆ CURRENT OCCUPANCY! Według mnie leaving counter dla procesu {i} wynosi {journal.CanalOfInterest.LeavingCounters[i]}, a według drugiego procesu {journal.RememberedLeavingCounters[i]}.");
								journal.CanalOfInterest.CurrentOccupancy -= journal.CanalOfInterest.LeavingCounters[i] - journal.RememberedLeavingCounters[i];
							}
							if (journal.CanalOfInterest.LeavingCounters[i] < journal.RememberedLeavingCounters[i])
							{
								journal.CanalOfInterest.LeavingCounters[i] = journal.RememberedLeavingCounters[i];
							}
						}

						journal.IsDemandingEntry = false;
						journal.CanalOfInterest.IncrementClock();

						if (journal.CanalOfInterest.CurrentOccupancy == journal.CanalOfInterest.maxOccupancy) //jeśli jesteśmy ostatnim mieszczącym się w kanale procesem
						{
							Console.WriteLine($"Proces {journal.shipRank}: kanał {journal.CanalOfInterest.ID} jest teraz pełny.");
							journal.IsBlockingCanalEntry = true;
						}
						else //jeśli uważamy, że jest jeszcze miejsce (nie jesteśmy ostatnim mieszczącym się procesem), wysyłamy zgody wszystkim, którzy czekają
						{
							int currentOccupancy = journal.CanalOfInterest.CurrentOccupancy;
							int logicalClock = journal.CanalOfInterest.LogicalClock;
							List<int> leavingCounters = journal.CanalOfInterest.LeavingCounters;
							journal.IncrementTimestamp();
							AcceptMessage accept = new AcceptMessage(journal.shipRank, journal.Timestamp, currentOccupancy, logicalClock, leavingCounters);
							int[] acceptArray = MessageArrays.AcceptMsgToArray(accept);

							foreach (DemandMessage demand in journal.DemandsForSameDirection)
							{
								comm.Send(acceptArray, demand.SenderRank, MSG_ACCEPT);
								Console.WriteLine($"Proces {journal.shipRank}: po moim wejściu kanał ma jeszcze miejsce, więc wysyłam ACCEPT do procesu {demand.SenderRank} do kanału {demand.Canal}.");
							}
							journal.DemandsForSameDirection.Clear();
						}

						journal.Accepts.Clear();
						waitForAcceptsHandle.Set(); //zwolnienie blokady
					}
				}

				//Obsłużenie otrzymania żądania
				if (probeResult.Tag == MSG_DEMAND)
				{
					DemandMessage msg = MessageArrays.DemandArrayToMsg(msgArray);
					journal.CompareTimestampsAndUpdate(msg.Timestamp);
					journal.IncrementTimestamp();

					Console.WriteLine($"Proces {journal.shipRank}: dostałem DEMAND od procesu {msg.SenderRank}.");

					bool accept = false;
					if (msg.Direction != journal.DirectionOfInterest) //jeśli żądany jest przeciwny kierunek do naszego
					{
						if (journal.CanalOfInterest == null || msg.Canal != journal.CanalOfInterest.ID) //jeśli żądany jest kanał, który nas nie obchodzi
							accept = true;
						else if (journal.IsDemandingEntry && (msg.Timestamp < journal.MyDemandTimestamp || (msg.Timestamp == journal.MyDemandTimestamp && msg.SenderRank > journal.shipRank))) //jeśli nie jesteśmy jeszcze w kanale, a konkurujący ma starsze żądanie (niższy timestamp) lub tak samo stare żądanie, lecz wyższą rangę
							accept = true;
					}
					else //jeśli żądany jest ten sam kierunek
					{
						if (journal.CanalOfInterest == null || msg.Canal != journal.CanalOfInterest.ID) //jeśli żądany jest kanał, który nas nie obchodzi
							accept = true;
						else if (journal.IsDemandingEntry && (msg.Timestamp < journal.MyDemandTimestamp || (msg.Timestamp == journal.MyDemandTimestamp && msg.SenderRank > journal.shipRank))) //jeśli nie jesteśmy jeszcze w kanale, a konkurujący ma starsze żądanie (niższy timestamp) lub tak samo stare żądanie, lecz wyższą rangę
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
						journal.IncrementTimestamp();
						Console.WriteLine($"Proces {journal.shipRank}: Wysyłam zgodę procesowi {msg.SenderRank} do kanału {msg.Canal}, według mnie leaving counter żądającego w żądanym kanale to {leavingCounters[msg.SenderRank]}.");
						AcceptMessage acceptMessage = new AcceptMessage(journal.shipRank, journal.Timestamp, currentOccupancy, logicalClock, leavingCounters);
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
					LeaveMessage msg = MessageArrays.LeaveArrayToMsg(msgArray);
					journal.CompareTimestampsAndUpdate(msg.Timestamp);
					journal.IncrementTimestamp();

					Canal canal = journal.ExistingCanals[msg.Canal];

					//Jeżeli jeszcze nie wiemy o tym wyjściu
					if (canal.LeavingCounters[msg.SenderRank] < msg.LeavingCounter)
					{
						Console.WriteLine($"Proces {journal.shipRank}: dostałem LEAVE od procesu {msg.SenderRank} z kanału {msg.Canal}, jego leaving counter to {msg.LeavingCounter}, a mój {canal.LeavingCounters[msg.SenderRank]}.");
						
						//Aktualizacja licznika wyjść procesu z kanału
						canal.LeavingCounters[msg.SenderRank] = msg.LeavingCounter;

						//Jeśli jesteśmy blokującym procesem w tym kanale, zwalniamy miejsce i wysyłamy zgodę wszystkim na naszej liście oczekujących w tym samym kierunku
						canal.CurrentOccupancy--;

						if (journal.CanalOfInterest != null && journal.CanalOfInterest.ID == msg.Canal && journal.IsBlockingCanalEntry)
						{
							int currentOccupancy = canal.CurrentOccupancy;
							int logicalClock = canal.LogicalClock;
							List<int> leavingCounters = canal.LeavingCounters;
							journal.IncrementTimestamp();
							AcceptMessage accept = new AcceptMessage(journal.shipRank, journal.Timestamp, currentOccupancy, logicalClock, leavingCounters);
							int[] acceptArray = MessageArrays.AcceptMsgToArray(accept);

							foreach (DemandMessage demand in journal.DemandsForSameDirection)
								comm.Send(acceptArray, demand.SenderRank, MSG_ACCEPT);

							journal.IsBlockingCanalEntry = false;
							journal.DemandsForSameDirection.Clear();
						}
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
						journal.RememberedClock = journal.CanalOfInterest.LogicalClock;
						journal.RememberedOccupancy = journal.CanalOfInterest.CurrentOccupancy;
						journal.RememberedLeavingCounters = journal.CanalOfInterest.LeavingCounters;
						journal.IncrementTimestamp();
						journal.MyDemandTimestamp = journal.Timestamp;
						DemandMessage demand = new DemandMessage(journal.shipRank, journal.Timestamp, journal.CanalOfInterest.ID, journal.DirectionOfInterest);
						int[] demandArray = MessageArrays.DemandMsgToArray(demand);
						for (int i = 0; i < journal.processesNumber; i++)
							if (i != journal.shipRank)
								comm.Send(demandArray, i, MSG_DEMAND);
					}
					waitForCommunicationHandle.Set();

					//Czekaj na sygnał od drugiego wątku, że mamy już wszystkie zgody
					waitForAcceptsHandle.WaitOne();

					Console.WriteLine($"Proces {journal.shipRank}: wchodzę do kanału o numerze {journal.CanalOfInterest.ID} i rozpoczynam podróż. Według mnie zajętość kanału to {journal.CanalOfInterest.CurrentOccupancy}/{journal.CanalOfInterest.maxOccupancy}");
					if (journal.CanalOfInterest.CurrentOccupancy == 0)
						throw new Exception("Dotarliśmy do zera");

					//Symulacja lokalnego przetwarzania (podróż kanałem)
					Thread.Sleep(rand.Next(4) * speed);

					string printDirection = journal.DirectionOfInterest == Directions.WEST ? "zachodzie" : "wschodzie";
					Console.WriteLine($"Proces {journal.shipRank}: opuszczam kanał o numerze {journal.CanalOfInterest.ID}. Znajduję się teraz na {printDirection}.");

					waitForCommunicationHandle.WaitOne();
					{
						//Zinkrementuj licznik wyjść z kanału i wyślij informację o wyjściu
						journal.CanalOfInterest.LeavingCounters[journal.shipRank]++;
						Console.WriteLine($"Proces {journal.shipRank}: mój leaving counter kanału to: {journal.CanalOfInterest.LeavingCounters[journal.shipRank]}.");
						journal.IncrementTimestamp();
						LeaveMessage leave = new LeaveMessage(journal.shipRank, journal.Timestamp, journal.CanalOfInterest.ID, journal.CanalOfInterest.LeavingCounters[journal.shipRank]);
						int[] leaveArray = MessageArrays.LeaveMsgToArray(leave);
						for (int i = 0; i < journal.processesNumber; i++)
							if (i != journal.shipRank)
								comm.Send(leaveArray, i, MSG_LEAVE);

						//Obniż zajętość i wyślij naszą zgodę na wejście tych, którzy chcieli wejść z przeciwnej strony (niech drugi wątek w tym nie przeszkadza, np. przez dodawanie nowych żądań do listy)
						journal.CanalOfInterest.CurrentOccupancy--;
						int currentOccupancy = journal.CanalOfInterest.CurrentOccupancy;
						int logicalClock = journal.CanalOfInterest.LogicalClock;
						List<int> leavingCounters = journal.CanalOfInterest.LeavingCounters;
						journal.IncrementTimestamp();
						AcceptMessage accept = new AcceptMessage(journal.shipRank, journal.Timestamp, currentOccupancy, logicalClock, leavingCounters);
						int[] acceptArray = MessageArrays.AcceptMsgToArray(accept);
						foreach (DemandMessage demand in journal.DemandsForOppositeDirection)
						{
							comm.Send(acceptArray, demand.SenderRank, MSG_ACCEPT);
							Console.WriteLine($"Proces {journal.shipRank}: po wyjściu wysłałem procesowi {demand.SenderRank} ACCEPT do kanału {demand.Canal} (ten sam kierunek).");
							string logString = $"Proces {journal.shipRank}: wysyłana zgoda ma następujące wartości pól - Ranga: {accept.SenderRank}, Zajętość: {accept.CurrentOccupancy}, Zegar: {accept.LogicalClock}, Leaving counters: ";
							foreach (int process in accept.LeavingCounters)
								logString += $"{process} ";
							Console.WriteLine(logString);
						}
						foreach (DemandMessage demand in journal.DemandsForSameDirection)
						{
							comm.Send(acceptArray, demand.SenderRank, MSG_ACCEPT);
							Console.WriteLine($"Proces {journal.shipRank}: po wyjściu wysłałem procesowi {demand.SenderRank} ACCEPT do kanału {demand.Canal} (przeciwny kierunek).");
							string logString = $"Proces {journal.shipRank}: wysyłana zgoda ma następujące wartości pól - Ranga: {accept.SenderRank}, Zajętość: {accept.CurrentOccupancy}, Zegar: {accept.LogicalClock}, Leaving counters: ";
							foreach (int process in accept.LeavingCounters)
								logString += $"{process} ";
							Console.WriteLine(logString);
						}
						journal.DemandsForOppositeDirection.Clear();
						journal.DemandsForSameDirection.Clear();

						//Oznaczamy wyjście z kanału
						journal.CanalOfInterest = null;
						journal.IsBlockingCanalEntry = false;

						//Zmień kierunek
						journal.SwitchDirection();
					}
					waitForCommunicationHandle.Set();
				}
			});
		}
	}
}
