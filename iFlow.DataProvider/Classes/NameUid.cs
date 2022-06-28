using System;
using iFlow.Utils;

namespace iFlow.Shared
{
	internal class UidName
	{
		public Guid? Uid { get; set; }
		public string Name { get; set; }

		public bool EqualsTo(Guid? uid, string name)
		{
			if (Uid == null || uid == null)
			{
				return Name != null && IgnoreCase.Equals(Name, name, true);
			}
			else
			{
				if (Uid == uid)
				{
					if (Name == null || name == null || Name == name)
						return true;
					throw new Exception($"Имя \"{name}\" сборки \"{Uid}\" не совпадает с искомым \"{Name}\"");
				}
				else
				{
					if (Name == null || name == null || Name != name)
						return false;
					throw new Exception($"Uid \"{uid}\" сборки \"{Name}\" не совпадает с искомым \"{Uid}\"");
				}
			}
		}

	}

}
