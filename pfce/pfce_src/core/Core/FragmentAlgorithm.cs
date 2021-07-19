using System;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core
{
	/// <summary>
	/// Base class for fragmentation algorithms.  Fragmentation algorithms are used
	/// in conjunction with the Frag element. They contain the logic for generating
	/// fragments on output and reassembling fragments on input.
	/// </summary>
	/// <remarks>
	/// To add a new fragmnetaiton algorithm, create a Class Library project in Visual
	/// Sudio or Mono Develop. Create a new class that extends from this base class
	/// and implment the required abstract methods.  Build and place output into 
	/// the Plugins folder.
	/// 
	/// To debug simply change the debug settings of your project to run Peach.exe along
	/// with a sample pit. Such as: "peach.exe -1 --debug test.xml".
	/// </remarks>
	[Serializable]
	public abstract class FragmentAlgorithm
	{
		/// <summary>
		/// Set to Frag element instance associated with current instance of class.
		/// </summary>
		public Frag Parent;

		/// <summary>
		/// Perform fragmentation of payload
		/// </summary>
		/// <remarks>
		/// Perform fragmentation of payload, generating multiple copies of template, one per fragment.
		/// These fragments are added to the rendering sequence in the correct order.
		/// 
		/// The template is a container with a child of 'FragData'. The 'FragData' element receives
		/// the fragments data.
		/// 
		/// The template data element can be cloned using the 'template.Clone(name)' method.
		/// Example: <pre>var fragment = template.Clone("Frag_" + cnt);</pre> where cnt is the
		/// count of fragments.
		/// 
		/// Method should check FragmentIndexField, FragementOffsetField, FragmentLengthField
		/// and TotalLengthField and make correct updates to the template clones as needed.
		/// </remarks>
		/// <param name="template"></param>
		/// <param name="payload"></param>
		/// <param name="rendering"></param>
		public abstract void Fragment(DataElement template, DataElement payload, Sequence rendering);

		/// <summary>
		/// Check if all fragments are present in rendering sequence.
		/// </summary>
		/// <remarks>
		/// Used on infrag action to determine if all fragments have been received. Returning true will cause
		/// another input action to occur.
		/// </remarks>
		/// <returns>True if more fragments are needed. False if all fragments have been received.</returns>
		public abstract bool NeedFragment();

		/// <summary>
		/// Reassemble fragemnets into payload data
		/// </summary>
		/// <remarks>
		/// Prior to this method being called, NeedFragment must return False.
		/// 
		/// Make sure to reassemble the fragments in the correct oder.
		/// 
		/// The returned data stream will be used to crack the Payload.
		/// </remarks>
		/// <returns>BitStream instance containing reassembled payload data.</returns>
		public abstract BitStream Reassemble();
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class FragmentAlgorithmAttribute : PluginAttribute
	{
		public FragmentAlgorithmAttribute(string name)
			: base(typeof(FragmentAlgorithm), name, true)
		{
		}
	}
}

// end
