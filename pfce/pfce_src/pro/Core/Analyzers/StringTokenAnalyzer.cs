

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Runtime;
using Encoding = Peach.Core.Encoding;

namespace Peach.Pro.Core.Analyzers
{
	[Analyzer("StringToken", true)]
	[Analyzer("StringTokenAnalyzer")]
	[Analyzer("stringtoken.StringTokenAnalyzer")]
	[Usage("<infile> <outfile>")]
	[Description("Generate a data model by tokenizing a text document.")]
	[Parameter("Tokens", typeof(string), "List of character tokens", StringTokenAnalyzer.TOKENS)]
	[Serializable]
	public class StringTokenAnalyzer : Analyzer
	{
		/// <summary>
		/// Default token set.  Order is important!
		/// </summary>
		public const string TOKENS = "\r\n\"'[]{}<>` \t.,~!@#$%^?&*_=+-|\\:;/";

		protected string tokens = TOKENS;
		protected Dictionary<string, Variant> args = null;
		protected StringType encodingType = StringType.ascii;
		protected Encoding encoding = null;
		protected Dictionary<DataElement, Position> positions = null;

		public new static readonly bool supportParser = false;
		public new static readonly bool supportDataElement = true;
		public new static readonly bool supportCommandLine = true;
		public new static readonly bool supportTopLevel = false;

		public StringTokenAnalyzer()
		{
		}

		public StringTokenAnalyzer(Dictionary<string, Variant> args)
		{
			this.args = args;
		}

		public override void asCommandLine(List<string> args)
		{
			if (args.Count != 2)
				throw new SyntaxException("Missing required arguments.");

			var inFile = args[0];
			var outFile = args[1];
			var data = new BitStream(File.ReadAllBytes(inFile));
			var model = new DataModel(Path.GetFileName(inFile).Replace(".", "_"));

			model.Add(new Peach.Core.Dom.String());
			model[0].DefaultValue = new Variant(data);

			asDataElement(model[0], null);

			var settings = new XmlWriterSettings();
			settings.Encoding = System.Text.UTF8Encoding.UTF8;
			settings.Indent = true;

			using (var sout = new FileStream(outFile, FileMode.Create))
			using (var xml = XmlWriter.Create(sout, settings))
			{
				xml.WriteStartDocument();
				xml.WriteStartElement("Peach");

				model.WritePit(xml);

				xml.WriteEndElement();
				xml.WriteEndDocument();
			}
		}

		public override void asDataElement(DataElement parent, Dictionary<DataElement, Position> positions)
		{
			if (args != null && args.ContainsKey("Tokens"))
				tokens = (string)args["Tokens"];

			if (!(parent is Peach.Core.Dom.String))
				throw new PeachException("Error, StringToken analyzer only operates on String elements!");

			var str = parent as Peach.Core.Dom.String;
			encodingType = str.stringType;
			encoding = Encoding.GetEncoding(encodingType.ToString());

			// Are our tokens present in this string?
			var val = (string)str.InternalValue;
			if (!val.Any(c => tokens.IndexOf(c) > -1))
				return;

			try
			{
				this.positions = positions;

				var block = new Block(str.Name);
				str.parent[str.Name] = block;
				var tokenTree = TokenTree.Parse(val, tokens.ToCharArray());
				block.Add(tokenTree.Eval(parent.Name, positions));

				if (positions != null) 
				{
					var end = str.Value.LengthBits;
					positions[block] = new Position(0, end);
					positions[str] = new Position(0, end);
				}
			}
			finally
			{
				this.positions = null;
			}
		}
	}

	/// <summary>
	/// Tree representing the hierarchical structure of a string with token characters.
	/// 
	/// Designed to avoid costly intermediary string and DataElement allocations, by only 
	/// allocating the ones that are strictly needed.
	/// </summary>
	internal abstract class TokenTree {
		internal readonly Position _position;

		internal TokenTree(Position position) {
			_position = position;
		}

		protected internal abstract DataElement DoEval(string name, Dictionary<DataElement, Peach.Core.Cracker.Position> positions);

		/// <summary>
		/// Evaluate the tree, converting it into a Peach DataElement representation.
		/// 
		/// Tracks the bit positions of every allocated DataElement.
		/// </summary>
		/// <param name="name">Name of the DataElement.</param>
		/// <param name="positions">Positions of the returned DataElement and the DataElements it contains.</param>
		internal DataElement Eval(string name, Dictionary<DataElement, Peach.Core.Cracker.Position> positions) {
			var element = this.DoEval(name, positions);
			if (positions != null)
			{
				positions[element] = this._position;
			}
			return element;
		}

		/// <summary>
		/// Recursively build a tree by splitting on every token.
		/// 
		/// To avoid exponentially allocating strings, new strings are only allocated for 
		/// substrings with no tokens (at the leaves).
		/// </summary>
		/// <param name="str">String to parse into a tree.</param>
		/// <param name="tokens">Array of tokens to split on</param>
		/// <param name="tokenIndex">Current token index. Starts at 0 and increases.</param>
		/// <param name="start">(Exclusive) start index of str include in parse.</param>
		/// <param name="end">(Exclusive) end index of str to include in parse.</param>
		internal static TokenTree Parse(string str, char[] tokens, int tokenIndex=0, int start=-1, int end=-1)
		{
			if (end < 0)
			{
				end = str.Length;
			}

			var position = new Position(start < 0 ? 0 : (start + 1) * 8, end * 8);

			while (true)
			{
				if (tokenIndex >= tokens.Length)
				{
					var count = end - start - 1;
					return new TokenLeaf(str.Substring(start + 1, count), position);
				}

				var matchIndex = str.IndexOf(tokens[tokenIndex], start + 1);
				if (matchIndex >= 0 && matchIndex < end)
				{
					return new TokenBranch(
						TokenTree.Parse(str, tokens, tokenIndex, start, matchIndex),
						tokens[tokenIndex],
						TokenTree.Parse(str, tokens, tokenIndex, matchIndex, end),
						position);
				}
				else
				{
					// skip tokens that aren't in str
					tokenIndex++;
				}
			}
		}
	}

	/// <summary>
	/// Node of a TokenTree representing a string with no tokens.
	/// </summary>
	sealed internal class TokenLeaf : TokenTree {
		readonly string _string;

		internal TokenLeaf(string str, Position position)
			: base(position)
		{
			_string = str;
		}

		protected internal override DataElement DoEval(string name, Dictionary<DataElement, Position> positions) {
			return new Peach.Core.Dom.String(name)
			{
				DefaultValue = new Variant(_string)
			};
		}
	}

	/// <summary>
	/// Node of a TokenTree representing a token and the TokenTrees before and after that token.
	/// </summary>
	sealed internal class TokenBranch : TokenTree {
		readonly TokenTree _pre;
		readonly TokenTree _token;
		readonly TokenTree _post;

		internal TokenBranch(TokenTree pre, char token, TokenTree post, Position position)
			: base(position)
		{
			_pre = pre;
			_token = new TokenLeaf(token.ToString(), new Position(pre._position.end, pre._position.end + 8));
			_post = post;
		}

		protected internal override DataElement DoEval(string name, Dictionary<DataElement, Position> positions) {
			var block = new Peach.Core.Dom.Block(name);
			block.Add(_pre.Eval("Pre", positions));
			block.Add(_token.Eval("Token", positions));
			block.Add(_post.Eval("Post", positions));
			return block;
		}
	}
}

// end
