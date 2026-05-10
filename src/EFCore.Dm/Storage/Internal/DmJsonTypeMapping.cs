using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data.Common;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmJsonTypeMapping : JsonTypeMapping
	{
		private static readonly MethodInfo CreateUtf8StreamMethod = typeof(DmJsonTypeMapping)!.GetMethod("CreateUtf8Stream", new Type[1] { typeof(string) });

		private static readonly MethodInfo GetStringMethod = typeof(DbDataReader).GetRuntimeMethod("GetString", new Type[1] { typeof(int) });

		public static DmJsonTypeMapping Default { get; } = new DmJsonTypeMapping("json");


		public DmJsonTypeMapping(string storeType)
			: base(storeType, typeof(JsonElement), (System.Data.DbType?)System.Data.DbType.String)
		{
		}

		protected DmJsonTypeMapping(RelationalTypeMappingParameters parameters)
			: base(parameters)
		{
		}

		protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
		{
			return new DmJsonTypeMapping(parameters);
		}

		protected virtual string EscapeSqlLiteral(string literal)
		{
			return literal.Replace("'", "''");
		}

		protected override string GenerateNonNullSqlLiteral(object value)
		{
			return "'" + EscapeSqlLiteral(JsonSerializer.Serialize(value)) + "'";
		}

		public override MethodInfo GetDataReaderMethod()
		{
			return GetStringMethod;
		}

		public static MemoryStream CreateUtf8Stream(string json)
		{
			if (json == "")
			{
				throw new InvalidOperationException(RelationalStrings.JsonEmptyString);
			}
			return new MemoryStream(Encoding.UTF8.GetBytes(json));
		}

		public override Expression CustomizeDataReaderExpression(Expression expression)
		{
			return Expression.Call(CreateUtf8StreamMethod, expression);
		}
	}
}
