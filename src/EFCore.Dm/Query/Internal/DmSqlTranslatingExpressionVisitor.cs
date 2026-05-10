using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    public class DmSqlTranslatingExpressionVisitor : RelationalSqlTranslatingExpressionVisitor
    {
        [EntityFrameworkInternal]
        public enum StartsEndsWithContains
        {
            StartsWith,
            EndsWith,
            Contains
        }

        private readonly QueryCompilationContext _queryCompilationContext;

        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        private readonly IRelationalTypeMappingSource _typeMappingSource;

        private static readonly HashSet<string> DateTimeDataTypes = new HashSet<string> { "time", "date", "datetime", "datetime2", "datetimeoffset" };

        private static readonly HashSet<Type> DateTimeClrTypes = new HashSet<Type>
        {
            typeof(TimeOnly),
            typeof(DateOnly),
            typeof(TimeSpan),
            typeof(DateTime),
            typeof(DateTimeOffset)
        };

        private static readonly HashSet<ExpressionType> ArithmeticOperatorTypes = new HashSet<ExpressionType>
        {
            ExpressionType.Add,
            ExpressionType.Subtract,
            ExpressionType.Multiply,
            ExpressionType.Divide,
            ExpressionType.Modulo
        };

        private static readonly MethodInfo StringStartsWithMethodInfo = typeof(string).GetRuntimeMethod("StartsWith", new Type[1] { typeof(string) });

        private static readonly MethodInfo StringEndsWithMethodInfo = typeof(string).GetRuntimeMethod("EndsWith", new Type[1] { typeof(string) });

        private static readonly MethodInfo StringContainsMethodInfo = typeof(string).GetRuntimeMethod("Contains", new Type[1] { typeof(string) });

        private static readonly MethodInfo StringJoinMethodInfo = typeof(string).GetRuntimeMethod("Join", new Type[2]
        {
            typeof(string),
            typeof(string[])
        });

        private static readonly MethodInfo EscapeLikePatternParameterMethod = typeof(DmSqlTranslatingExpressionVisitor).GetTypeInfo().GetDeclaredMethod("ConstructLikePatternParameter");

        private const char LikeEscapeChar = '\\';

        private const string LikeEscapeString = "\\";

        public DmSqlTranslatingExpressionVisitor(RelationalSqlTranslatingExpressionVisitorDependencies dependencies, QueryCompilationContext queryCompilationContext, QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
            : base(dependencies, queryCompilationContext, queryableMethodTranslatingExpressionVisitor)
        {
            _queryCompilationContext = queryCompilationContext;
            _sqlExpressionFactory = dependencies.SqlExpressionFactory;
            _typeMappingSource = dependencies.TypeMappingSource;
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            if (binaryExpression.NodeType == ExpressionType.ArrayIndex && binaryExpression.Left.Type == typeof(byte[]))
            {
                return TranslateByteArrayElementAccess(binaryExpression.Left, binaryExpression.Right, binaryExpression.Type);
            }
            Expression expression = base.VisitBinary(binaryExpression);
            if (expression is SqlBinaryExpression sqlBinary && ArithmeticOperatorTypes.Contains(sqlBinary.OperatorType))
            {
                string text = GetProviderType(sqlBinary.Left) ?? GetProviderType(sqlBinary.Right);
                if (text != null)
                {
                    if (DateTimeDataTypes.Contains(text))
                    {
                        return QueryCompilationContext.NotTranslatedExpression;
                    }
                }
                else
                {
                    Type type = sqlBinary.Left.Type;
                    Type type2 = sqlBinary.Right.Type;
                    if (DateTimeClrTypes.Contains(type) || DateTimeClrTypes.Contains(type2))
                    {
                        return QueryCompilationContext.NotTranslatedExpression;
                    }
                }
            }
            return expression;
        }

        protected override Expression VisitUnary(UnaryExpression unaryExpression)
        {
            return base.VisitUnary(unaryExpression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            MethodInfo method = methodCallExpression.Method;
            if (method == StringStartsWithMethodInfo && TryTranslateStartsEndsWithContains(methodCallExpression.Object, methodCallExpression.Arguments[0], StartsEndsWithContains.StartsWith, out var translation2))
            {
                return translation2;
            }
            if (method == StringEndsWithMethodInfo && TryTranslateStartsEndsWithContains(methodCallExpression.Object, methodCallExpression.Arguments[0], StartsEndsWithContains.EndsWith, out var translation3))
            {
                return translation3;
            }
            if (method == StringContainsMethodInfo && TryTranslateStartsEndsWithContains(methodCallExpression.Object, methodCallExpression.Arguments[0], StartsEndsWithContains.Contains, out var translation4))
            {
                return translation4;
            }
            if (method == StringJoinMethodInfo)
            {
                if (methodCallExpression.Arguments[1] is NewArrayExpression newArrayExpression)
                {
                    if (TranslationFailed(methodCallExpression.Arguments[0], Visit(methodCallExpression.Arguments[0]), out var castTranslation))
                    {
                        return QueryCompilationContext.NotTranslatedExpression;
                    }
                    SqlExpression[] array = new SqlExpression[newArrayExpression.Expressions.Count + 1];
                    array[0] = castTranslation;
                    RelationalTypeMapping typeMapping = castTranslation.TypeMapping;
                    bool flag = typeMapping != null && typeMapping.IsUnicode;
                    for (int i = 0; i < newArrayExpression.Expressions.Count; i++)
                    {
                        Expression expression = newArrayExpression.Expressions[i];
                        if (TranslationFailed(expression, Visit(expression), out var castTranslation2))
                        {
                            return QueryCompilationContext.NotTranslatedExpression;
                        }
                        RelationalTypeMapping typeMapping2 = castTranslation2.TypeMapping;
                        if (typeMapping2 != null && typeMapping2.IsUnicode)
                        {
                            flag = true;
                        }
                        array[i + 1] = Dependencies.SqlExpressionFactory.Coalesce(castTranslation2, _sqlExpressionFactory.Constant(string.Empty, null), null);
                    }
                    return Dependencies.SqlExpressionFactory.Function("CONCAT_WS", (IEnumerable<SqlExpression>)array, false, new bool[array.Length], typeof(string), _typeMappingSource.FindMapping(flag ? "nvarchar(max)" : "varchar(max)"));
                }
            }
            return base.VisitMethodCall(methodCallExpression);
            bool TryTranslateStartsEndsWithContains(Expression instance, Expression pattern, StartsEndsWithContains methodType, [NotNullWhen(true)] out SqlExpression? translation)
            {
                SqlExpression translatedInstance = Visit(instance) as SqlExpression;
                SqlExpression translatedPattern;
                RelationalTypeMapping stringTypeMapping;
                SqlExpression PositionGreaterThanZero()
                {
                    return _sqlExpressionFactory.GreaterThan(_sqlExpressionFactory.Function("POSITION", (IEnumerable<SqlExpression>)new SqlExpression[2] { translatedPattern, translatedInstance }, true, new bool[2] { true, true }, typeof(int), null), _sqlExpressionFactory.Constant(0, null));
                }
                if (translatedInstance != null)
                {
                    translatedPattern = Visit(pattern) as SqlExpression;
                    if (translatedPattern != null)
                    {
                        stringTypeMapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(new SqlExpression[2] { translatedInstance, translatedPattern });
                        translatedInstance = _sqlExpressionFactory.ApplyTypeMapping(translatedInstance, stringTypeMapping);
                        translatedPattern = _sqlExpressionFactory.ApplyTypeMapping(translatedPattern, stringTypeMapping);
                        if (translatedPattern is SqlConstantExpression constantPattern)
                        {
                            object value = constantPattern.Value;
                            SqlExpression likeExpression;
                            if (value != null)
                            {
                                if (value is not string text)
                                {
                                    throw new UnreachableException();
                                }
                                if (text == "")
                                {
                                    likeExpression = _sqlExpressionFactory.Like(translatedInstance, _sqlExpressionFactory.Constant("%", (RelationalTypeMapping)null), (SqlExpression)null);
                                }
                                else if (!text.Any(IsLikeWildChar))
                                {
                                    ISqlExpressionFactory sqlExpressionFactory = _sqlExpressionFactory;
                                    SqlExpression left = translatedInstance;
                                    ISqlExpressionFactory sqlExpressionFactory2 = _sqlExpressionFactory;
                                    likeExpression = sqlExpressionFactory.Like(left, sqlExpressionFactory2.Constant(methodType switch
                                    {
                                        StartsEndsWithContains.StartsWith => text + "%",
                                        StartsEndsWithContains.EndsWith => "%" + text,
                                        StartsEndsWithContains.Contains => "%" + text + "%",
                                        _ => throw new ArgumentOutOfRangeException("methodType", methodType, null),
                                    }, null), null);
                                }
                                else
                                {
                                    string pattern2 = text;
                                    ISqlExpressionFactory sqlExpressionFactory2 = _sqlExpressionFactory;
                                    SqlExpression left = translatedInstance;
                                    ISqlExpressionFactory sqlExpressionFactory = _sqlExpressionFactory;
                                    likeExpression = sqlExpressionFactory2.Like(left, sqlExpressionFactory.Constant(methodType switch
                                    {
                                        StartsEndsWithContains.StartsWith => EscapeLikePattern(pattern2) + "%",
                                        StartsEndsWithContains.EndsWith => "%" + EscapeLikePattern(pattern2),
                                        StartsEndsWithContains.Contains => "%" + EscapeLikePattern(pattern2) + "%",
                                        _ => throw new ArgumentOutOfRangeException("methodType", methodType, null),
                                    }, null), _sqlExpressionFactory.Constant("\\", null));
                                }
                            }
                            else
                            {
                                likeExpression = _sqlExpressionFactory.Like(translatedInstance, _sqlExpressionFactory.Constant(null, typeof(string), stringTypeMapping), null);
                            }
                            translation = likeExpression;
                            return true;
                        }
                        if (translatedPattern is SqlParameterExpression paramExpr && paramExpr.Name.StartsWith("__", StringComparison.Ordinal))
                        {
                            LambdaExpression lambdaExpression = Expression.Lambda(Expression.Call(EscapeLikePatternParameterMethod, QueryCompilationContext.QueryContextParameter, Expression.Constant(paramExpr.Name), Expression.Constant(methodType)), QueryCompilationContext.QueryContextParameter);
                            var parameterExpression = _queryCompilationContext.RegisterRuntimeParameter(paramExpr.Name + "_" + methodType.ToString().ToLower(CultureInfo.InvariantCulture), lambdaExpression);
                            translation = _sqlExpressionFactory.Like(translatedInstance, new SqlParameterExpression(parameterExpression.Name, parameterExpression.Type, stringTypeMapping), _sqlExpressionFactory.Constant("\\", null));
                            return true;
                        }
                        translation = TranslateWithoutLike();
                        return true;
                    }
                }
                translation = null;
                return false;
                SqlExpression TranslateWithoutLike(bool patternIsNonEmptyConstantString = false)
                {
                    switch (methodType)
                    {
                        case StartsEndsWithContains.StartsWith:
                        case StartsEndsWithContains.EndsWith:
                            return _sqlExpressionFactory.AndAlso(_sqlExpressionFactory.IsNotNull(translatedInstance), _sqlExpressionFactory.AndAlso(_sqlExpressionFactory.IsNotNull(translatedPattern), _sqlExpressionFactory.Equal(_sqlExpressionFactory.Function((methodType == StartsEndsWithContains.StartsWith) ? "LEFT" : "RIGHT", new SqlExpression[2]
                            {
                            translatedInstance,
                            _sqlExpressionFactory.Function("LEN", new SqlExpression[1] { translatedPattern }, true, new bool[1] { true }, typeof(int), null)
                            }, true, new bool[2] { true, true }, typeof(string), stringTypeMapping), translatedPattern)));
                        case StartsEndsWithContains.Contains:
                            if (patternIsNonEmptyConstantString)
                            {
                                return _sqlExpressionFactory.AndAlso(_sqlExpressionFactory.IsNotNull(translatedInstance), PositionGreaterThanZero());
                            }
                            return _sqlExpressionFactory.AndAlso(_sqlExpressionFactory.IsNotNull(translatedInstance), _sqlExpressionFactory.AndAlso(_sqlExpressionFactory.IsNotNull(translatedPattern), _sqlExpressionFactory.OrElse(PositionGreaterThanZero(), _sqlExpressionFactory.Like(translatedPattern, _sqlExpressionFactory.Constant(string.Empty, stringTypeMapping), null))));
                        default:
                            throw new UnreachableException();
                    }
                }
            }
        }

        [EntityFrameworkInternal]
        public static string? ConstructLikePatternParameter(QueryContext queryContext, string baseParameterName, StartsEndsWithContains methodType)
        {
            object paramValue = queryContext.Parameters[baseParameterName];
            if (paramValue != null)
            {
                string text = paramValue as string;
                if (text != null)
                {
                    if (text == "")
                    {
                        return "%";
                    }
                    return methodType switch
                    {
                        StartsEndsWithContains.StartsWith => EscapeLikePattern(text) + "%",
                        StartsEndsWithContains.EndsWith => "%" + EscapeLikePattern(text),
                        StartsEndsWithContains.Contains => "%" + EscapeLikePattern(text) + "%",
                        _ => throw new ArgumentOutOfRangeException("methodType", methodType, null),
                    };
                }
                throw new UnreachableException();
            }
            return null;
        }

        private static bool IsLikeWildChar(char c)
        {
            if (c == '%' || c == '[' || c == '_')
            {
                return true;
            }
            return false;
        }

        private static string EscapeLikePattern(string pattern)
        {
            int i;
            for (i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (IsLikeWildChar(c) || c == '\\')
                {
                    break;
                }
            }
            if (i == pattern.Length)
            {
                return pattern;
            }
            StringBuilder stringBuilder = new StringBuilder(pattern, 0, i, pattern.Length + 10);
            for (; i < pattern.Length; i++)
            {
                char c2 = pattern[i];
                if (IsLikeWildChar(c2) || c2 == '\\')
                {
                    stringBuilder.Append('\\');
                }
                stringBuilder.Append(c2);
            }
            return stringBuilder.ToString();
        }

        public override SqlExpression? GenerateGreatest(IReadOnlyList<SqlExpression> expressions, Type resultType)
        {
            RelationalTypeMapping typeMapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(expressions);
            return _sqlExpressionFactory.Function("GREATEST", (IEnumerable<SqlExpression>)expressions, true, Enumerable.Repeat(element: false, expressions.Count), resultType, typeMapping);
        }

        public override SqlExpression? GenerateLeast(IReadOnlyList<SqlExpression> expressions, Type resultType)
        {
            RelationalTypeMapping typeMapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(expressions);
            return _sqlExpressionFactory.Function("LEAST", (IEnumerable<SqlExpression>)expressions, true, Enumerable.Repeat(element: false, expressions.Count), resultType, typeMapping);
        }

        private Expression TranslateByteArrayElementAccess(Expression array, Expression index, Type resultType)
        {
            SqlExpression sqlArray = Visit(array) as SqlExpression;
            if (sqlArray != null)
            {
                SqlExpression sqlIndex = Visit(index) as SqlExpression;
                if (sqlIndex != null)
                {
                    return Dependencies.SqlExpressionFactory.Convert(Dependencies.SqlExpressionFactory.Function("SUBSTRING", new SqlExpression[3]
                    {
                        sqlArray,
                        Dependencies.SqlExpressionFactory.Add(Dependencies.SqlExpressionFactory.ApplyDefaultTypeMapping(sqlIndex), Dependencies.SqlExpressionFactory.Constant(1, null), null),
                        Dependencies.SqlExpressionFactory.Constant(1, null)
                    }, true, new bool[3] { true, true, true }, typeof(byte[]), null), resultType, null);
                }
            }
            return QueryCompilationContext.NotTranslatedExpression;
        }

        [DebuggerStepThrough]
        private static bool TranslationFailed(Expression? original, Expression? translation, out SqlExpression? castTranslation)
        {
            if (original != null && translation is not SqlExpression)
            {
                castTranslation = null;
                return true;
            }
            castTranslation = translation as SqlExpression;
            return false;
        }

        private static string? GetProviderType(SqlExpression expression)
        {
            RelationalTypeMapping typeMapping = expression.TypeMapping;
            if (typeMapping == null)
            {
                return null;
            }
            return typeMapping.StoreType;
        }
    }
}
