using System.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmByteTypeMapping : ByteTypeMapping
	{
		public DmByteTypeMapping([NotNull] string storeType, DbType? dbType = null)
			: base(storeType, dbType)
		{
		}

		protected DmByteTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmByteTypeMapping(parameters);
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			return $"CAST({base.GenerateNonNullSqlLiteral(value)} AS {StoreType})";
		}
	}
}
