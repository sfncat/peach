using System;

namespace Peach.Pro.Core.Storage
{
	[AttributeUsage(AttributeTargets.Property)]
	class NotMappedAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	class TableAttribute : Attribute
	{
		public string Name { get; set; }
		public TableAttribute(string name) { Name = name; }
	}

	[AttributeUsage(AttributeTargets.Property)]
	class KeyAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	class RequiredAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	class UniqueAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	class IndexAttribute : Attribute
	{
		public string Name { get; set; }
		public bool IsUnique { get; set; }

		public IndexAttribute(string name) { Name = name; }
	}

	[AttributeUsage(AttributeTargets.Property)]
	class ForeignKeyAttribute : Attribute
	{
		public string Name { get; set; }
		public Type TargetEntity { get; set; }
		public string TargetProperty { get; set; }

		public ForeignKeyAttribute(Type targetEntity)
		{
			TargetEntity = targetEntity;
			TargetProperty = "Id";
		}
	}
}
