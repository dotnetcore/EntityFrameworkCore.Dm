using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmSByteTypeMapping : SByteTypeMapping
	{
		public DmSByteTypeMapping(string storeType, DbType? dbType = System.Data.DbType.SByte)
			: base(storeType, dbType)
		{
		}

		protected DmSByteTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmSByteTypeMapping(parameters);
		}
	}
}
