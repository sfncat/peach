using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Logger = NLog.Logger;
using Monitor = Peach.Core.Agent.Monitor2;
using Random = System.Random;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("你好RandoFaulter", Scope = PluginScope.Internal)]
	[Description("Generate random faults for metrics testing")]
	[Parameter("Fault", typeof(int), "How often to fault", "10")]
	[Parameter("Exception", typeof(int), "How often to throw SoftException", "100")]
	[Parameter("NewMajor", typeof(int), "How often to generate a new major", "5")]
	[Parameter("NewMinor", typeof(int), "How often to generate a new minor", "5")]
	[Parameter("Boolean", typeof(bool), "A boolean parameter", "true")]
	[Parameter("String", typeof(string), "A string parameter", "some string")]
	[Parameter("When", typeof(MonitorWhen), "An enum parameter", "OnCall")]
	[Parameter("CrashAfter", typeof(int), "Cause native process to crash after specified seconds", "-1")]
	public class RandoFaulter : Monitor
	{
		static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		readonly Random _rnd = new Random();

		public int Fault { get; set; }
		public int Exception { get; set; }
		public int NewMajor { get; set; }
		public int NewMinor { get; set; }
		public int CrashAfter { get; set; }
		public bool Boolean { get; set; }
		public string String { get; set; }
		public MonitorWhen When { get; set; }

		private const string Fmt = "X8";
		private static readonly MemoryStream Snmpv2CPacket = LoadResource("snmpv2c.pcap");
		private static readonly string[] Severity =
		{
			"EXPLOITABLE", 
			"PROBABLY EXPLOITABLE", 
			"PROBABLY NOT EXPLOITABLE", 
			"UNKNOWN",
		};

		uint _startCount;
		bool _isControl;
		readonly List<string> _majors = new List<string>();
		readonly Dictionary<string, List<string>> _minors = new Dictionary<string, List<string>>();

		[DllImport("kernel32")]
		private static extern uint SetErrorMode(uint mode);

		private const uint SEM_FAILCRITICALERRORS = 0x1;
		private const uint SEM_NOGPFAULTERRORBOX = 0x2;

		public RandoFaulter(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (CrashAfter > 0)
			{
				// Prevent Windows Error Reporting from getting in the way
				if (Platform.GetOS() == Platform.OS.Windows)
					SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);

				Task.Factory.StartNew(() =>
				{
					Logger.Info("Crashing after {0}ms", CrashAfter);
					Thread.Sleep(CrashAfter);

					Logger.Info("Crash!!!");
					var ptr = Marshal.AllocHGlobal(10);
					for (var i = 0; i < 100000; i++)
					{
						Marshal.WriteInt64(ptr, i, -1);
					}
				});
			}
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			++_startCount;

			if (Exception > 0 && _rnd.Next() % Exception == 0)
				throw new SoftException("你好 from RandoFaulter.");
		}

		public override bool DetectedFault()
		{
			// Avoid faulting on first run of the monitor
			if (_startCount < 2)
				return false;

			// Let -1 turn off faulting
			if (!_isControl && Fault > 0 && _rnd.Next() % Fault == 0)
				return true;

			return false;
		}

		public override MonitorData GetMonitorData()
		{
			var buckets = GetBuckets();

			var ret = new MonitorData
			{
				Title = "你好 from RandoFaulter",
				Fault = new MonitorData.Info
				{
					Description = @"
機除拍禁響地章手棚国歳違不。氏検郷掲左発中時東自想図金観図配比。線加経購問読場舞似市前施。日談天出掲放非歴絵率右胎著義。録結取福歳更来思残読者対田媛水季境愛。者容能晴愛品芸羽著記将高違権界民。喜紹覧刊界準乗教待断皇地社学宇種次書者指。星習洋許機経教代崎会鋭覧情恋創職説。請写理行夫町季気事求塚講早関広姉。

על מדע לכאן לטיפול, יוני קסאם חבריכם אנא גם. אם ציור צילום פילוסופיה כלל, שימושי לאחרונה על אנא. גם אחד המזנון רשימות, או ארץ מחליטה טבלאות לאחרונה, של סדר רקטות ספרדית. מדע גם היום שמות, ערבית מחליטה ומהימנה של זכר, רבה ב הבקשה הסביבה לימודים. אחרים אחרונים או צ'ט.

وسمّيت عشوائية تشيكوسلوفاكيا جعل أن. لم استدعى وسمّيت بولندا، حول. تم المضي الأمريكي الأبرياء تعد, في قررت غينيا بأيدي مكن. تلك قد أحدث قبضتهم الجديدة،, واستمرت الجنرال المتّبعة إذ فعل, بهيئة إحتار التقليدية به، ان. نفس بل تكبّد محاولات أوراقهم.

Συ φιξ φερι θεμπορ ομνεσκυε, κυις φασιλισι εσθ εξ. Πρι ει σεθερο ασυσαθα, μει σασε περσιυς σονσλυσιονεμκυε ιδ. Νιβχ μεις θεμπορ ιν κυι. Σολυμ ευισμοδ φολυτπατ περ ιδ. Θε κυιδαμ σαυσαε εσθ, κυις προβο σιφιβυς ιυς εξ, ευμ εξ ποσιμ διγνισιμ πχιλωσοπηια.

Нык ут адмодум пльакырат зэнтынтиаэ, агам мёнём конвынёры хёз ед, мыа эи емпэтюсъ конжтетуто чадипжкёнг. Эа агам жэмпэр мэя, ыт эрож омнэжквюы хаж. Прё ад доминг ыкжплььикари губэргрэн. Про йн хабымуч тхэопхражтуз, ут путант интыллыгам вяш. Нык нонумй пожжёт дытракжйт эю, ед квюо клита дикунт, ты пошжим аюдиам рыкючабо мэя.

B|00 z3aRcH, why 1T. 1n70 r33dz0r 4s m4y, 4rE t3xt w4nN@ 1+. W17h 51T3$. != 70p, w1ll r1tez != f0r. Y0 N0+ qu3ry 3N9l1sh (4(]-[3z, 0n z33 alz0 34513r p@r+1cUL4r, qu3ry 4cc355 d159l4y3d, aLL y4. 83 4|| d0cum3nt INFoRm4T10N, y0 yOU, |3tz0rz aLL. 0n kn0w LINk p@r+1cUL4r 937.
",
					MajorHash = buckets[0],
					MinorHash = buckets[1],
					Risk = Severity[_rnd.Next(Severity.Length)],
				},
				Data = new Dictionary<string, Stream>
				{
					{ "NetworkCapture1.pcap", Snmpv2CPacket },
					{ "NetworkCapture2.pcapng", Snmpv2CPacket },
					{ "BinaryData.bin", Snmpv2CPacket },
					{ "機除拍禁響地章手棚国歳違不.pcap", Snmpv2CPacket },
				}
			};

			return ret;
		}

		public override void Message(string msg)
		{
			switch (msg.ToLower())
			{
				case "true":
					_isControl = true;
					break;
				case "false":
					_isControl = false;
					break;
			}
		}

		private string[] GetBuckets()
		{
			string major;
			string minor;

			if (_majors.Count == 0 || _rnd.Next() % NewMajor == 0)
			{
				major = _rnd.Next().ToString(Fmt);
				minor = _rnd.Next().ToString(Fmt);

				_majors.Add(major);
				_minors[major] = new List<string> { minor };

				return new[] { major, minor };
			}

			major = _majors[_rnd.Next(_majors.Count)];

			if (_rnd.Next() % NewMinor == 0)
			{
				do
				{
					minor = _rnd.Next().ToString(Fmt);
				}
				while (_minors[major].Contains(minor));

				_minors[major].Add(minor);

				return new[] { major, minor };
			}

			minor = _minors[major][_rnd.Next(_minors[major].Count)];

			return new[] { major, minor };
		}

		private static MemoryStream LoadResource(string name)
		{
			var asm = Assembly.GetExecutingAssembly();
			var fullName = "Peach.Pro.Core.Resources." + name;
			using (var stream = asm.GetManifestResourceStream(fullName))
			{
				Debug.Assert(stream != null);
				var ms = new MemoryStream();
				stream.CopyTo(ms);
				return ms;
			}
		}
	}
}
