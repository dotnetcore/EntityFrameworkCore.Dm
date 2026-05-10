using System;
using System.Data;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmTimeSpanTypeMapping : TimeSpanTypeMapping
	{
		public DmTimeSpanTypeMapping([NotNull] string storeType, DbType? dbType = null)
			: base(storeType, dbType)
		{
		}

		protected DmTimeSpanTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmTimeSpanTypeMapping(parameters);
		}

		protected override void ConfigureParameter(DbParameter parameter)
		{
			base.ConfigureParameter(parameter);
			if (Precision.HasValue)
			{
				parameter.Scale = (byte)Precision.Value;
			}
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			TimeSpan timeSpan = (TimeSpan)value;
			string text = timeSpan.Milliseconds.ToString();
			text = text.PadLeft(4 - text.Length, '0');
			return $"INTERVAL '{timeSpan.Days} {timeSpan.Hours}:{timeSpan.Minutes}:{timeSpan.Seconds}.{text}' DAY TO SECOND";
		}
	}
}
