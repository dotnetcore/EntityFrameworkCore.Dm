using Dm;
using Microsoft.EntityFrameworkCore.Dm.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
    public class DmDatabaseCreator : RelationalDatabaseCreator
    {
        private readonly IDmRelationalConnection _connection;

        private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

        public DmDatabaseCreator(RelationalDatabaseCreatorDependencies dependencies, IDmRelationalConnection connection, IRawSqlCommandBuilder rawSqlCommandBuilder)
            : base(dependencies)
        {
            _connection = connection;
            _rawSqlCommandBuilder = rawSqlCommandBuilder;
        }

        public override void Create()
        {
            DmConnectionStringBuilder val = new DmConnectionStringBuilder(_connection.ConnectionString);
            val.Schema = (string)null;
            bool flag = true;
            DmConnection val2 = new DmConnection(val.ConnectionString, true);
            try
            {
                ((DbConnection)(object)val2).Open();
            }
            catch (Exception)
            {
                flag = false;
            }
            finally
            {
                ((IDisposable)val2)?.Dispose();
            }
            using IDmRelationalConnection dmRelationalConnection = _connection.CreateMasterConnection();
            Dependencies.MigrationCommandExecutor.ExecuteNonQuery((flag ? CreateCreateSchemaOperations() : CreateCreateOperations()), dmRelationalConnection);
        }

        public override async Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            DmConnectionStringBuilder val = new DmConnectionStringBuilder(_connection.ConnectionString);
            val.Schema = (string)null;
            DmConnectionStringBuilder val2 = val;
            bool userExists = true;
            DmConnection connectionWithoutSchema = new DmConnection(((DbConnectionStringBuilder)(object)val2).ConnectionString, true);
            try
            {
                await ((DbConnection)(object)connectionWithoutSchema).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (Exception)
            {
                userExists = false;
            }
            finally
            {
                if (connectionWithoutSchema != null)
                {
                    await ((DbConnection)(object)connectionWithoutSchema).DisposeAsync();
                }
            }
            await using IDmRelationalConnection masterConnection = _connection.CreateMasterConnection();
            await Dependencies.MigrationCommandExecutor.ExecuteNonQueryAsync((userExists ? CreateCreateSchemaOperations() : CreateCreateOperations()), masterConnection, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public override bool HasTables()
        {
            return ExecutionStrategyExtensions.Execute(Dependencies.ExecutionStrategy, _connection, ((IDmRelationalConnection connection) => Convert.ToInt64(CreateHasTablesCommand().ExecuteScalar(new RelationalCommandParameterObject(connection, null, null, Dependencies.CurrentContext.Context, Dependencies.CommandLogger))) != 0));
        }

        public override Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ExecutionStrategyExtensions.ExecuteAsync(Dependencies.ExecutionStrategy, _connection, (async (IDmRelationalConnection connection, CancellationToken ct) => (int)(await CreateHasTablesCommand().ExecuteScalarAsync(new RelationalCommandParameterObject(connection, null, null, Dependencies.CurrentContext.Context, Dependencies.CommandLogger), ct).ConfigureAwait(continueOnCapturedContext: false)) != 0), cancellationToken);
        }

        private IRelationalCommand CreateHasTablesCommand()
        {
            return _rawSqlCommandBuilder.Build("SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END FROM SYS.SYSOBJECTS WHERE SCHID = CURRENT_SCHID() AND TYPE$='SCHOBJ' AND SUBTYPE$='UTAB' AND NAME NOT LIKE 'BIN$%'");
        }

        public override void CreateTables()
        {
            try
            {
                IReadOnlyList<MigrationCommand> createTablesCommands = GetCreateTablesCommands(0);
                Dependencies.MigrationCommandExecutor.ExecuteNonQuery(createTablesCommands, Dependencies.Connection);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private IReadOnlyList<MigrationCommand> CreateCreateOperations()
        {
            DmConnectionStringBuilder val = new DmConnectionStringBuilder(_connection.DbConnection.ConnectionString);
            return Dependencies.MigrationsSqlGenerator.Generate(
            [
                new DmCreateUserOperation
                {
                    UserName = val.User,
                    Password = val.Password,
                    Schema = val.Schema
                }
            ], null, 0);
        }

        private IReadOnlyList<MigrationCommand> CreateCreateSchemaOperations()
        {
            DmConnectionStringBuilder val = new DmConnectionStringBuilder((_connection).DbConnection.ConnectionString);
            return Dependencies.MigrationsSqlGenerator.Generate(new DmCreateSchemaOperation[1]
            {
                new DmCreateSchemaOperation
                {
                    UserName = val.User,
                    Schema = val.Schema
                }
            }, null, 0);
        }

        public override bool Exists()
        {
            try
            {
                _connection.Open(true);
                return true;
            }
            catch (DmException)
            {
                return false;
            }
            finally
            {
                _connection.Close();
            }
        }

        public override async Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool result;
                try
                {
                    await _connection.OpenAsync(cancellationToken, true).ConfigureAwait(continueOnCapturedContext: false);
                    result = true;
                }
                catch (DmException)
                {
                    result = false;
                }
                return result;
            }
            finally
            {
                await _connection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public override void Delete()
        {
            using IDmRelationalConnection dmRelationalConnection = _connection.CreateMasterConnection();
            try
            {
                Dependencies.MigrationCommandExecutor.ExecuteNonQuery(CreateDropSchemaCommands(), dmRelationalConnection);
            }
            catch (Exception)
            {
                Dependencies.MigrationCommandExecutor.ExecuteNonQuery(CreateDropUserCommands(), dmRelationalConnection);
            }
        }

        public override async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await using IDmRelationalConnection masterConnection = _connection.CreateMasterConnection();
            try
            {
                await Dependencies.MigrationCommandExecutor.ExecuteNonQueryAsync(CreateDropSchemaCommands(), masterConnection, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (Exception ex)
            {
                _ = ex;
                await Dependencies.MigrationCommandExecutor.ExecuteNonQueryAsync(CreateDropUserCommands(), masterConnection, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private IReadOnlyList<MigrationCommand> CreateDropSchemaCommands()
        {
            string schema = new DmConnectionStringBuilder(_connection.DbConnection.ConnectionString).Schema;
            if (string.IsNullOrEmpty(schema))
            {
                throw new InvalidOperationException(DmStrings.NoUserId);
            }
            MigrationOperation[] array = (MigrationOperation[])(object)new MigrationOperation[1]
            {
                new DmDropSchemaOperation
                {
                    Schema = schema
                }
            };
            return Dependencies.MigrationsSqlGenerator.Generate(array, null, 0);
        }

        private IReadOnlyList<MigrationCommand> CreateDropUserCommands()
        {
            string user = new DmConnectionStringBuilder(_connection.DbConnection.ConnectionString).User;
            if (string.IsNullOrEmpty(user))
            {
                throw new InvalidOperationException(DmStrings.NoUserId);
            }
            MigrationOperation[] array = (MigrationOperation[])(object)new MigrationOperation[1]
            {
                new DmDropUserOperation
                {
                    UserName = user
                }
            };
            return Dependencies.MigrationsSqlGenerator.Generate((IReadOnlyList<MigrationOperation>)array, null, 0);
        }
    }
}
