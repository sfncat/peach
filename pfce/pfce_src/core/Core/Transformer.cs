


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.Security.Cryptography;
using System.IO;
using System.Xml;

namespace Peach.Core
{
	/// <summary>
	/// Transformers perform static transforms of data.
	/// </summary>
	[Serializable]
	public abstract class Transformer : IOwned<DataElement>, IPitSerializable
	{
		public DataElement parent { get; set; }

		public Transformer anotherTransformer;

		public Transformer(DataElement parent, Dictionary<string, Variant> args)
		{
			this.parent = parent;
		}

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Transformer");

			foreach (var attrib in this.GetType().GetAttributes<TransformerAttribute>(null))
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
				if(objValue == null)
					continue;

				pit.WriteStartElement("Param");
				pit.WriteAttributeString("name", param.name);
				pit.WriteAttributeString("value", objValue.ToString());
			}

			if (anotherTransformer != null)
				anotherTransformer.WritePit(pit);

			pit.WriteEndElement();
		}


		/// <summary>
		/// Encode data, will properly call any chained transformers.
		/// </summary>
		/// <param name="data">Data to encode</param>
		/// <returns>Returns encoded value or null if encoding is not supported.</returns>
		public virtual BitwiseStream encode(BitwiseStream data)
		{
			data.Seek(0, System.IO.SeekOrigin.Begin);

			data = internalEncode(data);

			if (anotherTransformer != null)
				return anotherTransformer.encode(data);

			return data;
		}

		/// <summary>
		/// Decode data, will properly call any chained transformers.
		/// </summary>
		/// <param name="data">Data to decode</param>
		/// <returns>Returns decoded value or null if decoding is not supported.</returns>
		public virtual BitStream decode(BitStream data)
		{
			if (anotherTransformer != null)
				data = anotherTransformer.decode(data);

			data.Seek(0, System.IO.SeekOrigin.Begin);

			return internalDecode(data);
		}

		/// <summary>
		/// Implement to perform actual encoding of 
		/// data.
		/// </summary>
		/// <param name="data">Data to encode</param>
		/// <returns>Returns encoded data</returns>
		protected abstract BitwiseStream internalEncode(BitwiseStream data);

		/// <summary>
		/// Implement to perform actual decoding of
		/// data.
		/// </summary>
		/// <param name="data">Data to decode</param>
		/// <returns>Returns decoded data</returns>
		protected abstract BitStream internalDecode(BitStream data);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="transform"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		protected static BitStream CryptoStream(BitwiseStream stream, ICryptoTransform transform, CryptoStreamMode mode)
		{
			BitStream ret = new BitStream();

			if (mode == CryptoStreamMode.Write)
			{
				var cs = new CryptoStream(ret, transform, mode);
				stream.CopyTo(cs);
				cs.FlushFinalBlock();
			}
			else
			{
				var cs = new CryptoStream(stream, transform, mode);
				cs.CopyTo(ret);
			}

			if (stream.Position != stream.Length)
				throw new PeachException("Didn't transform all bytes.");

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}

	/// <summary>
	/// Use this attribute to identify Transformers
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class TransformerAttribute : PluginAttribute
	{
		public TransformerAttribute(string name, bool isDefault = false)
			: base(typeof(Transformer), name, isDefault)
		{
		}
	}
}

// end
