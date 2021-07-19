using System;

namespace Peach.Core
{
	public interface IFieldNamed
	{
		string Name { get; }

		string FieldId { get; }
	}
}
