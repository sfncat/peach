using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Peach.Core
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class PlatformImplAttribute : Attribute
	{
		public Platform.OS OS { get; private set; }

		public PlatformImplAttribute(Platform.OS OS)
		{
			this.OS = OS;
		}
	}

	public class PlatformFactory<T> where T : class
	{
		private static Type impl = FindImpl();

		private static Type FindImpl()
		{
			Platform.OS os = Platform.GetOS();
			Type type = typeof(T);
			var cls = ClassLoader.FindTypeByAttribute<PlatformImplAttribute>((t, a) => a.OS == os && type.IsAssignableFrom(t));
			if (cls == null)
				throw new TypeLoadException("Could not find an instance of '" + type.FullName + "' for the " + os + " platform.");
			return cls;
		}

		public static T CreateInstance(params object[] args)
		{
			object obj = Activator.CreateInstance(impl, args);
			T ret = obj as T;
			return ret;
		}
	}

	public class StaticPlatformFactory<T> where T : class
	{
		public static T Instance { get { return instance; } }

		private static T instance = LoadInstance();

		private static T LoadInstance()
		{
			Platform.OS os = Platform.GetOS();
			Type type = typeof(T);
			var cls = ClassLoader.FindTypeByAttribute<PlatformImplAttribute>((t, a) => a.OS == os && t.GetInterfaces().Contains(type));
			if (cls == null)
				throw new TypeLoadException("Could not find an instance of '" + type.FullName + "' for the " + os + " platform.");
			object obj = Activator.CreateInstance(cls);
			T ret = obj as T;
			return ret;
		}
	}

	/// <summary>
	/// Helper class to determine the OS/Platform we are on.  The built in 
	/// method returns incorrect results.
	/// </summary>
	public static class Platform
	{
		[Flags]
		public enum OS
		{
			[XmlEnum("none")]
			None = 0,
			[XmlEnum("windows")]
			Windows = 1,
			[XmlEnum("osx")]
			OSX = 2,
			[XmlEnum("linux")]
			Linux = 4,
			[XmlEnum("unix")]
			Unix = 6,
			[XmlEnum("all")]
			All = 7
		};

		public readonly static Version MonoRuntimeVersion = GetMonoVersion();

		public enum Architecture { x64, x86 };

		public static Architecture GetArch()
		{
			return IntPtr.Size == 8 ? Architecture.x64 : Architecture.x86;
		}

		public static OS GetOS()
		{
			return _os;
		}

		static OS _os = _GetOS();

		static OS _GetOS()
		{
			if (System.IO.Path.DirectorySeparatorChar == '\\')
				return OS.Windows;
			if (IsRunningOnMac())
				return OS.OSX;
			if (Environment.OSVersion.Platform == PlatformID.Unix)
				return OS.Linux;
			return OS.None;
		}

		[DllImport("libc")]
		static extern int uname(IntPtr buf);

		//From Managed.Windows.Forms/XplatUI
		static bool IsRunningOnMac()
		{
			IntPtr buf = IntPtr.Zero;
			try
			{
				buf = Marshal.AllocHGlobal(8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname(buf) == 0)
				{
					string os = Marshal.PtrToStringAnsi(buf);
					if (os == "Darwin") return true;
				}
			}
			finally
			{
				if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
			}
			return false;
		}

		public static bool IsRunningOnMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}

		private static Version GetMonoVersion()
		{
			var type = Type.GetType("Mono.Runtime");

			// If we are not on mono, no checks need to be performed.
			if (type == null)
				return null;

			var mi = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

			if (mi == null)
				return null;
			var str = mi.Invoke(null, null) as string;

			if (str == null)
				return null;

			return ParseMonoVersion(str);
		}

		private static Version ParseMonoVersion(string str)
		{
			// Example version string:
			// 3.2.8 (Debian 3.2.8+dfsg-4ubuntu1)

			var idx = str.IndexOf(' ');
			if (idx < 0)
				return null;

			var part = str.Substring(0, idx);

			Version ret;
			Version.TryParse(part, out ret);

			return ret;
		}
	}
}
