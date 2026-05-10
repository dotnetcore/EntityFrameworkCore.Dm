using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    public class SearchConditionConvertingExpressionVisitor : SqlExpressionVisitor
    {
        private bool _isSearchCondition;

        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public SearchConditionConvertingExpressionVisitor(ISqlExpressionFactory sqlExpressionFactory)
            : base()
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        private Expression ApplyConversion(SqlExpression sqlExpression, bool condition)
        {
            if (!_isSearchCondition)
            {
                return ConvertToValue(sqlExpression, condition);
            }
            return ConvertToSearchCondition(sqlExpression, condition);
        }

        private Expression ConvertToSearchCondition(SqlExpression sqlExpression, bool condition)
        {
            if (!condition)
            {
                return BuildCompareToExpression(sqlExpression);
            }
            return sqlExpression;
        }

        private Expression ConvertToValue(SqlExpression sqlExpression, bool condition)
        {
            if (condition)
            {
                if (sqlExpression is SqlUnaryExpression unary && unary.OperatorType == ExpressionType.Not)
                {
                    return _sqlExpressionFactory.Case(new CaseWhenClause[] { new CaseWhenClause(unary.Operand, _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Constant(false, null))) }, _sqlExpressionFactory.Constant(true, null));
                }
                return _sqlExpressionFactory.Case(new CaseWhenClause[] { new CaseWhenClause(sqlExpression, _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Constant(true, null))) }, _sqlExpressionFactory.Constant(false, null));
            }
            return sqlExpression;
        }

        private SqlExpression BuildCompareToExpression(SqlExpression sqlExpression)
        {
            return _sqlExpressionFactory.Equal(sqlExpression, _sqlExpressionFactory.Constant(1, null));
        }

        protected override Expression VisitCase(CaseExpression caseExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            bool operandIsSearchCondition = caseExpression.Operand == null;
            _isSearchCondition = false;
            SqlExpression operand = (SqlExpression)Visit(caseExpression.Operand);
            List<CaseWhenClause> list = new List<CaseWhenClause>();
            foreach (CaseWhenClause whenClause in caseExpression.WhenClauses)
            {
                _isSearchCondition = operandIsSearchCondition;
                SqlExpression test = (SqlExpression)Visit(whenClause.Test);
                _isSearchCondition = false;
                SqlExpression result = (SqlExpression)Visit(whenClause.Result);
                list.Add(new CaseWhenClause(test, result));
            }
            _isSearchCondition = false;
            SqlExpression elseResult = (SqlExpression)Visit(caseExpression.ElseResult);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(caseExpression.Update(operand, list, elseResult), condition: false);
        }

        protected override Expression VisitColumn(ColumnExpression columnExpression)
        {
            return ApplyConversion(columnExpression, condition: false);
        }

        protected override Expression VisitExists(ExistsExpression existsExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SelectExpression subquery = (SelectExpression)Visit(existsExpression.Subquery);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(existsExpression.Update(subquery), condition: true);
        }

        protected override Expression VisitFromSql(FromSqlExpression fromSqlExpression)
        {
            return fromSqlExpression;
        }

        protected override Expression VisitIn(InExpression inExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression item = (SqlExpression)Visit(inExpression.Item);
            SelectExpression subquery = (SelectExpression)Visit(inExpression.Subquery);
            IReadOnlyList<SqlExpression> values = inExpression.Values;
            SqlExpression[] newValues = null;
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    SqlExpression original = values[i];
                    SqlExpression visited = (SqlExpression)Visit(original);
                    if (!object.Equals(visited, original) && newValues == null)
                    {
                        newValues = new SqlExpression[values.Count];
                        for (int j = 0; j < i; j++)
                        {
                            newValues[j] = values[j];
                        }
                    }
                    if (newValues != null)
                    {
                        newValues[i] = visited;
                    }
                }
            }
            SqlParameterExpression valuesParameter = (SqlParameterExpression)Visit(inExpression.ValuesParameter);
            _isSearchCondition = isSearchCondition;
            IReadOnlyList<SqlExpression> finalValues = newValues;
            return ApplyConversion(inExpression.Update(item, subquery, finalValues ?? values, valuesParameter), condition: true);
        }

        protected override Expression VisitLike(LikeExpression likeExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression match = (SqlExpression)Visit(likeExpression.Match);
            SqlExpression pattern = (SqlExpression)Visit(likeExpression.Pattern);
            SqlExpression escapeChar = (SqlExpression)Visit(likeExpression.EscapeChar);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(likeExpression.Update(match, pattern, escapeChar), condition: true);
        }

        protected override Expression VisitSelect(SelectExpression selectExpression)
        {
            bool flag = false;
            bool isSearchCondition = _isSearchCondition;
            List<ProjectionExpression> list = new List<ProjectionExpression>();
            _isSearchCondition = false;
            foreach (ProjectionExpression item in selectExpression.Projection)
            {
                ProjectionExpression projection = (ProjectionExpression)Visit(item);
                list.Add(projection);
                flag |= !object.Equals(projection, item);
            }
            List<TableExpressionBase> list2 = new List<TableExpressionBase>();
            foreach (TableExpressionBase table in selectExpression.Tables)
            {
                TableExpressionBase visitedTable = (TableExpressionBase)Visit(table);
                flag |= !object.Equals(visitedTable, table);
                list2.Add(visitedTable);
            }
            _isSearchCondition = true;
            SqlExpression predicate = (SqlExpression)Visit(selectExpression.Predicate);
            flag |= !object.Equals(predicate, selectExpression.Predicate);
            List<SqlExpression> list3 = new List<SqlExpression>();
            _isSearchCondition = false;
            foreach (SqlExpression item2 in selectExpression.GroupBy)
            {
                SqlExpression groupByExpr = (SqlExpression)Visit(item2);
                flag |= !object.Equals(groupByExpr, item2);
                list3.Add(groupByExpr);
            }
            _isSearchCondition = true;
            SqlExpression having = (SqlExpression)Visit(selectExpression.Having);
            flag |= !object.Equals(having, selectExpression.Having);
            List<OrderingExpression> list4 = new List<OrderingExpression>();
            _isSearchCondition = false;
            foreach (OrderingExpression ordering in selectExpression.Orderings)
            {
                SqlExpression orderingExpr = (SqlExpression)Visit(ordering.Expression);
                flag |= !object.Equals(orderingExpr, ordering.Expression);
                list4.Add(ordering.Update(orderingExpr));
            }
            SqlExpression offset = (SqlExpression)Visit(selectExpression.Offset);
            flag |= !object.Equals(offset, selectExpression.Offset);
            SqlExpression limit = (SqlExpression)Visit(selectExpression.Limit);
            flag |= !object.Equals(limit, selectExpression.Limit);
            _isSearchCondition = isSearchCondition;
            if (!flag)
            {
                return selectExpression;
            }
            return selectExpression.Update(list2, predicate, list3, having, list, list4, offset, limit);
        }

        protected override Expression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            ExpressionType operatorType = sqlBinaryExpression.OperatorType;
            if (operatorType == ExpressionType.AndAlso || operatorType == ExpressionType.OrElse)
            {
                _isSearchCondition = true;
            }
            else
            {
                _isSearchCondition = false;
            }
            SqlExpression left = (SqlExpression)Visit(sqlBinaryExpression.Left);
            SqlExpression right = (SqlExpression)Visit(sqlBinaryExpression.Right);
            _isSearchCondition = isSearchCondition;
            sqlBinaryExpression = sqlBinaryExpression.Update(left, right);
            bool condition = sqlBinaryExpression.OperatorType == ExpressionType.AndAlso || sqlBinaryExpression.OperatorType == ExpressionType.OrElse || sqlBinaryExpression.OperatorType == ExpressionType.Equal || sqlBinaryExpression.OperatorType == ExpressionType.NotEqual || sqlBinaryExpression.OperatorType == ExpressionType.GreaterThan || sqlBinaryExpression.OperatorType == ExpressionType.GreaterThanOrEqual || sqlBinaryExpression.OperatorType == ExpressionType.LessThan || sqlBinaryExpression.OperatorType == ExpressionType.LessThanOrEqual;
            return ApplyConversion(sqlBinaryExpression, condition);
        }

        protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            bool condition;
            switch (sqlUnaryExpression.OperatorType)
            {
                case ExpressionType.Not:
                    _isSearchCondition = true;
                    condition = true;
                    break;
                case ExpressionType.Convert:
                case ExpressionType.Negate:
                    _isSearchCondition = false;
                    condition = false;
                    break;
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    _isSearchCondition = false;
                    condition = true;
                    break;
                default:
                    throw new InvalidOperationException("Unknown operator type encountered in SqlUnaryExpression.");
            }
            SqlExpression operand = (SqlExpression)Visit(sqlUnaryExpression.Operand);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(sqlUnaryExpression.Update(operand), condition);
        }

        protected override Expression VisitSqlConstant(SqlConstantExpression sqlConstantExpression)
        {
            return ApplyConversion(sqlConstantExpression, condition: false);
        }

        protected override Expression VisitSqlFragment(SqlFragmentExpression sqlFragmentExpression)
        {
            return sqlFragmentExpression;
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression instance = (SqlExpression)Visit(sqlFunctionExpression.Instance);
            SqlExpression[] args = new SqlExpression[sqlFunctionExpression.Arguments.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = (SqlExpression)Visit(sqlFunctionExpression.Arguments[i]);
            }
            _isSearchCondition = isSearchCondition;
            SqlFunctionExpression sqlExpression = sqlFunctionExpression.Update(instance, (IReadOnlyList<SqlExpression>)args);
            bool condition = string.Equals(sqlFunctionExpression.Name, "FREETEXT") || string.Equals(sqlFunctionExpression.Name, "CONTAINS");
            return ApplyConversion(sqlExpression, condition);
        }

        protected override Expression VisitSqlParameter(SqlParameterExpression sqlParameterExpression)
        {
            return ApplyConversion(sqlParameterExpression, condition: false);
        }

        protected override Expression VisitTable(TableExpression tableExpression)
        {
            return tableExpression;
        }

        protected override Expression VisitProjection(ProjectionExpression projectionExpression)
        {
            SqlExpression expression = (SqlExpression)Visit(projectionExpression.Expression);
            return projectionExpression.Update(expression);
        }

        protected override Expression VisitOrdering(OrderingExpression orderingExpression)
        {
            SqlExpression expression = (SqlExpression)Visit(orderingExpression.Expression);
            return orderingExpression.Update(expression);
        }

        protected override Expression VisitCrossJoin(CrossJoinExpression crossJoinExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            TableExpressionBase table = (TableExpressionBase)Visit(crossJoinExpression.Table);
            _isSearchCondition = isSearchCondition;
            return crossJoinExpression.Update(table);
        }

        protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            TableExpressionBase table = (TableExpressionBase)Visit((crossApplyExpression).Table);
            _isSearchCondition = isSearchCondition;
            return crossApplyExpression.Update(table);
        }

        protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            TableExpressionBase table = (TableExpressionBase)Visit((outerApplyExpression).Table);
            _isSearchCondition = isSearchCondition;
            return outerApplyExpression.Update(table);
        }

        protected override Expression VisitInnerJoin(InnerJoinExpression innerJoinExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            TableExpressionBase table = (TableExpressionBase)Visit((innerJoinExpression).Table);
            _isSearchCondition = true;
            SqlExpression joinPredicate = (SqlExpression)Visit((innerJoinExpression).JoinPredicate);
            _isSearchCondition = isSearchCondition;
            return innerJoinExpression.Update(table, joinPredicate);
        }

        protected override Expression VisitLeftJoin(LeftJoinExpression leftJoinExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            TableExpressionBase table = (TableExpressionBase)Visit((leftJoinExpression).Table);
            _isSearchCondition = true;
            SqlExpression joinPredicate = (SqlExpression)Visit((leftJoinExpression).JoinPredicate);
            _isSearchCondition = isSearchCondition;
            return leftJoinExpression.Update(table, joinPredicate);
        }

        protected override Expression VisitRightJoin(RightJoinExpression rightJoinExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            TableExpressionBase table = (TableExpressionBase)Visit((rightJoinExpression).Table);
            _isSearchCondition = true;
            SqlExpression joinPredicate = (SqlExpression)Visit((rightJoinExpression).JoinPredicate);
            _isSearchCondition = isSearchCondition;
            return rightJoinExpression.Update(table, joinPredicate);
        }

        protected override Expression VisitRowValue(RowValueExpression rowValueExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression[] array = new SqlExpression[rowValueExpression.Values.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (SqlExpression)Visit(rowValueExpression.Values[i]);
            }
            _isSearchCondition = isSearchCondition;
            return rowValueExpression.Update((IReadOnlyList<SqlExpression>)array);
        }

        protected override Expression VisitScalarSubquery(ScalarSubqueryExpression scalarSubqueryExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            SelectExpression subquery = (SelectExpression)Visit(scalarSubqueryExpression.Subquery);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(scalarSubqueryExpression.Update(subquery), condition: false);
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            List<SqlExpression> list = new List<SqlExpression>();
            foreach (SqlExpression partition in rowNumberExpression.Partitions)
            {
                SqlExpression item = (SqlExpression)Visit(partition);
                list.Add(item);
            }
            List<OrderingExpression> list2 = new List<OrderingExpression>();
            foreach (OrderingExpression ordering in rowNumberExpression.Orderings)
            {
                OrderingExpression item2 = (OrderingExpression)Visit(ordering);
                list2.Add(item2);
            }
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(rowNumberExpression.Update((IReadOnlyList<SqlExpression>)list, (IReadOnlyList<OrderingExpression>)list2), condition: false);
        }

        protected override Expression VisitExcept(ExceptExpression exceptExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SelectExpression source1 = (SelectExpression)Visit((exceptExpression).Source1);
            SelectExpression source2 = (SelectExpression)Visit((exceptExpression).Source2);
            _isSearchCondition = isSearchCondition;
            return exceptExpression.Update(source1, source2);
        }

        protected override Expression VisitIntersect(IntersectExpression intersectExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SelectExpression source1 = (SelectExpression)Visit((intersectExpression).Source1);
            SelectExpression source2 = (SelectExpression)Visit((intersectExpression).Source2);
            _isSearchCondition = isSearchCondition;
            return intersectExpression.Update(source1, source2);
        }

        protected override Expression VisitUnion(UnionExpression unionExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SelectExpression source1 = (SelectExpression)Visit((unionExpression).Source1);
            SelectExpression source2 = (SelectExpression)Visit((unionExpression).Source2);
            _isSearchCondition = isSearchCondition;
            return unionExpression.Update(source1, source2);
        }

        protected override Expression VisitCollate(CollateExpression collateExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression operand = (SqlExpression)Visit(collateExpression.Operand);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(collateExpression.Update(operand), condition: false);
        }

        protected override Expression VisitDistinct(DistinctExpression distinctExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression operand = (SqlExpression)Visit(distinctExpression.Operand);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(distinctExpression.Update(operand), condition: false);
        }

        protected override Expression VisitTableValuedFunction(TableValuedFunctionExpression tableValuedFunctionExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression[] args = new SqlExpression[tableValuedFunctionExpression.Arguments.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = (SqlExpression)Visit(tableValuedFunctionExpression.Arguments[i]);
            }
            _isSearchCondition = isSearchCondition;
            return tableValuedFunctionExpression.Update((IReadOnlyList<SqlExpression>)args);
        }

        protected override Expression VisitAtTimeZone(AtTimeZoneExpression atTimeZoneExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SqlExpression operand = (SqlExpression)Visit(atTimeZoneExpression.Operand);
            SqlExpression timeZone = (SqlExpression)Visit(atTimeZoneExpression.TimeZone);
            _isSearchCondition = isSearchCondition;
            return ApplyConversion(atTimeZoneExpression.Update(operand, timeZone), condition: false);
        }

        protected override Expression VisitDelete(DeleteExpression deleteExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SelectExpression selectExpression = (SelectExpression)Visit(deleteExpression.SelectExpression);
            _isSearchCondition = isSearchCondition;
            return deleteExpression.Update(deleteExpression.Table, selectExpression);
        }

        protected override Expression VisitUpdate(UpdateExpression updateExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            SelectExpression selectExpression = (SelectExpression)Visit(updateExpression.SelectExpression);
            List<ColumnValueSetter> list = new List<ColumnValueSetter>();
            foreach (ColumnValueSetter columnValueSetter in updateExpression.ColumnValueSetters)
            {
                ColumnValueSetter current = columnValueSetter;
                _isSearchCondition = false;
                ColumnExpression column = (ColumnExpression)Visit((current).Column);
                _isSearchCondition = false;
                SqlExpression value = (SqlExpression)Visit((current).Value);
                list.Add(new ColumnValueSetter(column, value));
            }
            _isSearchCondition = isSearchCondition;
            return updateExpression.Update(selectExpression, list);
        }

        protected override Expression VisitJsonScalar(JsonScalarExpression jsonScalarExpression)
        {
            return ApplyConversion(jsonScalarExpression, condition: false);
        }

        protected override Expression VisitValues(ValuesExpression valuesExpression)
        {
            bool isSearchCondition = _isSearchCondition;
            _isSearchCondition = false;
            RowValueExpression[] array = new RowValueExpression[valuesExpression.RowValues.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (RowValueExpression)Visit(valuesExpression.RowValues[i]);
            }
            _isSearchCondition = isSearchCondition;
            return valuesExpression.Update((IReadOnlyList<RowValueExpression>)array);
        }
    }
}
