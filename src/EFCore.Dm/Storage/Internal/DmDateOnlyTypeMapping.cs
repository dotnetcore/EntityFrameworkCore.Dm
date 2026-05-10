using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmDateOnlyTypeMapping : DateOnlyTypeMapping
	{
		private const string DateFormatConst = "'{0:yyyy-MM-dd}'";

		protected override string SqlLiteralFormatString => "'{0:yyyy-MM-dd}'";

		public DmDateOnlyTypeMapping(string storeType, DbType? dbType)
			: base(storeType, dbType)
		{
		}

		protected DmDateOnlyTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmDateOnlyTypeMapping(parameters);
		}
	}
}
