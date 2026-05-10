using System.Data;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmDateTimeTypeMapping : DateTimeTypeMapping
	{
		private const string DateFormatConst = "{0:yyyy-MM-dd}";

		private const string DateTimeFormatConst = "{0:yyyy-MM-dd HH:mm:ss.ffffff}";

		protected override string SqlLiteralFormatString
		{
			get
			{
				if (!(StoreType == "date"))
				{
					return "'{0:yyyy-MM-dd HH:mm:ss.ffffff}'";
				}
				return "'{0:yyyy-MM-dd}'";
			}
		}

		public DmDateTimeTypeMapping([NotNull] string storeType, DbType? dbType = null)
			: base(storeType, dbType)
		{
		}

		protected DmDateTimeTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override void ConfigureParameter(DbParameter parameter)
		{
			base.ConfigureParameter(parameter);
			if (Size.HasValue && Size.Value != -1)
			{
				parameter.Size = Size.Value;
			}
			if (Precision.HasValue)
			{
				parameter.Scale = (byte)Precision.Value;
			}
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmDateTimeTypeMapping(parameters);
		}
	}
}
