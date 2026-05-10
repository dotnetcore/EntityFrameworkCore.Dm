using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	public class DmIsDateFunctionTranslator : IMethodCallTranslator
	{
		private readonly ISqlExpressionFactory _sqlExpressionFactory;

		private static readonly MethodInfo _methodInfo = typeof(DmDbFunctionsExtensions).GetRuntimeMethod("IsDate", new Type[2]
		{
			typeof(DbFunctions),
			typeof(string)
		});

		public DmIsDateFunctionTranslator(ISqlExpressionFactory sqlExpressionFactory)
		{
			_sqlExpressionFactory = sqlExpressionFactory;
		}

		public virtual SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
		{
			if (!_methodInfo.Equals(method))
			{
				return null;
			}
			return _sqlExpressionFactory.Convert(_sqlExpressionFactory.Function("ISDATE", new SqlExpression[] { arguments[1] }, true, new bool[1] { true }, _methodInfo.ReturnType, null), _methodInfo.ReturnType, null);
		}
	}
}
