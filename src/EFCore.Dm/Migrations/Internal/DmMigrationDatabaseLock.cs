using Dm;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Dm.Migrations.Internal
{
    public class DmMigrationDatabaseLock : IMigrationsDatabaseLock, IDisposable, IAsyncDisposable
    {
        public readonly DmConnection Connection;

        public IHistoryRepository HistoryRepository { get; set; }

        public DmMigrationDatabaseLock(IHistoryRepository historyRepository, HistoryRepositoryDependencies dependencies)
        {
            HistoryRepository = historyRepository;
            Connection = (DmConnection)dependencies.Connection.DbConnection;
            if (Connection == null || Connection.State == ConnectionState.Closed)
            {
                throw new InvalidOperationException("lock对应连接无效，上锁失败");
            }
            DmCommand command = (DmCommand)Connection.CreateCommand();
            command.CommandText = "LOCK TABLE \"__EFMigrationsHistory\" IN EXCLUSIVE MODE;";
            command.ExecuteNonQuery();
            command.Close();
        }

        public void Dispose()
        {
            Connection.Close();
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.CloseAsync();
        }
    }
}
