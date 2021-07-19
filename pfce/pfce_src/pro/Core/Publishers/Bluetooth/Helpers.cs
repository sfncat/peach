using System;
using System.Collections.ObjectModel;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public interface IUuid
	{
		string UUID { get; }
	}

	public interface IGattProperties : Properties
	{
		string InterfaceName { get; }
	}

	public class ByUuid<T> : KeyedCollection<Guid, T> where T : IUuid
	{
		public bool TryGetValue(Guid key, out T value)
		{
			if (Dictionary == null)
			{
				value = default(T);
				return false;
			}

			return Dictionary.TryGetValue(key, out value);
		}

		protected override Guid GetKeyForItem(T item)
		{
			return Guid.ParseExact(item.UUID, "D");
		}
	}
}
