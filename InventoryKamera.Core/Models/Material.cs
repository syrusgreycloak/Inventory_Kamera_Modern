using System;
using System.Runtime.Serialization;

namespace InventoryKamera
{
	[Serializable]
	public struct Material : ISerializable
	{
		public string name;
		public int count;

		public Material(string _name, int _count)
		{
			name = _name;
			count = _count;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue(name, count);

		public override int GetHashCode()
		{
			return name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is Material material && name == material.name;
		}
	}
}
