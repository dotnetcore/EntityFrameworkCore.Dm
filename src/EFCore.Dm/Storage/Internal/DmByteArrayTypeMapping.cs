using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmByteArrayTypeMapping : ByteArrayTypeMapping
	{
		internal const int MaxSize = 32767;

		private readonly StoreTypePostfix? _storeTypePostfix;

		public DmByteArrayTypeMapping([NotNull] string storeType, DbType? dbType = System.Data.DbType.Binary, int? size = null, bool fixedLength = false, ValueComparer comparer = null, StoreTypePostfix? storeTypePostfix = null)
			: this(new RelationalTypeMappingParameters(new CoreTypeMappingParameters(typeof(byte[]), null, comparer, null, null, null, null, null), storeType, GetStoreTypePostfix(storeTypePostfix, size), dbType, false, size, fixedLength, (int?)null, (int?)null))
		{
			_storeTypePostfix = storeTypePostfix;
		}

		private static StoreTypePostfix GetStoreTypePostfix(StoreTypePostfix? storeTypePostfix, int? size)
		{
			StoreTypePostfix? val = storeTypePostfix;
			if (!val.HasValue)
			{
				if (size.HasValue && size <= MaxSize)
				{
					return (StoreTypePostfix)1;
				}
				return 0;
			}
			return val.GetValueOrDefault();
		}

		protected DmByteArrayTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}


		private static int CalculateSize(int? size)
		{
			if (!size.HasValue || !(size < MaxSize))
			{
				return MaxSize;
			}
			return size.Value;
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return (RelationalTypeMapping)(object)new DmByteArrayTypeMapping(parameters);
		}

		protected override void ConfigureParameter(DbParameter parameter)
		{
			object value = parameter.Value;
			int? num = (value as byte[])?.Length;
			int num2 = CalculateSize(Size);
			parameter.Size = ((value == null || value == DBNull.Value || (num.HasValue && num <= num2)) ? num2 : (-1));
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("0x");
			byte[] array = (byte[])value;
			foreach (byte b in array)
			{
				stringBuilder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
			}
			return stringBuilder.ToString();
		}
	}
}
