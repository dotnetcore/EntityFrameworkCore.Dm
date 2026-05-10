using System.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmDoubleTypeMapping : DoubleTypeMapping
	{
		public DmDoubleTypeMapping([NotNull] string storeType, DbType? dbType = null)
			: base(storeType, dbType)
		{
		}

		protected DmDoubleTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmDoubleTypeMapping(parameters);
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			string text = base.GenerateNonNullSqlLiteral(value);
			if (!text.Contains('E') && !text.Contains('e'))
			{
				return text + "E0";
			}
			return text;
		}
	}
}
