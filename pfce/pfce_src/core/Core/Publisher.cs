


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Peach.Core.Dom;
using Peach.Core.IO;
using Action = System.Action;

namespace Peach.Core
{
	/// <summary>
	/// Publishers are I/O interfaces called by actions in the state model. They
	/// perform stream or call based interactions with external entities.
	/// </summary>
	/// <remarks>
	/// Publishers are I/O interfaces for Peach.  They glue the actions
	/// in a state model to the target interface.  Publishers can be 
	/// stream based such as files or sockets, and also call based like
	/// COM and shared libraries.  They can also be hybrids using both
	/// stream and call based methods to make more complex publishers.
	/// 
	/// Multiple publishers can be used in a single state model to allow
	/// for more complex operations such as writing to the registry and
	/// then calling an RPC method.
	/// 
	/// Custom publishers should implement the methods prefixed with "On"
	/// such as OnStart, OnOpen and OnInput. Additionally they should
	/// the Stream interface as needed if they will support the
	/// input action.
	/// </remarks>
	public abstract class Publisher : Stream, INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		protected abstract NLog.Logger Logger { get; }

		#region Private Members

		private bool _hasStarted;
		private bool _isOpen;

		private void WrapFault(Action fn)
		{
			WrapFault(() =>
			{
				fn();
				return (object)null;
			});
		}

		protected T WrapFault<T>(Func<T> fn)
		{
			try
			{
				return fn();
			}
			catch (FaultException ex)
			{
				if (string.IsNullOrEmpty(ex.Fault.DetectionSource))
					ex.Fault.DetectionSource = GetType().GetAttributes<PublisherAttribute>().First().Name;

				if (string.IsNullOrEmpty(ex.Fault.DetectionName))
					ex.Fault.DetectionName = Name;

				throw;
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// The name of the publisher.
		/// </summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// Gets/sets the current fuzzing iteration.
		/// </summary>
		public uint Iteration
		{
			get;
			set;
		}

		/// <summary>
		/// Gets/sets if the current iteration is a control iteration.
		/// </summary>
		public bool IsControlIteration
		{
			get;
			set;
		}

		/// <summary>
		/// Is iteration after a fault has occured?
		/// </summary>
		public bool IsIterationAfterFault
		{
			get;
			set;
		}

		/// <summary>
		/// Is control record iteration?
		/// </summary>
		public bool IsControlRecordingIteration { get; set; }

		#endregion

		#region Implementation Functions

		/// <summary>
		/// Called when the publisher is started.  This method will be called
		/// once per fuzzing "Session", not on every iteration.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// 
		/// For Publishers that will listen for incoming connections, this is a good
		/// place to bind to a port, then perform the blocking accept in the OnAccept
		/// method.
		/// </remarks>
		/// <seealso cref="OnStop"/>
		protected virtual void OnStart()
		{
		}

		/// <summary>
		/// Called when the publisher is stopped.  This method will be called
		/// once per fuzzing "Session", not on every iteration.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		/// <seealso cref="OnStart"/>
		protected virtual void OnStop()
		{
		}

		/// <summary>
		/// Open or connect to a resource.  Will be called
		/// automatically if not called specifically.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// 
		/// OnOpen will automatically get called if an input or output action
		/// is performed from the state model. In the case where only a call
		/// action is performed, there is no automatic open or close.
		/// </remarks>
		/// <seealso cref="OnClose"/>
		protected virtual void OnOpen()
		{
		}

		/// <summary>
		/// Close a resource.  Will be called automatically when
		/// state model exists.  Can also be called explicitly when
		/// needed.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// 
		/// This method will not be called unless OnOpen has also been called.
		/// OnOpen will automatically get called if an input or output action
		/// is performed from the state model. In the case where only a call
		/// action is performed, there is no automatic open or close.
		/// </remarks>
		/// <seealso cref="OnOpen"/>
		protected virtual void OnClose()
		{
		}

		/// <summary>
		/// Accept an incoming connection.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		protected virtual void OnAccept()
		{
			throw new PeachException("Error, action 'accept' not supported by publisher");
		}

		/// <summary>
		/// Call a method on the Publishers resource using data models.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		/// <param name="method">Name of method to call</param>
		/// <param name="args">Arguments to pass</param>
		/// <returns>Returns resulting data</returns>
		protected virtual Variant OnCall(string method, List<ActionParameter> args)
		{
			return call(method, args.Select(AsBitwiseStream).ToList());
		}

		/// <summary>
		/// Call a method on the Publishers resource using data model values.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		/// <param name="method">Name of method to call</param>
		/// <param name="args">Arguments to pass</param>
		/// <returns>Returns resulting data</returns>
		protected virtual Variant OnCall(string method, List<BitwiseStream> args)
		{
			throw new PeachException("Error, action 'call' not supported by publisher");
		}

		/// <summary>
		/// Set a property on the Publishers resource.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		/// <seealso cref="setProperty" />
		/// <seealso cref="OnGetProperty" />
		/// <param name="property">Name of property to set</param>
		/// <param name="value">Value to set on property</param>
		protected virtual void OnSetProperty(string property, Variant value)
		{
			throw new PeachException("Error, action setProperty='{0}' not supported by publisher".Fmt(property));
		}

		/// <summary>
		/// Get value of a property exposed by Publishers resource
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		/// <seealso cref="getProperty" />
		/// <seealso cref="OnSetProperty" />
		/// <param name="property">Name of property</param>
		/// <returns>Returns value of property</returns>
		protected virtual Variant OnGetProperty(string property)
		{
			throw new PeachException("Error, action getProperty='{0}' not supported by publisher".Fmt(property));
		}

		/// <summary>
		/// Send data
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		/// <seealso cref="OnInput"/>
		/// <param name="dataModel">Data to send/write</param>
		protected virtual void OnOutput(DataModel dataModel)
		{
			output(dataModel.Value);
		}

		/// <summary>
		/// Send data
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// </remarks>
		/// <seealso cref="OnInput"/>
		/// <param name="data">Data to send/write</param>
		protected virtual void OnOutput(BitwiseStream data)
		{
			throw new PeachException("Error, action 'output' not supported by publisher");
		}

		/// <summary>
		/// Read data
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// 
		/// The following chain of method calls will occour when an input action is performed:
		/// 
		/// <list type="number">
		///		<item>
		///			OnInput() is called when an input action occurs. For publishers that can read a complete
		///			peice of data with out any indication of how much data must be read can do so at this time.
		///			An example of this is UDP based protocols in which an entire packet can be read. Typically
		///			stream based publishers cannot read data at this time and should instead implement
		///			WantBytes.
		///		</item>
		///		<item>
		///			Cracking of input data model will commence resulting in the following method calls:
		///			
		///			<list type="number">
		///				<item>WantBytes(count) is called when the cracker needs additional data. Custom publishers should perform blocking reads here.</item>
		///				<item>read() is called on the stream interface as needed to read from buffered data.</item>
		///				<item>These steps repeat until cracking has completed or no more data can be read.</item>
		///			</list>
		///		</item>
		///		<item>Input action has completed.</item>
		/// </list>
		/// </remarks>
		/// <seealso cref="OnOutput(BitwiseStream)"/>
		protected virtual void OnInput()
		{
			throw new PeachException("Error, action 'input' not supported by publisher");
		}

		#endregion

		#region Ctor

		protected Publisher(Dictionary<string, Variant> args)
		{
			ParameterParser.Parse(this, args);
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Called to Start publisher. To implement override the OnStart method.
		/// </summary>
		/// <remarks>
		/// This action is always performed
		/// even if not specifically called.  This method will be called
		/// once per fuzzing "Session", not on every iteration.
		/// 
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnStart method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnStart"/>
		public void start()
		{
			if (_hasStarted)
				return;

			Logger.Debug("start()");
			OnStart();

			_hasStarted = true;
		}

		/// <summary>
		/// Called to Stop publisher.  This action is always performed
		/// even if not specifically called.  This method will be called
		/// once per fuzzing "Session", not on every iteration.
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnStop method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnStop"/>
		public void stop()
		{
			if (!_hasStarted)
				return;

			Logger.Debug("stop()");
			OnStop();

			_hasStarted = false;
		}

		/// <summary>
		/// Accept an incoming connection.
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnAccept method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnAccept"/>
		public void accept()
		{
			Logger.Debug("accept()");
			WrapFault(OnAccept);
		}

		/// <summary>
		/// Open or connect to a resource.  Will be called
		/// automatically if not called specifically.
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnOpen method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnOpen"/>
		/// <seealso cref="close"/>
		public void open()
		{
			if (_isOpen)
				return;

			Logger.Debug("open()");
			WrapFault(OnOpen);

			_isOpen = true;
		}

		/// <summary>
		/// Close a resource.  Will be called automatically when
		/// state model exists.  Can also be called explicitly when
		/// needed.
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnClose method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnClose"/>
		/// <seealso cref="open"/>
		public void close()
		{
			if (!_isOpen)
				return;

			Logger.Debug("close()");
			WrapFault(OnClose);

			_isOpen = false;
		}

		/// <summary>
		/// Call a method on the Publishers resource
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnCall method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="!:Peach.Core.Publisher.OnCall(string, System.Collections.Generic.List`Peach.Core.IO.BitwiseStream)"/>
		/// <param name="method">Name of method to call</param>
		/// <param name="args">Arguments to pass</param>
		/// <returns>Returns resulting data</returns>
		public Variant call(string method, List<BitwiseStream> args)
		{
			Logger.Debug("call({0}) BitwiseStream Count: {1}", method, args.Count);
			return WrapFault(() => OnCall(method, args));
		}

		/// <summary>
		/// Set a property on the Publishers resource.
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnSetProperty method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnSetProperty"/>
		/// <seealso cref="getProperty"/>
		/// <param name="property">Name of property to set</param>
		/// <param name="value">Value to set on property</param>
		public void setProperty(string property, Variant value)
		{
			Logger.Debug("setProperty({0}, {1})", property, value);
			WrapFault(() => OnSetProperty(property, value));
		}

		/// <summary>
		/// Get value of a property exposed by Publishers resource
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnGetProperty method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnGetProperty"/>
		/// <seealso cref="setProperty"/>
		/// <param name="property">Name of property</param>
		/// <returns>Returns value of property</returns>
		public Variant getProperty(string property)
		{
			Logger.Debug("getProperty({0})", property);
			return WrapFault(() => OnGetProperty(property));
		}

		/// <summary>
		/// Send data
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnOutput method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnOutput(BitwiseStream)"/>
		/// <seealso cref="input"/>
		/// <param name="data">Data to send/write</param>
		public void output(BitwiseStream data)
		{
			data = data.PadBits();
			data.Seek(0, SeekOrigin.Begin);

			Logger.Debug("output({0} bytes)", data.Length);
			WrapFault(() => OnOutput(data));
		}

		/// <summary>
		/// Read data
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnInput method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnInput"/>
		/// <seealso cref="!:Peach.Core.Publisher.output(Peach.Core.IO.BitwiseStream)"/>
		public void input()
		{
			Logger.Debug("input()");
			WrapFault(OnInput);
		}

		/// <summary>
		/// Blocking stream based publishers override this to wait
		/// for a certian amount of bytes to be available for reading.
		/// </summary>
		/// <remarks>
		/// This method can be overriden by custom Publishers.
		/// 
		/// The following chain of method calls will occour when an input action is performed:
		/// 
		/// <list type="number">
		///		<item>
		///			OnInput() is called when an input action occurs. For publishers that can read a complete
		///			peice of data with out any indication of how much data must be read can do so at this time.
		///			An example of this is UDP based protocols in which an entire packet can be read. Typically
		///			stream based publishers cannot read data at this time and should instead implement
		///			WantBytes.
		///		</item>
		///		<item>
		///			Cracking of input data model will commence resulting in the following method calls:
		///			
		///			<list type="number">
		///				<item>WantBytes(count) is called when the cracker needs additional data. Custom publishers should perform blocking reads here.</item>
		///				<item>read() is called on the stream interface as needed to read from buffered data.</item>
		///				<item>These steps repeat until cracking has completed or no more data can be read.</item>
		///			</list>
		///		</item>
		///		<item>Input action has completed.</item>
		/// </list>
		/// </remarks>
		/// <param name="count">The requested byte count</param>
		public virtual void WantBytes(long count)
		{
		}

		/// <summary>
		/// Send data model
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnOutput method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="OnOutput(DataModel)"/>
		/// <param name="dataModel">DataModel to send/write</param>
		public void output(DataModel dataModel)
		{
			WrapFault(() => OnOutput(dataModel));
		}

		/// <summary>
		/// Call a method on the Publishers resource
		/// </summary>
		/// <remarks>
		/// This method is part of the base class. When implenting a custom Publisher 
		/// the OnCall method can be overriden to implement functionality that should
		/// occur when this method is called.
		/// </remarks>
		/// <seealso cref="!:Peach.Core.Publisher.OnCall(string, System.Collections.Generic.List`Peach.Core.Dom.ActionParameter)"/>
		/// <param name="method">Name of method to call</param>
		/// <param name="args">Arguments to pass</param>
		/// <returns>Returns resulting data</returns>
		public Variant call(string method, List<ActionParameter> args)
		{
			Logger.Debug("call({0}) ActionParameter Count: {1}", method, args.Count);
			return WrapFault(() => OnCall(method, args));
		}

		#endregion

		#region Stream

		/// <summary>
		/// Can stream be read from.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers should implement this property.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override bool CanRead { get { lock(_buffer) { return _buffer.CanRead; } } }
		/// </code>
		/// </remarks>
		public override bool CanRead
		{
			get { return false; }
		}

		/// <summary>
		/// Can seek in buffer.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers should implement this property.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override bool CanSeek { get { lock(_buffer) { return _buffer.CanSeek; } } }
		/// </code>
		/// </remarks>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>
		/// Can stream be written to.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers should implement this property.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override bool CanWrite { get { lock(_buffer) { return _buffer.CanWrite; } } }
		/// </code>
		/// </remarks>
		public override bool CanWrite
		{
			get { return false; }
		}

		/// <summary>
		/// Flush stream
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers should implement this method.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override void Flush { lock(_buffer) { _buffer.Flush(); } }
		/// </code>
		/// </remarks>
		public override void Flush()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Length of stream in bytes.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers should implement this property.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override long Length { get { lock(_buffer) { return _buffer.Length; } } }
		/// </code>
		/// </remarks>
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Current position in stream
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers should implement this property.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override long Position { get { lock(_buffer) { return _buffer.Position; } } }
		/// </code>
		/// </remarks>
		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Read data from stream.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers must implement this method.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override int Read(byte[] buffer, int offset, int count)
		/// {
		///		lock(_buffer)
		///		{
		///			return _buffer.Read(buffer, offset, count);
		///		}
		///	}
		/// </code>
		/// </remarks>
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Seek to position in stream.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers must implement this method.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override long Seek(long offset, SeekOrigin origin)
		/// {
		///		lock(_buffer)
		///		{
		///			return _buffer.Seek(offset, origin);
		///		}
		///	}
		/// </code>
		/// </remarks>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Set length of stream.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers can implement this method.
		/// 
		/// Typically this property is implemented by exposing the internal buffer's own stream operations.
		/// 
		/// <code>
		/// private MemoryStream _buffer;
		/// 
		/// public override int SetLength(long value)
		/// {
		///		lock(_buffer)
		///		{
		///			return _buffer.SetLength(value);
		///		}
		///	}
		/// </code>
		/// </remarks>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// This method is not currently used.
		/// </summary>
		/// <remarks>
		/// This method is part of the Stream interface that is implemented as part of the Publisher interface.
		/// Custom publishers should not implement this method.
		/// </remarks>
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		#endregion

		private static BitwiseStream AsBitwiseStream(ActionParameter param)
		{
			// Turn into a BitwiseStream where the name corresponds
			// to the name of the parameter.
			return new BitStreamList(new[] { param.dataModel.Value }) { Name = param.Name };
		}

		protected ushort UShortFromVariant(Variant value)
		{
			ushort ret = 0;

			if (value.GetVariantType() == Variant.VariantType.BitStream)
			{
				var bs = (BitwiseStream)value;
				bs.SeekBits(0, SeekOrigin.Begin);
				ulong bits;
				int len = bs.ReadBits(out bits, 16);
				ret = Endian.Little.GetUInt16(bits, len);
			}
			else if (value.GetVariantType() == Variant.VariantType.ByteString)
			{
				byte[] buf = (byte[])value;
				int len = Math.Min(buf.Length * 8, 16);
				ret = Endian.Little.GetUInt16(buf, len);
			}
			else
			{
				try
				{
					ret = ushort.Parse((string)value, CultureInfo.InvariantCulture);
				}
				catch
				{
					throw new SoftException("Can't convert to ushort, 'value' is an unsupported type.");
				}
			}

			return ret;
		}
	}


	/// <summary>
	/// Used to indicate a class is a valid Publisher and 
	/// provide it's invoking name used in the Pit XML file.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class PublisherAttribute : PluginAttribute
	{
		// ReSharper disable once UnusedParameter.Local
		[Obsolete("This constructor is obsolete. Use the constructor without the isDefault argument.")]
		public PublisherAttribute(string name, bool isDefault)
			: base(typeof(Publisher), name, true)
		{
		}

		public PublisherAttribute(string name)
			: base(typeof(Publisher), name, true)
		{
		}
	}
}

// END
