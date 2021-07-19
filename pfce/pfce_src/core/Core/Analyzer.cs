


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Xml;

using Peach.Core.Dom;
using Peach.Core.Cracker;

namespace Peach.Core
{
	[Serializable]
	public abstract class Analyzer : IPitSerializable
	{
		public static readonly bool supportParser = false;
		public static readonly bool supportDataElement = false;
		public static readonly bool supportCommandLine = false;
		public static readonly bool supportTopLevel = false;

		public static Analyzer defaultParser = null;

		static Analyzer()
		{
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (!type.IsAbstract && type.IsClass &&
					type.IsPublic && type.IsSubclassOf(typeof(Analyzer)))
				{
					// Found an Analyzer!

					type.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
				}
			}
		}

		/// <summary>
		/// Replaces the parser for fuzzer definition.
		/// </summary>
		/// <param name="args">Command line arguments</param>
		/// <param name="fileName">File to parse</param>
		public virtual Dom.Dom asParser(Dictionary<string, object> args, string fileName)
		{
			try
			{
				using (Stream fin = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					Dom.Dom ret = asParser(args, fin);
					ret.fileName = fileName;
					return ret;
				}
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				throw new PeachException("Error, " + fileNotFoundException.Message, fileNotFoundException);
			}
			catch (PathTooLongException pathTooLongException)
			{
				throw new PeachException("Error, " + pathTooLongException.Message, pathTooLongException);
			}
			catch (DirectoryNotFoundException directoryNotFoundException)
			{
				throw new PeachException("Error, " + directoryNotFoundException.Message, directoryNotFoundException);
			}
			catch (UnauthorizedAccessException unauthorizedAccessException)
			{
				throw new PeachException("Error, " + unauthorizedAccessException.Message, unauthorizedAccessException);
			}
			catch (NotSupportedException notSupportedException)
			{
				throw new PeachException("Error, " + notSupportedException.Message, notSupportedException);
			}
		}

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Analyzer");

			foreach (var attrib in this.GetType().GetAttributes<AnalyzerAttribute>(null))
			{
				if (attrib.IsDefault)
					pit.WriteAttributeString("class", attrib.Name);
			}

			foreach (var param in this.GetType().GetAttributes<ParameterAttribute>(null))
			{
				var prop = this.GetType().GetProperty(param.name);
				if (prop == null)
					continue;

				var objValue = prop.GetValue(this, null);
				if (objValue == null)
					continue;

				var value = objValue.ToString();

				pit.WriteStartElement("Param");
				pit.WriteAttributeString("name", param.name);
				pit.WriteAttributeString("value", value);
			}

			pit.WriteEndElement();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public virtual Dom.Dom asParser(Dictionary<string, object> args, Stream data)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called to perform validation pass.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <param name="fileName">Filename to test</param>
		/// <returns>Throws PeachException on error.</returns>
		public virtual void asParserValidation(Dictionary<string, object> args, string fileName)
		{
			try
			{
				asParserValidation(args, File.OpenRead(fileName));
			}
			catch (PeachException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, " + ex.Message, ex);
			}
		}

		public virtual void asParserValidation(Dictionary<string, object> args, Stream data)
		{
			throw new NotImplementedException();
		}

		public virtual void asDataElement(DataElement parent, Dictionary<DataElement, Position> positions)
		{
			throw new NotImplementedException();
		}

		public virtual void asCommandLine(List<string> args)
		{
			throw new NotImplementedException();
		}

		public virtual void asTopLevel(Dom.Dom dom, Dictionary<string, string> args)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Used to indicate a class is a valid Publisher and 
	/// provide it's invoking name used in the Pit XML file.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class AnalyzerAttribute : PluginAttribute
	{
		public AnalyzerAttribute(string name, bool isDefault = false)
			: base(typeof(Analyzer), name, isDefault)
		{
		}
	}
}

// end
