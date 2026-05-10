using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	public class DmNewGuidTranslator : IMethodCallTranslator
	{
		private static readonly MethodInfo _methodInfo = typeof(Guid).GetRuntimeMethod("NewGuid", Array.Empty<Type>());

		private readonly ISqlExpressionFactory _sqlExpressionFactory;

		public DmNewGuidTranslator(ISqlExpressionFactory sqlExpressionFactory)
		{
			_sqlExpressionFactory = sqlExpressionFactory;
		}

		public virtual SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
		{
			if (!_methodInfo.Equals(method))
			{
				return null;
			}
			return _sqlExpressionFactory.Function("NEWID", Array.Empty<SqlExpression>(), false, Array.Empty<bool>(), method.ReturnType, null);
		}
	}
}
