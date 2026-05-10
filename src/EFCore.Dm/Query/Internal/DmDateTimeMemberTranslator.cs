using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    public class DmDateTimeMemberTranslator : IMemberTranslator
    {
        private static readonly Dictionary<string, string> _datePartMapping = new Dictionary<string, string>
        {
            { "Year", "year" },
            { "Month", "month" },
            { "DayOfYear", "dayofyear" },
            { "Day", "day" },
            { "Hour", "hour" },
            { "Minute", "minute" },
            { "Second", "second" },
            { "Millisecond", "millisecond" }
        };

        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public DmDateTimeMemberTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public virtual SqlExpression Translate(SqlExpression instance, MemberInfo member, Type returnType, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            Type declaringType = member.DeclaringType;
            if (declaringType == typeof(DateTime) || declaringType == typeof(DateTimeOffset))
            {
                string name = member.Name;
                if (_datePartMapping.TryGetValue(name, out var value))
                {
                    return _sqlExpressionFactory.Function("DATEPART", new SqlExpression[]
                    {
                        _sqlExpressionFactory.Fragment(value),
                        instance
                    }, true, new bool[2] { false, true }, returnType, null);
                }
                switch (name)
                {
                    case "Date":
                        return _sqlExpressionFactory.Function("CONVERT", new SqlExpression[]
                        {
                        _sqlExpressionFactory.Fragment("date"),
                        instance
                        }, true, new bool[2] { false, true }, returnType, instance.TypeMapping);
                    case "TimeOfDay":
                        return _sqlExpressionFactory.Convert(instance, returnType, null);
                    case "Now":
                        return _sqlExpressionFactory.Function((declaringType == typeof(DateTime)) ? "GETDATE" : "SYSDATETIMEOFFSET", Array.Empty<SqlExpression>(), false, Array.Empty<bool>(), returnType, null);
                    case "UtcNow":
                        {
                            SqlExpression utcNowExpr = _sqlExpressionFactory.Function((declaringType == typeof(DateTime)) ? "GETUTCDATE" : "SYSUTCDATETIME", Array.Empty<SqlExpression>(), false, Array.Empty<bool>(), returnType, null);
                            if (!(declaringType == typeof(DateTime)))
                            {
                                return _sqlExpressionFactory.Convert(utcNowExpr, returnType, null);
                            }
                            return utcNowExpr;
                        }
                    case "Today":
                        return _sqlExpressionFactory.Function("CONVERT", new SqlExpression[]
                        {
                        _sqlExpressionFactory.Fragment("date"),
                        _sqlExpressionFactory.Function("GETDATE", Array.Empty<SqlExpression>(), false, Array.Empty<bool>(), typeof(DateTime), null)
                        }, true, new bool[2] { false, true }, returnType, null);
                }
            }
            return null;
        }
    }
}
