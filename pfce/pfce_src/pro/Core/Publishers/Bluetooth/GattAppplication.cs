using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NDesk.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class OrderedDict<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private class Collection : KeyedCollection<TKey, KeyValuePair<TKey, TValue>>
		{
			public bool ContainsKey(TKey key)
			{
				return Dictionary != null && Dictionary.ContainsKey(key);
			}

			protected override TKey GetKeyForItem(KeyValuePair<TKey, TValue> item)
			{
				return item.Key;
			}
		}

		private readonly Collection _items = new Collection();

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			_items.Add(item);
		}

		public void Clear()
		{
			_items.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return _items.Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			_items.CopyTo(array, arrayIndex);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return _items.Remove(item);
		}

		public int Count
		{
			get { return _items.Count; }
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool ContainsKey(TKey key)
		{
			return _items.ContainsKey(key);
		}

		public void Add(TKey key, TValue value)
		{
			_items.Add(new KeyValuePair<TKey, TValue>(key, value));
		}

		public bool Remove(TKey key)
		{
			return _items.Remove(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			throw new NotImplementedException();
		}

		public TValue this[TKey key]
		{
			get
			{
				return _items[key].Value;
			}
			set
			{
				_items.Remove(key);
				_items.Add(new KeyValuePair<TKey, TValue>(key, value));
			}
		}

		public ICollection<TKey> Keys
		{
			get
			{
				return _items.Select(kv => kv.Key).ToList();
			}
		}

		public ICollection<TValue> Values
		{
			get
			{
				return _items.Select(kv => kv.Value).ToList();
			}
		}
	}

	public class GattApplication : ObjectManager
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static string PropertyIface = typeof(org.freedesktop.DBus.Properties)
			.GetCustomAttributes(false)
			.OfType<InterfaceAttribute>()
			.Select(x => x.Name)
			.First();

		public GattApplication()
		{
			Path = new ObjectPath("/com/peach");
			Services = new ByUuid<LocalService>();
			Advertisement = new LocalAdvertisement();
		}

		public ObjectPath Path { get; set; }

		public ByUuid<LocalService> Services { get; private set; }

		public LocalAdvertisement Advertisement { get; private set; }

		public string Introspect()
		{
			throw new NotImplementedException();
		}

		public IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> GetManagedObjects()
		{
			Logger.Trace("GetManagedObjects>");

			var ret = new OrderedDict<ObjectPath, IDictionary<string, IDictionary<string, object>>>();

			foreach (var svc in Services)
			{
				ret.Add(svc.Path, GetProps(svc));

				foreach (var chr in svc.Characteristics)
				{
					ret.Add(chr.Path, GetProps(chr));

					foreach (var dsc in chr.Descriptors)
					{
						ret.Add(dsc.Path, GetProps(dsc));
					}
				}
			}

			foreach (var kv1 in ret)
			{
				Logger.Trace("{0}", kv1.Key);

				foreach (var kv2 in kv1.Value)
				{
					Logger.Trace("  {0}", kv2.Key);
					foreach (var kv3 in kv2.Value)
					{
						Logger.Trace("    {0}={1}", kv3.Key, kv3.Value);
					}
				}
			}

			return ret;
		}

		public event InterfacesAddedDelegate InterfacesAdded
		{
			add { }
			remove { }
		}

		public event InterfacesRemovedDelegate IntefacesRemoved
		{
			add { }
			remove { }
		}

		private IDictionary<string, IDictionary<string, object>> GetProps(IGattProperties obj)
		{
			var ifaceNames = new[] { PropertyIface, obj.InterfaceName };
			return ifaceNames.ToDictionary(propName => propName, obj.GetAll);
		}
	}

}
