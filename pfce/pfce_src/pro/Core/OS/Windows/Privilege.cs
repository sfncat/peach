using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Peach.Pro.Core.OS.Windows
{
	public class Privilege : IDisposable
	{
		public static string SeDebugPrivilege = "SeDebugPrivilege";

		private IntPtr hToken;
		private readonly string name;

		public Privilege(string name)
		{
			this.name = name;

			IntPtr hThread = Interop.GetCurrentThread();

			if (!Interop.OpenThreadToken(hThread, Interop.TOKEN_ADJUST_PRIVILEGES | Interop.TOKEN_QUERY, false, out hToken))
			{
				int error = Marshal.GetLastWin32Error();

				if (error != Interop.ERROR_NO_TOKEN)
					throw new Win32Exception(error);

				if (!Interop.ImpersonateSelf(Interop.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation))
				{
					error = Marshal.GetLastWin32Error();
					throw new Win32Exception(error);
				}

				if (!Interop.OpenThreadToken(hThread, Interop.TOKEN_ADJUST_PRIVILEGES | Interop.TOKEN_QUERY, false, out hToken))
				{
					error = Marshal.GetLastWin32Error();
					throw new Win32Exception(error);
				}
			}

			if (!SetPrivilege(true))
			{
				int error = Marshal.GetLastWin32Error();
				Interop.CloseHandle(hToken);
				hToken = IntPtr.Zero;
				throw new Win32Exception(error);
			}
		}

		public static bool IsUserAdministrator()
		{
			bool isAdmin;
			try
			{
				//get the currently logged in user
				var user = WindowsIdentity.GetCurrent();
				var principal = new WindowsPrincipal(user);
				isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
			catch (UnauthorizedAccessException)
			{
				isAdmin = false;
			}
			catch (Exception)
			{
				isAdmin = false;
			}
			return isAdmin;
		}

		public void Dispose()
		{
			if (IntPtr.Zero != hToken)
			{
				SetPrivilege(false);
				Interop.CloseHandle(hToken);
				hToken = IntPtr.Zero;
			}
		}

		private bool SetPrivilege(bool bEnablePrivilege)
		{
			Interop.TOKEN_PRIVILEGES tp;
			Interop.LUID luid;

			if (!Interop.LookupPrivilegeValue(null, name, out luid))
				return false;

			tp.PrivilegeCount = 1;
			tp.Luid = luid;
			tp.Attributes = bEnablePrivilege ? Interop.SE_PRIVILEGE_ENABLED : 0;

			// Enable the privilege or disable all privileges.

			if (!Interop.AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
				return false;

			int err = Marshal.GetLastWin32Error();

			if (err == Interop.ERROR_NOT_ALL_ASSIGNED)
				return false;

			return true;
		}
	}
}
