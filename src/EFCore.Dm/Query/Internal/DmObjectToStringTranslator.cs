using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	public class DmObjectToStringTranslator : IMethodCallTranslator
	{
		private const int DefaultLength = 100;

		private static readonly Dictionary<Type, string> _typeMapping;

		private readonly ISqlExpressionFactory _sqlExpressionFactory;

		public DmObjectToStringTranslator(ISqlExpressionFactory sqlExpressionFactory)
		{
			_sqlExpressionFactory = sqlExpressionFactory;
		}

		public virtual SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
		{
			if (!(method.Name == "ToString") || arguments.Count != 0 || instance == null || !_typeMapping.TryGetValue(instance.Type.UnwrapNullableType(), out var value))
			{
				return null;
			}
			return _sqlExpressionFactory.Function("CONVERT", new SqlExpression[]
			{
				_sqlExpressionFactory.Fragment(value),
				instance
			}, true, new bool[2] { false, true }, typeof(string), null);
		}

		static DmObjectToStringTranslator()
		{
			Dictionary<Type, string> typeMapping = new Dictionary<Type, string>
			{
				{
					typeof(int),
					"VARCHAR(11)"
				},
				{
					typeof(long),
					"VARCHAR(20)"
				}
			};
			typeMapping.Add(typeof(DateTime), $"VARCHAR({DefaultLength})");
			typeMapping.Add(typeof(Guid), "CHAR(36)");
			typeMapping.Add(typeof(byte), "VARCHAR(3)");
			typeMapping.Add(typeof(byte[]), $"VARCHAR({DefaultLength})");
			typeMapping.Add(typeof(double), $"VARCHAR({DefaultLength})");
			typeMapping.Add(typeof(DateTimeOffset), $"VARCHAR({DefaultLength})");
			typeMapping.Add(typeof(char), "VARCHAR(1)");
			typeMapping.Add(typeof(short), "VARCHAR(6)");
			typeMapping.Add(typeof(float), $"VARCHAR({DefaultLength})");
			typeMapping.Add(typeof(decimal), $"VARCHAR({DefaultLength})");
			typeMapping.Add(typeof(TimeSpan), $"VARCHAR({DefaultLength})");
			typeMapping.Add(typeof(uint), "VARCHAR(10)");
			typeMapping.Add(typeof(ushort), "VARCHAR(5)");
			typeMapping.Add(typeof(ulong), "VARCHAR(19)");
			typeMapping.Add(typeof(sbyte), "VARCHAR(4)");
			_typeMapping = typeMapping;
		}
	}
}
