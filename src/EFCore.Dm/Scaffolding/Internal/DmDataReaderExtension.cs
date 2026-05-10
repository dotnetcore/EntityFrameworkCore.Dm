using System.Data.Common;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Dm.Scaffolding.Internal
{
	public static class DmDataReaderExtension
	{
		public static T GetValueOrDefault<T>([NotNull] this DbDataReader reader, [NotNull] string name)
		{
			int ordinal = reader.GetOrdinal(name);
			if (!reader.IsDBNull(ordinal))
			{
				return reader.GetFieldValue<T>(ordinal);
			}
			return default;
		}

		public static T GetValueOrDefault<T>([NotNull] this DbDataRecord record, [NotNull] string name)
		{
			int ordinal = record.GetOrdinal(name);
			if (!record.IsDBNull(ordinal))
			{
				return (T)record.GetValue(ordinal);
			}
			return default;
		}
	}
}
