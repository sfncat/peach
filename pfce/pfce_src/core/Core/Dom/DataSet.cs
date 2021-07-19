


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Specify a set of Data for a DataModel
	/// </summary>
	[Serializable]
	public class DataSet : List<Data>, INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		public string Name
		{
			get;
			set;
		}

		public string FieldId
		{
			get;
			set;
		}
	}
}

// END
