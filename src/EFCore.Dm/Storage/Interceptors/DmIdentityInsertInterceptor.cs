using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Interceptors;

internal class DmIdentityInsertInterceptor : DbCommandInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (string.IsNullOrEmpty(command.CommandText))
        {
            return result;
        }
        var tables = eventData.Context.GetIdentityInsertTable();
        if (tables == null || tables.Count <= 0)
        {
            return result;
        }
        var changeCommandText = tables.WrapIdentityInsert(command.CommandText);
        if (string.IsNullOrEmpty(changeCommandText))
        {
            return result;
        }
        command.CommandText = changeCommandText;
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)

    {
        if (string.IsNullOrEmpty(command.CommandText))
        {
            return new(result);
        }
        var tables = eventData.Context.GetIdentityInsertTable();
        if (tables == null || tables.Count <= 0)
        {
            return new(result);
        }
        var changeCommandText = tables.WrapIdentityInsert(command.CommandText);
        if (string.IsNullOrEmpty(changeCommandText))
        {
            return new(result);
        }
        command.CommandText = changeCommandText;
        return new(result);
    }
}
