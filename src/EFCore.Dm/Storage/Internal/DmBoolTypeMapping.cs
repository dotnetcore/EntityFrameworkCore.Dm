using System;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmBoolTypeMapping : BoolTypeMapping
	{
		public DmBoolTypeMapping(string storeType, DbType? dbType)
			: base(storeType, dbType)
		{
		}

		protected DmBoolTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			if (value is bool v)
			{
				if (!v)
				{
					return "0";
				}
				return "1";
			}
			if (Convert.ToInt32(value) != 1)
			{
				return "0";
			}
			return "1";
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmBoolTypeMapping(parameters);
		}
	}
}
