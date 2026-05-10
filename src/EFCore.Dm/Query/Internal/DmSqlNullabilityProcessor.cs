using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	public class DmSqlNullabilityProcessor : SqlNullabilityProcessor
	{
		public DmSqlNullabilityProcessor(RelationalParameterBasedSqlProcessorDependencies dependencies, RelationalParameterBasedSqlProcessorParameters parameters)
			: base(dependencies, parameters)
		{
		}

		protected override SqlExpression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression, bool allowOptimizedExpansion, out bool nullable)
		{
			if (sqlBinaryExpression.OperatorType == ExpressionType.Equal && (IsNotNullBoolConst(sqlBinaryExpression.Left) || IsNotNullBoolConst(sqlBinaryExpression.Right)))
			{
				nullable = false;
				return sqlBinaryExpression;
			}
			return base.VisitSqlBinary(sqlBinaryExpression, false, out nullable);
		}

		protected override SqlExpression VisitIn(InExpression inExpression, bool allowOptimizedExpansion, out bool nullable)
		{
			SqlExpression visited = base.VisitIn(inExpression, allowOptimizedExpansion, out nullable);

			// base.VisitIn expands ValuesParameter into a VALUES table subquery
			// (SELECT v FROM (VALUES (v1),(v2),...) t(v))
			// DM does not support VALUES(...) as a table expression.
			// Convert to a flat IN (v1, v2, ...) values list instead.
			if (visited is InExpression resultIn
				&& resultIn.Subquery is { } sq
				&& sq.Tables.Count == 1
				&& sq.Tables[0] is ValuesExpression ve)
			{
				List<SqlExpression> values = ve.RowValues.Select(rv => rv.Values[0]).ToList();
				nullable = false;
				visited = resultIn.Update(resultIn.Item, subquery: null, values, valuesParameter: null);
			}

			// When the collection is empty, base.VisitIn returns Constant(false).
			// DM does not allow bare value expressions (e.g. WHERE 0) as filter conditions.
			// Use a literal fragment to avoid constant-folding: "1 = 0" / "1 = 1".
			if (visited is SqlConstantExpression { Value: bool boolVal })
			{
				nullable = false;
				return Dependencies.SqlExpressionFactory.Fragment(boolVal ? "1 = 1" : "1 = 0");
			}

			return visited;
		}

		private bool IsNotNullBoolConst(SqlExpression expression)
		{
            if (expression is SqlConstantExpression constantExpr)
            {
                object value = constantExpr.Value;
                if (value is bool v)
                {
                    _ = v;
                    return true;
                }
            }
            return false;
		}
	}
}
