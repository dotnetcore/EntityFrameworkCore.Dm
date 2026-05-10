using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmTimeOnlyTypeMapping : TimeOnlyTypeMapping
	{
		public DmTimeOnlyTypeMapping(string storeType, DbType? dbType)
			: base(storeType, dbType)
		{
		}

		protected DmTimeOnlyTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmTimeOnlyTypeMapping(parameters);
		}
	}
}
