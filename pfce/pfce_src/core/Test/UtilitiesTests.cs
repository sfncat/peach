using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Peach.Core.Agent;

namespace Peach.Core.Test
{
	[TestFixture]
	[Peach]
	[Quick]
	class UtilitiesTests
	{
		[Test]
		public void TestBadSlices()
		{
			// begin > end
			Assert.Throws<ArgumentOutOfRangeException>(delegate()
			{
				try
				{
					Utilities.SliceRange(1, 0, 1, 10);
				}
				catch (Exception ex)
				{
					Assert.True(ex.Message.Contains("Parameter name: begin"));
					throw;
				}
			});

			// curSlice > numSlices
			Assert.Throws<ArgumentOutOfRangeException>(delegate()
			{
				try
				{
					Utilities.SliceRange(0, 10, 5, 4);
				}
				catch (Exception ex)
				{
					Assert.True(ex.Message.Contains("Parameter name: curSlice"));
					throw;
				}
			});

			// curSlice = 0
			Assert.Throws<ArgumentOutOfRangeException>(delegate()
			{
				try
				{
					Utilities.SliceRange(0, 10, 0, 4);
				}
				catch (Exception ex)
				{
					Assert.True(ex.Message.Contains("Parameter name: curSlice"));
					throw;
				}
			});

			// numSlices > (end - begin + 1)
			Assert.Throws<ArgumentOutOfRangeException>(delegate()
			{
				try
				{
					Utilities.SliceRange(0, 10, 1, 14);
				}
				catch (Exception ex)
				{
					Assert.True(ex.Message.Contains("Parameter name: numSlices"));
					throw;
				}
			});
		}

		[Test]
		public void TestGoodSlices()
		{
			Tuple<uint, uint> ret;

			for (uint i = 1; i <= 10; ++i)
			{
				ret = Utilities.SliceRange(1, 10, i, 10);
				Assert.AreEqual(i, ret.Item1);
				Assert.AreEqual(i, ret.Item2);
			}

			ret = Utilities.SliceRange(1, 10, 1, 1);
			Assert.AreEqual(1, ret.Item1);
			Assert.AreEqual(10, ret.Item2);

			ret = Utilities.SliceRange(1, 10, 1, 2);
			Assert.AreEqual(1, ret.Item1);
			Assert.AreEqual(5, ret.Item2);

			ret = Utilities.SliceRange(1, 10, 2, 2);
			Assert.AreEqual(6, ret.Item1);
			Assert.AreEqual(10, ret.Item2);

			for (uint i = 1; i <= 9; ++i)
			{
				ret = Utilities.SliceRange(1, 10, i, 9);

				if (i == 9)
				{
					Assert.AreEqual(9, ret.Item1);
					Assert.AreEqual(10, ret.Item2);
				}
				else
				{
					Assert.AreEqual(i, ret.Item1);
					Assert.AreEqual(i, ret.Item2);
				}
			}

			ret = Utilities.SliceRange(1, uint.MaxValue, 1, 1);
			Assert.AreEqual(1, ret.Item1);
			Assert.AreEqual(uint.MaxValue, ret.Item2);

			ret = Utilities.SliceRange(1, uint.MaxValue, 1, 2);
			Assert.AreEqual(1, ret.Item1);
			Assert.AreEqual(uint.MaxValue / 2, ret.Item2);

			ret = Utilities.SliceRange(1, uint.MaxValue, 2, 2);
			Assert.AreEqual(uint.MaxValue / 2 + 1, ret.Item1);
			Assert.AreEqual(uint.MaxValue, ret.Item2);

			ret = Utilities.SliceRange(1, 5907588, 1, 38);
			Assert.AreEqual(1, ret.Item1);
			Assert.AreEqual(155462, ret.Item2);
			ret = Utilities.SliceRange(1, 5907588, 2, 38);
			Assert.AreEqual(155463, ret.Item1);
			Assert.AreEqual(310924, ret.Item2);
			ret = Utilities.SliceRange(1, 5907588, 37, 38);
			Assert.AreEqual(5596633, ret.Item1);
			Assert.AreEqual(5752094, ret.Item2);
			ret = Utilities.SliceRange(1, 5907588, 38, 38);
			Assert.AreEqual(5752095, ret.Item1);
			Assert.AreEqual(5907588, ret.Item2);
		}

		[Test]
		public void TestHexDump()
		{
			var output = new MemoryStream();
			var ms = new MemoryStream(Encoding.ASCII.GetBytes("0Hello World"));
			ms.Position = 1;
			Utilities.HexDump(ms, output);
			Assert.AreEqual(1, ms.Position);
			Assert.AreEqual(output.Position, output.Length);
			output.Seek(0, SeekOrigin.Begin);
			var str = Encoding.ASCII.GetString(output.GetBuffer(), 0, (int)output.Length);
			string expected = "00000000   48 65 6C 6C 6F 20 57 6F  72 6C 64                  Hello World     " + Environment.NewLine;
			Assert.AreEqual(expected, str);

			str = Utilities.HexDump(ms);
			Assert.AreEqual(1, ms.Position);
			Assert.AreEqual(expected, str);
		}

		[Test]
		public void TestHexDumpWithNonZeroStartAddress()
		{
			var output = new MemoryStream();
			var ms = new MemoryStream(Encoding.ASCII.GetBytes("0Hello World Hello World Hello World"));
			ms.Position = 1;
			Utilities.HexDump(ms, output, startAddress: 0x5555);
			Assert.AreEqual(1, ms.Position);
			Assert.AreEqual(output.Position, output.Length);
			output.Seek(0, SeekOrigin.Begin);
			var str = Encoding.ASCII.GetString(output.GetBuffer(), 0, (int)output.Length);
			string expected = string.Join(Environment.NewLine, new [] {
				"00005555   48 65 6C 6C 6F 20 57 6F  72 6C 64 20 48 65 6C 6C   Hello World Hell",
				"00005565   6F 20 57 6F 72 6C 64 20  48 65 6C 6C 6F 20 57 6F   o World Hello Wo",
				"00005575   72 6C 64                                           rld             ",
				"",
			});
			Assert.AreEqual(expected, str);

			str = Utilities.HexDump(ms, startAddress: 0x5555);
			Assert.AreEqual(1, ms.Position);
			Assert.AreEqual(expected, str);
		}

		[Test]
		public void TestMulticast()
		{
			Assert.False(System.Net.IPAddress.Any.IsMulticast());
			Assert.False(System.Net.IPAddress.Loopback.IsMulticast());
			Assert.False(System.Net.IPAddress.Broadcast.IsMulticast());

			Assert.True(System.Net.IPAddress.Parse("224.0.0.1").IsMulticast());
			Assert.True(System.Net.IPAddress.Parse("239.255.255.255").IsMulticast());
		}

		[Test]
		public void TestHexDumpTruncate()
		{
			var ms = new MemoryStream();
			for (var i = 0; i < 32; ++i)
				ms.WriteByte((byte)i);

			ms.Seek(0, SeekOrigin.Begin);

			var str = Utilities.HexDump(ms, 16, 24);
			var expected = string.Join(Environment.NewLine, new[] {
				"00000000   00 01 02 03 04 05 06 07  08 09 0A 0B 0C 0D 0E 0F   ................",
				"00000010   10 11 12 13 14 15 16 17                            ........        ",
				"---- TRUNCATED (Total Length: 32 bytes) ----",
			});

			Assert.That(str, Is.EqualTo(expected));
		}

		[Test]
		public void TestHash()
		{
			Assert.AreEqual("D41D8CD9", Monitor2.Hash(""));
			Assert.AreEqual("ACBD18DB", Monitor2.Hash("foo"));
			Assert.AreEqual("6DF23DC0", Monitor2.Hash("foobarbaz"));
		}

		[Test]
		public void TestCopyright()
		{
			var str = Assembly.GetExecutingAssembly().GetCopyright();
			StringAssert.IsMatch("Copyright \\(c\\) (\\d+ )?Peach Fuzzer, LLC", str);
		}
	}
}
