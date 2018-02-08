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
		private static int quickMod(int x, long y, int p)
		{
			int res = 1;
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
			int x = quickMod(a, d, n);

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
		private static int p = 0, q = 0, N, e, tmp1, tmp2, f, d;

		public static int getKeys()
		{
			Random rnd = new Random ();

			tmp1 = rnd.Next (100, 4000);
			tmp2 = rnd.Next (100, 4000);

			if (tmp1 == tmp2)
				tmp1 += rnd.Next (20, 500);

			for (int k = 0, s = 0; s <= Math.Max (tmp1, tmp2); k++) 
			{
				if (PrimeTest.isPrime (k, 3)) {
					if (s == tmp1 && p == 0)
						p = k;
					if (s == tmp2 && q == 0)
						q = k;
					s++;	
				}
				if (p != 0 && q != 0)
					break;
			}
				
			N = p * q;
			f = Euler (Math.Abs (N));
			e = rnd.Next(1, f); //ПРОВЕРЯТЬ НА ВЗАИМНУЮ ПРОСТОТУ e и Euler(N)
			d = reverse(e, f); //ЗАПИЛИТЬ ФУНКЦИЮ МУЛЬТИПЛИКАТИВНОГО ОБРАТНОГО

			return e;
		}

		private static int reverse(int x, int m)
		{
			return -1;
		}

		private static int Euler(int n)
		{
			int result = n;
			for (int i = 2; i * i <= n; i++)
				if (n % i == 0) 
				{
					while (n % i == 0)
						n /= i;
					result -= result / i;
				}
			if (n > 1)
				result -= result / n;
			return result;
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
			Console.WriteLine (RSA.getKeys ());

			Console.WriteLine ("Port for receiving messages: ");
			localPort = Convert.ToInt32 (Console.ReadLine ());
			Console.WriteLine ("Port for sending messages: ");
			remotePort = Convert.ToInt32(Console.ReadLine ());
			Console.WriteLine ("Server IPAddress: ");
			remoteIpAddress = Console.ReadLine ();

			try
			{
				listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				Task listeningTask = new Task(Listen);
				listeningTask.Start();

				Console.WriteLine ("*****Start chating*****");
				while(true)
				{
					string message = Console.ReadLine();

					byte[] data = Encoding.ASCII.GetBytes(message);
					EndPoint remotePoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);
					listeningSocket.SendTo(data, remotePoint);
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
				listeningSocket.Bind(localIP); //???

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

			finally 
			{
				Close ();
			}
		}

		private static void Close()
		{
			if (listeningSocket != null)
			{
				listeningSocket.Shutdown(SocketShutdown.Both);
				listeningSocket.Close();
				listeningSocket = null;
			}
		}
	}
}
