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
	public class DmStringTypeMapping : StringTypeMapping
	{
		internal const int UnicodeMax = 32767;

		private const int AnsiMax = 32767;

		private readonly int _maxSpecificSize;

		private readonly StoreTypePostfix? _storeTypePostfix;

		public DmStringTypeMapping([NotNull] string storeType, [CanBeNull] DbType? dbType, bool unicode = false, int? size = null, bool fixedLength = false, StoreTypePostfix? storeTypePostfix = null)
			: this(new RelationalTypeMappingParameters(new CoreTypeMappingParameters(typeof(string), (ValueConverter)null, (ValueComparer)null, (ValueComparer)null, (ValueComparer)null, (Func<IProperty, ITypeBase, ValueGenerator>)null, (CoreTypeMapping)null, (JsonValueReaderWriter)(object)JsonStringReaderWriter.Instance), storeType, GetStoreTypePostfix(storeTypePostfix, unicode, size), dbType, unicode, size, fixedLength, (int?)null, (int?)null))
		{
			_storeTypePostfix = storeTypePostfix;
		}

		protected DmStringTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
			_maxSpecificSize = CalculateSize(parameters.Unicode, parameters.Size);
		}

		private static StoreTypePostfix GetStoreTypePostfix(StoreTypePostfix? storeTypePostfix, bool unicode, int? size)
		{
			StoreTypePostfix? val = storeTypePostfix;
			if (!val.HasValue)
			{
				if (!unicode)
				{
					if (size.HasValue && size <= 32767)
					{
						return (StoreTypePostfix)1;
					}
					return (StoreTypePostfix)0;
				}
				if (size.HasValue && size <= 32767)
				{
					return (StoreTypePostfix)1;
				}
				return (StoreTypePostfix)0;
			}
			return val.GetValueOrDefault();
		}

		private static int CalculateSize(bool unicode, int? size)
		{
			if (!unicode)
			{
				if (!size.HasValue || !(size <= 32767))
				{
					return 32767;
				}
				return size.Value;
			}
			if (!size.HasValue || !(size <= 32767))
			{
				return 32767;
			}
			return size.Value;
		}

		private static DbType? GetDbType(bool unicode, bool fixedLength)
		{
			return (!unicode) ? (fixedLength ? System.Data.DbType.AnsiStringFixedLength : System.Data.DbType.AnsiString) : (fixedLength ? System.Data.DbType.StringFixedLength : System.Data.DbType.String);
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			if (parameters.Unicode)
			{
				parameters = new RelationalTypeMappingParameters(parameters.CoreParameters, parameters.StoreType, parameters.StoreTypePostfix, GetDbType(parameters.Unicode, parameters.FixedLength), parameters.Unicode, parameters.Size, parameters.FixedLength, parameters.Precision, parameters.Scale);
			}
			return new DmStringTypeMapping(parameters);
		}

		protected override void ConfigureParameter(DbParameter parameter)
		{
			object value = parameter.Value;
			int? num = (value as string)?.Length;
			parameter.Size = ((value == null || value == DBNull.Value || (num.HasValue && num <= _maxSpecificSize)) ? _maxSpecificSize : 0);
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			return "'" + EscapeSqlLiteral((string)value) + "'";
		}
	}
}
