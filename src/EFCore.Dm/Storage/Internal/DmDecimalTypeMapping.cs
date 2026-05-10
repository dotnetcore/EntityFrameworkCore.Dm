using System;
using System.Data;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmDecimalTypeMapping : DecimalTypeMapping
	{
		public DmDecimalTypeMapping([NotNull] string storeType, DbType? dbType = null, int? precision = null, int? scale = null)
			: this(new RelationalTypeMappingParameters(new CoreTypeMappingParameters(typeof(decimal), (ValueConverter)null, (ValueComparer)null, (ValueComparer)null, (ValueComparer)null, (Func<IProperty, ITypeBase, ValueGenerator>)null, (CoreTypeMapping)null, (JsonValueReaderWriter)null), storeType, (StoreTypePostfix)3, dbType, false, (int?)null, false, precision, scale))
		{
		}

		protected DmDecimalTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmDecimalTypeMapping(parameters);
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
				parameter.Precision = (byte)Precision.Value;
			}
			if (Scale.HasValue)
			{
				parameter.Scale = (byte)Scale.Value;
			}
		}
	}
}
