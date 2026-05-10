using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Extensions;
using Microsoft.EntityFrameworkCore.Dm.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	public class DmFullTextSearchFunctionsTranslator : IMethodCallTranslator
	{
		private const string FreeTextFunctionName = "FREETEXT";

		private const string ContainsFunctionName = "CONTAINS";

		private static readonly MethodInfo _freeTextMethodInfo = typeof(DmDbFunctionsExtensions).GetRuntimeMethod("FreeText", new Type[3]
		{
			typeof(DbFunctions),
			typeof(string),
			typeof(string)
		});

		private static readonly MethodInfo _freeTextMethodInfoWithLanguage = typeof(DmDbFunctionsExtensions).GetRuntimeMethod("FreeText", new Type[4]
		{
			typeof(DbFunctions),
			typeof(string),
			typeof(string),
			typeof(int)
		});

		private static readonly MethodInfo _containsMethodInfo = typeof(DmDbFunctionsExtensions).GetRuntimeMethod("Contains", new Type[3]
		{
			typeof(DbFunctions),
			typeof(string),
			typeof(string)
		});

		private static readonly MethodInfo _containsMethodInfoWithLanguage = typeof(DmDbFunctionsExtensions).GetRuntimeMethod("Contains", new Type[4]
		{
			typeof(DbFunctions),
			typeof(string),
			typeof(string),
			typeof(int)
		});

		private static readonly IDictionary<MethodInfo, string> _functionMapping = new Dictionary<MethodInfo, string>
		{
			{ _freeTextMethodInfo, FreeTextFunctionName },
			{ _freeTextMethodInfoWithLanguage, FreeTextFunctionName },
			{ _containsMethodInfo, ContainsFunctionName },
			{ _containsMethodInfoWithLanguage, ContainsFunctionName }
		};

		private readonly ISqlExpressionFactory _sqlExpressionFactory;

		public DmFullTextSearchFunctionsTranslator(ISqlExpressionFactory sqlExpressionFactory)
		{
			_sqlExpressionFactory = sqlExpressionFactory;
		}

		public virtual SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
		{
			if (_functionMapping.TryGetValue(method, out var value))
			{
				SqlExpression val = arguments[1];
				if (!(val is ColumnExpression))
				{
					throw new InvalidOperationException(DmStrings.InvalidColumnNameForFreeText);
				}
				RelationalTypeMapping typeMapping = val.TypeMapping;
				SqlExpression item = _sqlExpressionFactory.ApplyTypeMapping(arguments[2], typeMapping);
				List<SqlExpression> list = new List<SqlExpression> { val, item };
				if (arguments.Count == 4)
				{
					ISqlExpressionFactory sqlExpressionFactory = _sqlExpressionFactory;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 1);
					defaultInterpolatedStringHandler.AppendLiteral("LANGUAGE ");
					defaultInterpolatedStringHandler.AppendFormatted(((SqlConstantExpression)arguments[3]).Value);
					list.Add(sqlExpressionFactory.Fragment(defaultInterpolatedStringHandler.ToStringAndClear()));
				}
				return _sqlExpressionFactory.Function(value, list, true, list.Select((SqlExpression a) => false).ToList(), typeof(bool), null);
			}
			return null;
		}

}
}
