using System;
using System.Reflection;

namespace AsmVersion
{
	class Program
	{
		static void Main(string[] args)
		{
			foreach (var arg in args)
			{
				Console.WriteLine(Assembly.ReflectionOnlyLoadFrom(arg));
			}
		}
	}
}
