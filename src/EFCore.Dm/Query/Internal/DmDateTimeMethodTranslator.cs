using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    public class DmDateTimeMethodTranslator : IMethodCallTranslator
    {
        private readonly Dictionary<MethodInfo, string> _methodInfoDatePartMapping = new Dictionary<MethodInfo, string>
        {
            {
                typeof(DateTime).GetRuntimeMethod("AddYears", new Type[1] { typeof(int) }),
                "year"
            },
            {
                typeof(DateTime).GetRuntimeMethod("AddMonths", new Type[1] { typeof(int) }),
                "month"
            },
            {
                typeof(DateTime).GetRuntimeMethod("AddDays", new Type[1] { typeof(double) }),
                "day"
            },
            {
                typeof(DateTime).GetRuntimeMethod("AddHours", new Type[1] { typeof(double) }),
                "hour"
            },
            {
                typeof(DateTime).GetRuntimeMethod("AddMinutes", new Type[1] { typeof(double) }),
                "minute"
            },
            {
                typeof(DateTime).GetRuntimeMethod("AddSeconds", new Type[1] { typeof(double) }),
                "second"
            },
            {
                typeof(DateTime).GetRuntimeMethod("AddMilliseconds", new Type[1] { typeof(double) }),
                "millisecond"
            },
            {
                typeof(DateTimeOffset).GetRuntimeMethod("AddYears", new Type[1] { typeof(int) }),
                "year"
            },
            {
                typeof(DateTimeOffset).GetRuntimeMethod("AddMonths", new Type[1] { typeof(int) }),
                "month"
            },
            {
                typeof(DateTimeOffset).GetRuntimeMethod("AddDays", new Type[1] { typeof(double) }),
                "day"
            },
            {
                typeof(DateTimeOffset).GetRuntimeMethod("AddHours", new Type[1] { typeof(double) }),
                "hour"
            },
            {
                typeof(DateTimeOffset).GetRuntimeMethod("AddMinutes", new Type[1] { typeof(double) }),
                "minute"
            },
            {
                typeof(DateTimeOffset).GetRuntimeMethod("AddSeconds", new Type[1] { typeof(double) }),
                "second"
            },
            {
                typeof(DateTimeOffset).GetRuntimeMethod("AddMilliseconds", new Type[1] { typeof(double) }),
                "millisecond"
            }
        };

        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public DmDateTimeMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public virtual SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (_methodInfoDatePartMapping.TryGetValue(method, out var value))
            {
                if (!value.Equals("year") && !value.Equals("month"))
                {
                    if (arguments[0] is SqlConstantExpression sqlConstant && ((double)sqlConstant.Value >= 2147483647.0 || (double)sqlConstant.Value <= -2147483648.0))
                    {
                        return null;
                    }
                }
                return _sqlExpressionFactory.Function("DATEADD", new SqlExpression[]
                {
                    _sqlExpressionFactory.Fragment(value),
                    _sqlExpressionFactory.Convert(arguments[0], typeof(int), null),
                    instance
                }, true, new bool[3] { false, true, true }, instance.Type, instance.TypeMapping);
            }
            return null;
        }
    }
}
