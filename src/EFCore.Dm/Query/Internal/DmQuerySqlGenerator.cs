using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Storage.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    public class DmQuerySqlGenerator : QuerySqlGenerator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        public DmQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies, IRelationalTypeMappingSource typeMappingSource)
            : base(dependencies)
        {
            _typeMappingSource = typeMappingSource;
        }

        private bool HasStringChild(SqlBinaryExpression binaryExpression)
        {
            if (binaryExpression.Left != null && binaryExpression.Left.Type == typeof(string))
            {
                return true;
            }
            if (binaryExpression.Right != null && binaryExpression.Right.Type == typeof(string))
            {
                return true;
            }
            if (binaryExpression.Left != null && binaryExpression.Left is SqlBinaryExpression)
            {
                return HasStringChild((SqlBinaryExpression)binaryExpression.Left);
            }
            if (binaryExpression.Right != null && binaryExpression.Right is SqlBinaryExpression)
            {
                return HasStringChild((SqlBinaryExpression)binaryExpression.Right);
            }
            return false;
        }

        protected override string GetOperator(SqlBinaryExpression binaryExpression)
        {
            if (binaryExpression.OperatorType == ExpressionType.Add)
            {
                if (binaryExpression.Left.Type == typeof(string) || binaryExpression.Right.Type == typeof(string))
                {
                    return "||";
                }
                if (binaryExpression.Left is SqlBinaryExpression && HasStringChild((SqlBinaryExpression)binaryExpression.Left))
                {
                    return "||";
                }
                if (binaryExpression.Right is SqlBinaryExpression && HasStringChild((SqlBinaryExpression)binaryExpression.Right))
                {
                    return "||";
                }
            }
            return base.GetOperator(binaryExpression);
        }

        protected override void GenerateTop(SelectExpression selectExpression)
        {
            if (selectExpression.Limit != null && selectExpression.Offset == null)
            {
                Sql.Append("TOP(");
                Visit(selectExpression.Limit);
                Sql.Append(") ");
            }
        }

        protected override void GenerateLimitOffset(SelectExpression selectExpression)
        {
            if (selectExpression.Offset != null)
            {
                Sql.AppendLine().Append("OFFSET ");
                Visit(selectExpression.Offset);
                Sql.Append(" ROWS");
                if (selectExpression.Limit != null)
                {
                    Sql.Append(" FETCH NEXT ");
                    Visit(selectExpression.Limit);
                    Sql.Append(" ROWS ONLY");
                }
            }
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
        {
            if (sqlFunctionExpression.Name.StartsWith("@@", StringComparison.Ordinal))
            {
                Sql.Append(sqlFunctionExpression.Name);
                return sqlFunctionExpression;
            }
            if (sqlFunctionExpression.Name == "COUNT")
            {
                if (sqlFunctionExpression.Type == typeof(int))
                {
                    Sql.Append(" CAST(");
                    base.VisitSqlFunction(sqlFunctionExpression);
                    Sql.Append(" AS INT) ");
                    return sqlFunctionExpression;
                }
            }
            else if (sqlFunctionExpression.Name == "SUM")
            {
                if (sqlFunctionExpression.Type == typeof(int))
                {
                    Sql.Append(" CAST(");
                    base.VisitSqlFunction(sqlFunctionExpression);
                    Sql.Append(" AS INT) ");
                    return sqlFunctionExpression;
                }
            }
            else
            {
                if ((sqlFunctionExpression.Name == "TRUNCATE" || sqlFunctionExpression.Name == "ROUND") && sqlFunctionExpression.Type == typeof(double))
                {
                    Sql.Append(" CAST(");
                    base.VisitSqlFunction(sqlFunctionExpression);
                    Sql.Append(" AS DOUBLE)");
                    return sqlFunctionExpression;
                }
                if (sqlFunctionExpression.Name == "TIMEONLY.FROMDATETIME")
                {
                    Sql.Append(" CAST(");
                    GenerateList(sqlFunctionExpression.Arguments, e => Visit(e));
                    Sql.Append(" AS TIME)");
                    return null;
                }
                if (sqlFunctionExpression.Name == "TIMEONLY.FROMTIMESPAN")
                {
                    GenerateList(sqlFunctionExpression.Arguments, e => Visit(e));
                    return null;
                }
            }
            if (sqlFunctionExpression.Name.Equals("POSITION"))
            {
                Sql.Append("LOCATE");
                Sql.Append("(");
                GenerateList(sqlFunctionExpression.Arguments, e => Visit(e));
                Sql.Append(")");
                return sqlFunctionExpression;
            }
            if (!sqlFunctionExpression.IsBuiltIn && string.IsNullOrEmpty(sqlFunctionExpression.Schema))
            {
                sqlFunctionExpression = new SqlFunctionExpression("SYSDBA", sqlFunctionExpression.Name, sqlFunctionExpression.Arguments, sqlFunctionExpression.IsNullable, sqlFunctionExpression.ArgumentsPropagateNullability, sqlFunctionExpression.Type, sqlFunctionExpression.TypeMapping);
            }
            return base.VisitSqlFunction(sqlFunctionExpression);
        }

        private static bool RequiresBrackets(SqlExpression expression)
        {
            if (!(expression is SqlBinaryExpression))
            {
                return expression is LikeExpression;
            }
            return true;
        }

        private void GenerateList<T>(IReadOnlyList<T> items, Action<T> generationAction, Action<IRelationalCommandBuilder> joinAction = null)
        {
            joinAction ??= isb =>
            {
                isb.Append(", ");
            };
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    joinAction(Sql);
                }
                generationAction(items[i]);
            }
        }

        protected override Expression VisitDelete(DeleteExpression deleteExpression)
        {
            SelectExpression selectExpression = deleteExpression.SelectExpression;
            if (selectExpression.Offset != null || selectExpression.Limit != null || selectExpression.Having != null || selectExpression.Orderings.Count != 0 || selectExpression.GroupBy.Count != 0 || selectExpression.Tables.Count != 1 || selectExpression.Tables[0] != deleteExpression.Table || selectExpression.Projection.Count != 0)
            {
                throw new InvalidOperationException(RelationalStrings.ExecuteOperationWithUnsupportedOperatorInSqlGeneration((object)"ExecuteDelete"));
            }
            Sql.Append("DELETE FROM ");
            Visit(deleteExpression.Table);
            if (selectExpression.Predicate != null)
            {
                Sql.AppendLine().Append("WHERE ");
                SqlExpression predicate = selectExpression.Predicate;
                SqlConstantExpression constantPredicate = predicate as SqlConstantExpression;
                if (constantPredicate != null && constantPredicate.Type == typeof(bool))
                {
                    Sql.Append(((bool)constantPredicate.Value) ? "TRUE " : "FALSE ");
                }
                else
                {
                    Visit(selectExpression.Predicate);
                }
            }
            return deleteExpression;
        }

        protected override Expression VisitJsonScalar(JsonScalarExpression jsonScalarExpression)
        {
            if (jsonScalarExpression.Path.Count == 0)
            {
                Visit(jsonScalarExpression.Json);
                return jsonScalarExpression;
            }

            RelationalTypeMapping typeMapping = jsonScalarExpression.TypeMapping;
            if (typeMapping is not DmJsonTypeMapping && typeMapping?.ElementTypeMapping == null)
                Sql.Append(typeMapping is StringTypeMapping ? "JSON_VALUE(" : "CAST(JSON_VALUE(");
            else
                Sql.Append("JSON_QUERY(");

            Visit(jsonScalarExpression.Json);
            Sql.Append(", ");
            GenerateJsonPath(jsonScalarExpression.Path);
            Sql.Append(")");

            if (typeMapping is not DmJsonTypeMapping && typeMapping is not StringTypeMapping)
            {
                Sql.Append(" AS ");
                Sql.Append(typeMapping.StoreType);
                Sql.Append(")");
            }

            return jsonScalarExpression;
        }

        private void GenerateJsonPath(IReadOnlyList<PathSegment> path)
        {
            Sql.Append("'$");
            foreach (PathSegment item in path)
            {
                PathSegment current = item;
                string propertyName = current.PropertyName;
                if (propertyName == null)
                {
                    SqlExpression arrayIndex = current.ArrayIndex;
                    if (arrayIndex == null)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    Sql.Append("[");
                    if (arrayIndex is SqlConstantExpression)
                    {
                        Visit(arrayIndex);
                    }
                    else
                    {
                        Sql.Append("' + CAST(");
                        Visit(arrayIndex);
                        Sql.Append(" AS ");
                        Sql.Append(RelationalTypeMappingSourceExtensions.GetMapping(_typeMappingSource, typeof(string)).StoreType);
                        Sql.Append(") + '");
                    }
                    Sql.Append("]");
                }
                else
                {
                    Sql.Append(".").Append(Dependencies.SqlGenerationHelper.DelimitJsonPathElement(propertyName));
                }
            }
            Sql.Append("'");
        }

        protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
        {
            switch (sqlUnaryExpression.OperatorType)
            {
                case ExpressionType.Convert:
                    {
                        Sql.Append("CAST(");
                        bool needsParens = RequiresParentheses(sqlUnaryExpression, sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append("(");
                        Visit(sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append(")");
                        Sql.Append(" AS ");
                        Sql.Append(sqlUnaryExpression.TypeMapping.StoreType);
                        Sql.Append(")");
                        break;
                    }
                case ExpressionType.Equal:
                    {
                        bool needsParens = RequiresParentheses(sqlUnaryExpression, sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append("(");
                        Visit(sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append(")");
                        Sql.Append(" IS NULL");
                        break;
                    }
                case ExpressionType.Negate:
                    {
                        Sql.Append("-");
                        bool needsParens = RequiresParentheses(sqlUnaryExpression, sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append("(");
                        Visit(sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append(")");
                        break;
                    }
                case ExpressionType.NotEqual:
                    {
                        bool needsParens = RequiresParentheses(sqlUnaryExpression, sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append("(");
                        Visit(sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append(")");
                        Sql.Append(" IS NOT NULL");
                        break;
                    }
                case ExpressionType.Not when sqlUnaryExpression.Type == typeof(bool):
                    {
                        SqlExpression operand = sqlUnaryExpression.Operand;
                        if (operand is InExpression inExpr)
                            GenerateIn(inExpr, true);
                        else if (operand is ExistsExpression existsExpr)
                            GenerateExists(existsExpr, true);
                        else if (operand is LikeExpression likeExpr)
                            GenerateLike(likeExpr, true);
                        else if (operand is ColumnExpression columnExpr)
                        {
                            Visit(columnExpr);
                            Sql.Append(" == 0");
                        }
                        else
                        {
                            Sql.Append("NOT (");
                            Visit(sqlUnaryExpression.Operand);
                            Sql.Append(")");
                        }
                        break;
                    }
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                    {
                        Sql.Append("~");
                        bool needsParens = RequiresParentheses(sqlUnaryExpression, sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append("(");
                        Visit(sqlUnaryExpression.Operand);
                        if (needsParens) Sql.Append(")");
                        break;
                    }
            }
            return sqlUnaryExpression;
        }

        // DM does not support VALUES(...) as a table-valued expression in FROM clause.
        // EF Core expands ValuesParameter into one of:
        //   IN  (SELECT v FROM (VALUES (v1),(v2),...) t(v))
        //   EXISTS (SELECT 1 FROM (VALUES (v1),(v2),...) t(v) WHERE t.v = col)
        // Both are rewritten to: col IN (v1, v2, ...) or col NOT IN (v1, v2, ...)

        private static bool TryGetValuesExpression(SelectExpression subquery, out ValuesExpression valuesExpression)
        {
            if (subquery.Tables.Count == 1 && subquery.Tables[0] is ValuesExpression ve)
            {
                valuesExpression = ve;
                return true;
            }
            valuesExpression = null;
            return false;
        }

        private void GenerateInFromValuesExpression(SqlExpression item, ValuesExpression valuesExpression, bool negated)
        {
            Visit(item);
            Sql.Append(negated ? " NOT IN " : " IN ");
            Sql.Append("(");
            for (int i = 0; i < valuesExpression.RowValues.Count; i++)
            {
                if (i > 0) Sql.Append(", ");
                Visit(valuesExpression.RowValues[i].Values[0]);
            }
            Sql.Append(")");
        }

        protected override void GenerateIn(InExpression inExpression, bool negated)
        {
            if (inExpression.Subquery is { } subquery && TryGetValuesExpression(subquery, out var ve))
            {
                GenerateInFromValuesExpression(inExpression.Item, ve, negated);
                return;
            }
            base.GenerateIn(inExpression, negated);
        }

        protected override void GenerateExists(ExistsExpression existsExpression, bool negated)
        {
            if (TryRewriteExistsAsIn(existsExpression.Subquery, negated))
                return;
            base.GenerateExists(existsExpression, negated);
        }

        // EXISTS (SELECT 1 FROM (VALUES (v1),(v2),...) t(v) WHERE t.v = col)
        // NOT EXISTS(...) → rewrite to: col [NOT] IN (v1, v2, ...)
        private bool TryRewriteExistsAsIn(SelectExpression subquery, bool negated)
        {
            if (!TryGetValuesExpression(subquery, out var ve))
            {
                return false;
            }

            if (subquery.Predicate is not SqlBinaryExpression pred
                || pred.OperatorType != ExpressionType.Equal)
            {
                return false;
            }

            // One side references the VALUES table alias, the other is the outer column
            SqlExpression outerCol = null;
            if (pred.Left is ColumnExpression lc && lc.TableAlias == ve.Alias)
            {
                outerCol = pred.Right;
            }
            else if (pred.Right is ColumnExpression rc && rc.TableAlias == ve.Alias)
            {
                outerCol = pred.Left;
            }

            if (outerCol == null)
            {
                return false;
            }

            GenerateInFromValuesExpression(outerCol, ve, negated);
            return true;
        }

        protected override Expression VisitTableValuedFunction(TableValuedFunctionExpression tableValuedFunctionExpression)
        {
            if (!string.IsNullOrEmpty(tableValuedFunctionExpression.Schema))
            {
                Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tableValuedFunctionExpression.Schema)).Append(".");
            }
            Sql.Append("table(");
            string text = (tableValuedFunctionExpression.IsBuiltIn ? tableValuedFunctionExpression.Name : Dependencies.SqlGenerationHelper.DelimitIdentifier(tableValuedFunctionExpression.Name));
            Sql.Append(text).Append("(");
            GenerateList(tableValuedFunctionExpression.Arguments, e => Visit(e));
            Sql.Append(")");
            Sql.Append(")").Append(AliasSeparator)
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tableValuedFunctionExpression.Alias));
            return tableValuedFunctionExpression;
        }
    }
}
