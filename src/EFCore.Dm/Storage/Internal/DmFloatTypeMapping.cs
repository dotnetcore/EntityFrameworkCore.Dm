using System.Data;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmFloatTypeMapping : FloatTypeMapping
	{
		public DmFloatTypeMapping([NotNull] string storeType, DbType? dbType = null)
			: base(storeType, dbType)
		{
		}

		protected DmFloatTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmFloatTypeMapping(parameters);
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			return $"CAST({base.GenerateNonNullSqlLiteral(value)} AS {StoreType})";
		}

		protected override void ConfigureParameter(DbParameter parameter)
		{
			base.ConfigureParameter(parameter);
			if (Precision.HasValue && Precision.Value != -1)
			{
				parameter.Size = (byte)Precision.Value;
			}
		}
	}
}
