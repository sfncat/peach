using System;
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Simple interface for objects that are owned by a parent
	/// </summary>
	/// <typeparam name="T">Type of the owner</typeparam>
	public interface IOwned<T>
	{
		/// <summary>
		/// Returns the parent of the object
		/// </summary>
		T parent { get; set; }
	}

	/// <summary>
	/// Collection that contains both named and owned objects.
	/// Automatically synchronizes the parent property of all items
	/// to be the owner on insertion and null on removal.
	/// </summary>
	/// <typeparam name="TOwner">Thet type of the owner.</typeparam>
	/// <typeparam name="TObject">The type of the objects to collect.</typeparam>
	[Serializable]
	public class OwnedCollection<TOwner, TObject> : NamedCollection<TObject> where TObject : INamed, IOwned<TOwner>
	{
		protected TOwner owner { get; private set; }

		/// <summary>
		/// Constructs a new OwnedCollection
		/// </summary>
		/// <param name="owner">The value to set to the parent property of inserted items.</param>
		public OwnedCollection(TOwner owner)
		{
			this.owner = owner;
		}

		/// <summary>
		/// Constructs a new OwnedCollection.
		/// </summary>
		/// <param name="owner">The value to set to the parent property of inserted items.</param>
		/// <param name="baseName">Specifies the base name to use when generating unique names.</param>
		public OwnedCollection(TOwner owner, string baseName)
			: base(baseName)
		{
			this.owner = owner;
		}

		protected override void InsertItem(int index, TObject item)
		{
			item.parent = owner;

			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			this[index].parent = default(TOwner);

			base.RemoveItem(index);
		}

		protected override void SetItem(int index, TObject item)
		{
			this[index].parent = default(TOwner);
			item.parent = owner;

			base.SetItem(index, item);
		}
	}
}
