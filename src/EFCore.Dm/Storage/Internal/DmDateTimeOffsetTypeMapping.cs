using System.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmDateTimeOffsetTypeMapping : DateTimeOffsetTypeMapping
	{
		private const string DateTimeOffsetFormatConst = "{0:yyyy-MM-dd HH:mm:ss.fffzzz}";

		protected override string SqlLiteralFormatString => "'{0:yyyy-MM-dd HH:mm:ss.fffzzz}'";

		public DmDateTimeOffsetTypeMapping([NotNull] string storeType, DbType? dbType = System.Data.DbType.DateTimeOffset)
			: base(storeType, dbType)
		{
		}

		protected DmDateTimeOffsetTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmDateTimeOffsetTypeMapping(parameters);
		}
	}
}
