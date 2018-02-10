using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Chat_Udp
{
	public class PrimeTest
	{
		public static long quickMod(long x, long y, long p)
		{
			long res = 1;
			x %= p;
			while (y > 0)
			{
				if (y % 2 == 1)
					res = (res * x) % p;
				y /= 2;
				x = (x * x) % p;
			}
			return res;
		}

		private static bool MillerTest(int d, int n)
		{
			Random rnd = new Random();
			int a = rnd.Next(2, n - 2);
			int x = (int)quickMod(a, d, n);

			if (x == 1 || x == n - 1)
				return true;

			while (d < n - 1)
			{
				x = (x * x) % n;
				d *= 2;

				if (x == 1)
					return false;
				if (x == n - 1)
					return true;
			}
			return false;
		}

		public static bool isPrime(int n, int k)
		{
			if (n <= 1 || n == 4) return false;
			if (n <= 3) return true;

			int d = n - 1;

			while (d % 2 == 0)
				d /= 2;

			for (int i = 0; i < k; i++)
				if (MillerTest(d, n) == false)
					return false;

			return true;
		}
	}

	class RSA
	{
		private static int p = 0, q = 0, tmp1, tmp2;
		private static long N, f, d, e;
		private static List <int> prime = new List <int> ();

		public static long getKeys()
		{
			Random rnd = new Random ();

			tmp1 = rnd.Next (100, prime.Count);
			tmp2 = rnd.Next (100, prime.Count);

			if (tmp1 == tmp2)
				tmp1 += rnd.Next (20, 100);

			p = prime[tmp1];
			q = prime [tmp2];

			N = (long)(p * q);
			f = (long)((p - 1) * (q - 1));
			e = (long)(LongRandom(1, f, new Random())); //ПРОВЕРЯТЬ НА ВЗАИМНУЮ ПРОСТОТУ e и Euler(N)
			d = reverse(e, f); //ЗАПИЛИТЬ ФУНКЦИЮ МУЛЬТИПЛИКАТИВНОГО ОБРАТНОГО

			Console.Write ("p: " + Convert.ToString(p) + "; q: " + Convert.ToString(q) +
				"; e: " + Convert.ToString(e) + "; d: " + Convert.ToString(d));

			return d;
		}

		private static long LongRandom(int min, long max, Random rand) {
			byte[] buf = new byte[8];
			rand.NextBytes(buf);
			long longRand = BitConverter.ToInt64(buf, 0);

			return (Math.Abs(longRand % (max - min)) + min);
		}

		private static long reverse(long e, long f)
		{
			return ((2 * f - 1) / e);
		}

		public static void getPrimeNums()
		{
			for (int i = 100; i < int.MaxValue; i++) {
				if (PrimeTest.isPrime (i, 3)) {
					prime.Add (i);
				}
			}
		}

		public static long Encryption(long msg, long e, long n)
		{
			return PrimeTest.quickMod (msg, e, n);
		}

		public static long Decryption(long c, long d, long n)
		{
			return PrimeTest.quickMod (c, d, n);
		}
	}

	class MainClass
	{
		static int localPort; //порт приема сообщений
		static int remotePort; //порт для отправки сообщений
		static string remoteIpAddress;
		static Socket listeningSocket;

		public static void Main (string[] args)
		{
			Task getPrimeNums = new Task (RSA.getPrimeNums);
			getPrimeNums.Start ();

			bool NormIn = false;
			while (!NormIn) {
				try
				{
					Console.WriteLine ("Port for receiving messages: ");
					localPort = Convert.ToInt32 (Console.ReadLine ());
					NormIn = true;
				}
				catch { }
			}


			NormIn = false;
			while (!NormIn) {
				try
				{
					Console.WriteLine ("Port for sending messages: ");
					remotePort = Convert.ToInt32(Console.ReadLine ());
					NormIn = true;
				}
				catch { }
			}
				
			Console.WriteLine ("Server IPAddress: ");
			remoteIpAddress = Console.ReadLine ();

			try
			{
				listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				Task listeningTask = new Task(Listen);
				listeningTask.Start();

				while(true)
				{
					string message = Console.ReadLine();
					RSA.getKeys();
		//			byte[] data = Encoding.ASCII.GetBytes(message);
		//			EndPoint remotePoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);
		//			listeningSocket.SendTo(data, remotePoint);
				}
			}

			catch (Exception ex) 
			{
				Console.WriteLine (ex);
			}

			finally
			{
				Close ();
			}
		}

		private static void Listen()
		{
			try
			{
				IPEndPoint localIP = new IPEndPoint(IPAddress.Parse(remoteIpAddress), localPort);
				listeningSocket.Bind(localIP); 

				Console.WriteLine ("*****Start chating*****");

				while(true)
				{
					string message;
					byte[] data = new byte[256];

					EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

					do
					{
						listeningSocket.ReceiveFrom(data, ref remoteIp);
						message = Encoding.ASCII.GetString(data);
					}
					while (listeningSocket.Available > 0);
					IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

					if(message != "" && message != " ") 
						Console.WriteLine("{0}:{1} - {2}", remoteFullIp.Address.ToString(), remoteFullIp.Port, message);
				}
			}

			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			Close ();
		}

		private static void Close()
		{
			if (listeningSocket != null)
			{
				try{ listeningSocket.Shutdown(SocketShutdown.Both); }
				catch {}
				listeningSocket.Close();
				listeningSocket = null;
			}
		}
	}
}
