using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    public class DmTimeOnlyMethodTranslator : IMethodCallTranslator
    {
        public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (method.DeclaringType == typeof(TimeOnly) && method.Name == "FromDateTime")
            {
                return new SqlFunctionExpression("TIMEONLY.FROMDATETIME", (IEnumerable<SqlExpression>)new SqlExpression[1] { arguments[0] }, true, new bool[1] { true }, typeof(TimeOnly), null);
            }
            if (method.DeclaringType == typeof(TimeOnly) && method.Name == "FromTimeSpan")
            {
                return new SqlFunctionExpression("TIMEONLY.FROMTIMESPAN", (IEnumerable<SqlExpression>)new SqlExpression[1] { arguments[0] }, true, new bool[1] { true }, typeof(TimeOnly), null);
            }
            return null;
        }
    }
}
