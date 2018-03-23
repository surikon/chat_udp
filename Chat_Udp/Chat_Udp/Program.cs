using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
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

		private static string hex_convert(long key)
		{
			return Convert.ToString (key, 16);
		}

		private static long gcd(long a, long b)
		{
			while (b != 0)
				b = a % (a = b);
			return a;
		}

		public static long[] getKeys()
		{
			Random rnd = new Random ();

			tmp1 = rnd.Next (0, Math.Min(prime.Count, 50));
			tmp2 = rnd.Next (0, Math.Min(prime.Count, 50));

			if (tmp1 == tmp2)
				tmp1 += rnd.Next (5, 10);

			p = prime [tmp1];
			q = prime [tmp2];

			N = (long)(p * q);
			f = (long)((p - 1) * (q - 1));

			for (int i = 1; i < Math.Min(f, prime.Count); i++) {
				if ((long)prime [i] > (f % 2 == 0 ? f / 2 : f / 2 + 1)) {
					e = (long)Convert.ToInt64 (prime [i]);
					break;
				}
			}

			d = reverse(e, f); //ЗАПИЛИТЬ ФУНКЦИЮ МУЛЬТИПЛИКАТИВНОГО ОБРАТНОГО

			long[] key = new long[3];

			key [0] = N;
			key [1] = e;
			key [2] = d;

			return key;
		}

		public static long LongRandom(int min, long max, Random rand) {
			byte[] buf = new byte[8];
			rand.NextBytes(buf);
			long longRand = BitConverter.ToInt64(buf, 0);

			return (Math.Abs(longRand % (max - min)) + min);
		}

		private static long reverse(long e, long f)
		{
			long d = 10;

			while (true)
			{
				if ((d * e) % f == 1)
					break;
				else
					d++;
			}

			return d;
		}

		public static void getPrimeNums()
		{
			for (int i = 100; i < int.MaxValue - 999999; i++) {
				if (PrimeTest.isPrime (i, 3)) {
					prime.Add (i);
				}
			}
		}
			
		public static string XOR_Encrypt(byte[] arr, long key)
		{

			byte[] ans = new byte[arr.Length];
			string hex = hex_convert (key);
			for (long i = 0; i < ans.Length; i++)
				ans[i] = (byte)(arr[i] ^ hex[(int)i % hex.Length]);
			return Encoding.Unicode.GetString(ans);
		}

		public static string XOR_Decrypt(string encrypt, long key)
		{
			byte[] arr = Encoding.Unicode.GetBytes(encrypt);
			return XOR_Encrypt(arr, key);
		}
	}

	class MainClass
	{
		static int localPort; //порт приема сообщений
		static int remotePort; //порт для отправки сообщений
		static string remoteIpAddress;
		static Socket listeningSocket;
		static long[] MyKeys = new long[3];
		static long[] HerOpenKey = new long[2];
		static bool wasGettedN = false;
		static bool wasGettedKey = false;
		static bool Handshake = false;
		static bool wasGettedMainKey = false;
		static long MyMainKey = RSA.LongRandom(10, 9999, new Random());
		static long HerMainKey;
		static int HeightCursor = 0;
	
		protected static int origRow;
		protected static int origCol;

		public static void WriteAt(string s, int x, int y)
		{
			try
			{
				Console.SetCursorPosition(origCol+x, origRow+y);
				Console.Write(s);
			}
			catch (ArgumentOutOfRangeException e)
			{
				Console.Clear();
				Console.WriteLine(e.Message);
			}
		}

		public static void Main (string[] args)
		{
			Task getPrimeNums = new Task (RSA.getPrimeNums);
			getPrimeNums.Start ();

			bool NormIn = false;
			while (!NormIn) {
				try
				{
					Console.Write ("Port for receiving messages: ");
					localPort = Convert.ToInt32 (Console.ReadLine ());
					NormIn = true;
				}
				catch { }
			}
				
			NormIn = false;
			while (!NormIn) {
				try
				{
					Console.Write ("Port for sending messages: ");
					remotePort = Convert.ToInt32(Console.ReadLine ());
					NormIn = true;
				}
				catch { }
			}
				
			Console.Write ("Server IPAddress: ");
			remoteIpAddress = Console.ReadLine ();

			try
			{
				listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				Task listeningTask = new Task(Listen);
				listeningTask.Start();
			
				while(true)
				{
					byte[] data;
					EndPoint remotePoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);

					if(!Handshake || !wasGettedKey || !wasGettedN || !wasGettedMainKey)
					{
						do
						{
							Thread.Sleep(1000);
							data = Encoding.Unicode.GetBytes("0");
							listeningSocket.SendTo(data, remotePoint);
							Thread.Sleep(10);
						}	while (!Handshake);

						Console.WriteLine ("********************\nHANDSHAKE!");

						MyKeys = RSA.getKeys();
						/* 
						 * MyKeys[0] = N
						 * MyKeys[1] = e
						 * MyKeys[2] = d
						*/
						Console.Write("My close key: ");
						Console.WriteLine(MyKeys[2]);
						Console.Write("My open key: e-");
						Console.Write(MyKeys[1]);
						Console.Write(" N-");
						Console.WriteLine(MyKeys[0]);
						Console.Write("My main key: ");
						Console.WriteLine(MyMainKey);

						Console.WriteLine ("********************");

						do
						{
							data = Encoding.Unicode.GetBytes(Convert.ToString(MyKeys[0])); //Отправляем N
							listeningSocket.SendTo(data, remotePoint);
							Thread.Sleep(1000);
						} while (!wasGettedN);

						Console.Write("Her open key: N-");
						Console.Write (HerOpenKey[0]);

						do
						{
							data = Encoding.Unicode.GetBytes(Convert.ToString(MyKeys[1])); //Отправляем e
							listeningSocket.SendTo(data, remotePoint);
							Thread.Sleep(1000);
						} while (!wasGettedKey);
							
						Console.Write(" e-");
						Console.WriteLine(HerOpenKey[1]);
						Console.Write("Encoded MainKey: ");
						Console.WriteLine (PrimeTest.quickMod(MyMainKey, HerOpenKey[1], HerOpenKey[0]));
						/*
						 * HerOpenKey[0] = N
						 * HerOpenKey[1] = e
						*/
						Console.Write("Her main key: ");

						do
						{
							data = Encoding.Unicode.GetBytes(Convert.ToString(PrimeTest.quickMod(
									MyMainKey, HerOpenKey[1], HerOpenKey[0])));
							listeningSocket.SendTo(data, remotePoint);
							Thread.Sleep(1000);
						} while(!wasGettedMainKey);
							
						Console.WriteLine(HerMainKey);
						Console.WriteLine("********************");
						Thread.Sleep(3000);

						Console.Clear();
						origRow = Console.CursorTop;
						origCol = Console.CursorLeft;
					}

					Console.Write("Your message: ");
					string message = Console.ReadLine();
					message += "~#@";

					data = Encoding.Unicode.GetBytes(RSA.XOR_Decrypt(message, HerMainKey));
					listeningSocket.SendTo(data, remotePoint);
					HeightCursor++;
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

				while(true)
				{
					string message;
					byte[] data = new byte[256];

					EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

					do
					{
						listeningSocket.ReceiveFrom(data, ref remoteIp);
						message = Encoding.Unicode.GetString(data);
					}
					while
						(listeningSocket.Available > 0);

					if(!wasGettedKey || !Handshake || !wasGettedN || !wasGettedMainKey)
					{
						if(Convert.ToInt64(message) == 0)
							Handshake = true;
						else if(!wasGettedN)
						{
							wasGettedN = true;
							HerOpenKey[0] = Convert.ToInt64(message);
						}
						else if(!wasGettedKey)
						{
							HerOpenKey[1] = Convert.ToInt64(message);
							wasGettedKey = true;
							
						}
						else if(!wasGettedMainKey)
						{
							wasGettedMainKey = true;
							HerMainKey = PrimeTest.quickMod(Convert.ToInt64(message), MyKeys[2], MyKeys[0]);
						}
					}
					else
					{
						IPEndPoint remoteFullIp = remoteIp as IPEndPoint;
//						Console.Write("{0}:{1} - ", remoteFullIp.Address.ToString(), remoteFullIp.Port);

						string who = remoteFullIp.Address.ToString() + "-" + Convert.ToString(remoteFullIp.Port) + ": ";
						WriteAt(who, 0, HeightCursor);

						string decrypted_message = RSA.XOR_Encrypt(data, MyMainKey);

						for(int i = 0; i < decrypted_message.Length - 2; i++)
						{
							if(decrypted_message[i] == '~' && decrypted_message[i + 1] == '#' && decrypted_message[i + 2] == '@')
							{
								WriteAt(decrypted_message.Substring(0, i), who.Length, HeightCursor);
							}
						}
						HeightCursor++;
						WriteAt("Your Message: ", 0, HeightCursor);	
					}
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