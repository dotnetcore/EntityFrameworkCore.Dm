using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    public class DmStringMethodTranslator : IMethodCallTranslator
    {
        private static readonly MethodInfo IndexOfMethodInfo = typeof(string).GetRuntimeMethod("IndexOf", new Type[1] { typeof(string) });

        private static readonly MethodInfo IndexOfMethodInfoWithStartingPosition = typeof(string).GetRuntimeMethod("IndexOf", new Type[2]
        {
            typeof(string),
            typeof(int)
        });

        private static readonly MethodInfo ReplaceMethodInfo = typeof(string).GetRuntimeMethod("Replace", new Type[2]
        {
            typeof(string),
            typeof(string)
        });

        private static readonly MethodInfo ToLowerMethodInfo = typeof(string).GetRuntimeMethod("ToLower", Type.EmptyTypes);

        private static readonly MethodInfo ToUpperMethodInfo = typeof(string).GetRuntimeMethod("ToUpper", Type.EmptyTypes);

        private static readonly MethodInfo SubstringMethodInfoWithOneArg = typeof(string).GetRuntimeMethod("Substring", new Type[1] { typeof(int) });

        private static readonly MethodInfo SubstringMethodInfoWithTwoArgs = typeof(string).GetRuntimeMethod("Substring", new Type[2]
        {
            typeof(int),
            typeof(int)
        });

        private static readonly MethodInfo IsNullOrEmptyMethodInfo = typeof(string).GetRuntimeMethod("IsNullOrEmpty", new Type[1] { typeof(string) });

        private static readonly MethodInfo IsNullOrWhiteSpaceMethodInfo = typeof(string).GetRuntimeMethod("IsNullOrWhiteSpace", new Type[1] { typeof(string) });

        private static readonly MethodInfo TrimStartMethodInfoWithoutArgs = typeof(string).GetRuntimeMethod("TrimStart", Type.EmptyTypes);

        private static readonly MethodInfo TrimEndMethodInfoWithoutArgs = typeof(string).GetRuntimeMethod("TrimEnd", Type.EmptyTypes);

        private static readonly MethodInfo TrimMethodInfoWithoutArgs = typeof(string).GetRuntimeMethod("Trim", Type.EmptyTypes);

        private static readonly MethodInfo TrimStartMethodInfoWithCharArrayArg = typeof(string).GetRuntimeMethod("TrimStart", new Type[1] { typeof(char[]) });

        private static readonly MethodInfo TrimEndMethodInfoWithCharArrayArg = typeof(string).GetRuntimeMethod("TrimEnd", new Type[1] { typeof(char[]) });

        private static readonly MethodInfo TrimMethodInfoWithCharArrayArg = typeof(string).GetRuntimeMethod("Trim", new Type[1] { typeof(char[]) });

        private static readonly MethodInfo TrimStartMethodInfoWithCharArg = typeof(string).GetRuntimeMethod("TrimStart", new Type[1] { typeof(char) });

        private static readonly MethodInfo TrimEndMethodInfoWithCharArg = typeof(string).GetRuntimeMethod("TrimEnd", new Type[1] { typeof(char) });

        private static readonly MethodInfo FirstOrDefaultMethodInfoWithoutArgs = typeof(Enumerable).GetRuntimeMethods().Single((MethodInfo m) => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1).MakeGenericMethod(typeof(char));

        private static readonly MethodInfo LastOrDefaultMethodInfoWithoutArgs = typeof(Enumerable).GetRuntimeMethods().Single((MethodInfo m) => m.Name == "LastOrDefault" && m.GetParameters().Length == 1).MakeGenericMethod(typeof(char));

        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public DmStringMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public virtual SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (instance != null)
            {
                if (IndexOfMethodInfo.Equals(method))
                {
                    return TranslateIndexOf(instance, method, arguments[0], null);
                }

                if (IndexOfMethodInfoWithStartingPosition.Equals(method))
                {
                    return TranslateIndexOf(instance, method, arguments[0], arguments[1]);
                }

                if (ReplaceMethodInfo.Equals(method))
                {
                    SqlExpression oldValue = arguments[0];
                    SqlExpression newValue = arguments[1];
                    RelationalTypeMapping typeMapping = ExpressionExtensions.InferTypeMapping(new SqlExpression[3] { instance, oldValue, newValue });
                    instance = _sqlExpressionFactory.ApplyTypeMapping(instance, typeMapping);
                    oldValue = _sqlExpressionFactory.ApplyTypeMapping(oldValue, typeMapping);
                    newValue = _sqlExpressionFactory.ApplyTypeMapping(newValue, typeMapping);
                    return _sqlExpressionFactory.Function("REPLACE", new SqlExpression[3] { instance, oldValue, newValue }, true, new bool[3] { true, true, true }, method.ReturnType, typeMapping);
                }

                if (ToLowerMethodInfo.Equals(method) || ToUpperMethodInfo.Equals(method))
                    return _sqlExpressionFactory.Function(ToLowerMethodInfo.Equals(method) ? "LOWER" : "UPPER", (IEnumerable<SqlExpression>)new SqlExpression[1] { instance }, true, new bool[1] { true }, method.ReturnType, instance!.TypeMapping);

                if (SubstringMethodInfoWithOneArg.Equals(method))
                    return _sqlExpressionFactory.Function("SUBSTRING", new SqlExpression[3]
                    {
                        instance,
                        _sqlExpressionFactory.Add(arguments[0], _sqlExpressionFactory.Constant(1, null), null),
                        _sqlExpressionFactory.Function("LEN", new SqlExpression[1] { instance }, true, new bool[1] { true }, typeof(int), null)
                    }, true, new bool[3] { true, true, true }, method.ReturnType, instance!.TypeMapping);

                if (SubstringMethodInfoWithTwoArgs.Equals(method))
                    return _sqlExpressionFactory.Function("SUBSTRING", new SqlExpression[3]
                    {
                        instance,
                        _sqlExpressionFactory.Add(arguments[0], _sqlExpressionFactory.Constant(1, null), null),
                        arguments[1]
                    }, true, new bool[3] { true, true, true }, method.ReturnType, instance!.TypeMapping);

                // TrimStart → LTRIM
                if (method == TrimStartMethodInfoWithoutArgs
                    || method == TrimStartMethodInfoWithCharArg
                    || (method == TrimStartMethodInfoWithCharArrayArg && IsEmptyCharArray(arguments[0])))
                {
                    return ProcessTrimStartEnd(instance, arguments, "LTRIM");
                }

                // TrimEnd → RTRIM
                if (method == TrimEndMethodInfoWithoutArgs
                    || method == TrimEndMethodInfoWithCharArg
                    || (method == TrimEndMethodInfoWithCharArrayArg && IsEmptyCharArray(arguments[0])))
                {
                    return ProcessTrimStartEnd(instance, arguments, "RTRIM");
                }

                // Trim → LTRIM(RTRIM(...))
                if (method == TrimMethodInfoWithoutArgs
                    || (method == TrimMethodInfoWithCharArrayArg && IsEmptyCharArray(arguments[0])))
                    return _sqlExpressionFactory.Function("LTRIM", (IEnumerable<SqlExpression>)new SqlExpression[1]
                    {
                        _sqlExpressionFactory.Function("RTRIM", new SqlExpression[1] { instance }, true, new bool[1] { true }, instance!.Type, instance!.TypeMapping)
                    }, true, new bool[1] { true }, instance!.Type, instance!.TypeMapping);
            }

            if (IsNullOrEmptyMethodInfo.Equals(method))
            {
                SqlExpression arg = arguments[0];
                return _sqlExpressionFactory.OrElse(_sqlExpressionFactory.IsNull(arg), _sqlExpressionFactory.Like(arg, _sqlExpressionFactory.Constant(string.Empty, (RelationalTypeMapping)null), (SqlExpression)null));
            }

            if (IsNullOrWhiteSpaceMethodInfo.Equals(method))
            {
                SqlExpression arg = arguments[0];
                return _sqlExpressionFactory.OrElse(_sqlExpressionFactory.IsNull(arg), _sqlExpressionFactory.Equal(arg, _sqlExpressionFactory.Constant(string.Empty, arg.TypeMapping)));
            }

            if (FirstOrDefaultMethodInfoWithoutArgs.Equals(method))
            {
                SqlExpression arg = arguments[0];
                return _sqlExpressionFactory.Function("SUBSTRING", (IEnumerable<SqlExpression>)new SqlExpression[3]
                {
                    arg,
                    _sqlExpressionFactory.Constant(1, null),
                    _sqlExpressionFactory.Constant(1, null)
                }, true, new bool[3] { true, true, true }, method.ReturnType, null);
            }

            if (LastOrDefaultMethodInfoWithoutArgs.Equals(method))
            {
                SqlExpression arg = arguments[0];
                return _sqlExpressionFactory.Function("SUBSTRING", (IEnumerable<SqlExpression>)new SqlExpression[3]
                {
                    arg,
                    _sqlExpressionFactory.Function("LEN", (IEnumerable<SqlExpression>)new SqlExpression[1] { arg }, true, new bool[1] { true }, typeof(int), null),
                    _sqlExpressionFactory.Constant(1, null)
                }, true, new bool[3] { true, true, true }, method.ReturnType, null);
            }

            return null;
        }

        private static bool IsEmptyCharArray(SqlExpression argument)
            => argument is SqlConstantExpression { Value: char[] arr } && arr.Length == 0;

        private SqlExpression TranslateIndexOf(SqlExpression instance, MethodInfo method, SqlExpression searchExpression, SqlExpression? startIndex)
        {
            RelationalTypeMapping typeMapping = ExpressionExtensions.InferTypeMapping(new SqlExpression[2] { instance, searchExpression });
            searchExpression = _sqlExpressionFactory.ApplyTypeMapping(searchExpression, typeMapping);
            instance = _sqlExpressionFactory.ApplyTypeMapping(instance, typeMapping);
            List<SqlExpression> list = new List<SqlExpression> { searchExpression, instance };

            if (startIndex != null)
            {
                SqlExpression item;
                if (startIndex is SqlConstantExpression startIndexConst && startIndexConst.Value is int num)
                    item = _sqlExpressionFactory.Constant(num + 1, typeof(int), null);
                else
                    item = _sqlExpressionFactory.Add(startIndex, _sqlExpressionFactory.Constant(1, null), null);
                list.Add(item);
            }

            IEnumerable<bool> enumerable = Enumerable.Repeat(element: true, list.Count);
            string storeType = typeMapping.StoreType;
            SqlExpression positionResult;
            if (string.Equals(storeType, "nvarchar(max)", StringComparison.OrdinalIgnoreCase) || string.Equals(storeType, "varchar(max)", StringComparison.OrdinalIgnoreCase))
            {
                positionResult = _sqlExpressionFactory.Function("POSITION", (IEnumerable<SqlExpression>)list, true, enumerable, typeof(long), null);
                positionResult = _sqlExpressionFactory.Convert(positionResult, typeof(int), null);
            }
            else
            {
                positionResult = _sqlExpressionFactory.Function("POSITION", (IEnumerable<SqlExpression>)list, true, enumerable, method.ReturnType, null);
            }

            SqlConstantExpression searchConst = searchExpression as SqlConstantExpression;
            if (searchConst != null)
            {
                string text = searchConst.Value as string;
                if (text != null && text == "")
                    return _sqlExpressionFactory.Case(new CaseWhenClause[] { new CaseWhenClause(_sqlExpressionFactory.IsNotNull(instance), _sqlExpressionFactory.Constant(0, null)) }, null);
            }

            SqlExpression offset = ((searchExpression is SqlConstantExpression) ? _sqlExpressionFactory.Constant(1, null) : _sqlExpressionFactory.Case((IReadOnlyList<CaseWhenClause>)new CaseWhenClause[1]
            {
                new CaseWhenClause(_sqlExpressionFactory.Equal(searchExpression, _sqlExpressionFactory.Constant(string.Empty, typeMapping)), _sqlExpressionFactory.Constant(0, null))
            }, _sqlExpressionFactory.Constant(1, null)));
            return _sqlExpressionFactory.Subtract(positionResult, offset, null);
        }

        private SqlExpression? ProcessTrimStartEnd(SqlExpression instance, IReadOnlyList<SqlExpression> arguments, string functionName)
        {
            SqlExpression trimChars = null;
            if (arguments.Count > 0)
            {
                SqlExpression arg = arguments[0];
                if (arg is SqlConstantExpression constArg)
                {
                    object value = constArg.Value;
                    SqlExpression charExpr;
                    if (value is char c)
                    {
                        charExpr = _sqlExpressionFactory.Constant(c.ToString(), instance.TypeMapping);
                    }
                    else
                    {
                        char[] array = value as char[];
                        if (array == null)
                        {
                            throw new UnreachableException("Invalid parameter type for string.TrimStart/TrimEnd");
                        }
                        charExpr = _sqlExpressionFactory.Constant(new string(array), instance.TypeMapping);
                    }
                    trimChars = charExpr;
                }
            }
            ISqlExpressionFactory sqlExpressionFactory = _sqlExpressionFactory;
            IEnumerable<SqlExpression> enumerable2 = (trimChars != null)
                ? new SqlExpression[] { instance, trimChars }
                : new SqlExpression[] { instance };
            IEnumerable<bool> enumerable4 = (trimChars != null)
                ? new bool[] { true, true }
                : new bool[] { true };
            return sqlExpressionFactory.Function(functionName, enumerable2, true, enumerable4, instance.Type, instance.TypeMapping);
        }
    }
}
