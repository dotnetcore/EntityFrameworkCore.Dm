using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Dm.Migrations.Internal
{
	public class DmHistoryRepository : HistoryRepository
	{
		protected override string ExistsSql
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("SELECT ID FROM SYS.SYSOBJECTS WHERE ");
				if (TableSchema != null)
				{
					stringBuilder.Append(" SCHID = (SELECT ID FROM SYS.SYSOBJECTS WHERE TYPE$ = 'SCH' AND NAME = ").Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(TableSchema)).Append(") AND ");
				}
				else
				{
					stringBuilder.Append(" SCHID = (SELECT ID FROM SYS.SYSOBJECTS WHERE TYPE$ = 'SCH' AND NAME = ").Append(" (SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'))").Append(") AND ");
				}
				stringBuilder.Append("NAME = ").Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(TableName)).Append(";");
				return stringBuilder.ToString();
			}
		}

		public override LockReleaseBehavior LockReleaseBehavior => default;

		public DmHistoryRepository([NotNull] HistoryRepositoryDependencies dependencies)
			: base(dependencies)
		{
		}

		protected override bool InterpretExistsResult(object value)
		{
			return value != null;
		}

		public override string GetCreateIfNotExistsScript()
		{
			IndentedStringBuilder isb = new IndentedStringBuilder();
			isb.AppendLine("DECLARE CNT INT;\nBEGIN").Append(" SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END INTO CNT FROM SYSOBJECTS WHERE NAME = ").Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(TableName))
				.Append(" ");
			if (TableSchema != null)
			{
				isb.Append(" AND ").Append(" SCHID = (SELECT ID FROM SYS.SYSOBJECTS WHERE TYPE$ = 'SCH' AND NAME = ").Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(TableSchema))
					.Append(") ");
			}
			else
			{
				isb.Append(" AND ").Append(" SCHID = (SELECT ID FROM SYS.SYSOBJECTS WHERE TYPE$ = 'SCH' AND NAME = ").Append(" (SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'))")
					.Append(") ");
			}
			isb.AppendLine(";");
			using (isb.Indent())
			{
				isb.AppendLine("IF CNT == 0 THEN").Append("EXECUTE IMMEDIATE '").AppendLines(GetCreateScript() + "';", false)
					.AppendLine("END IF;");
			}
			isb.AppendLine("END;");
			return isb.ToString();
		}

		public override string GetBeginIfNotExistsScript(string migrationId)
		{
			Check.NotEmpty(migrationId, "migrationId");
			return new StringBuilder().AppendLine("DECLARE CNT INT;\nBEGIN").Append(" SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END INTO CNT FROM ").Append(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
				.Append(" WHERE ")
				.Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
				.Append(" = ")
				.Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(migrationId))
				.AppendLine(";")
				.AppendLine("IF CNT == 0 THEN ")
				.ToString();
		}

		public override string GetBeginIfExistsScript(string migrationId)
		{
			Check.NotEmpty(migrationId, "migrationId");
			return new StringBuilder().AppendLine("DECLARE ").AppendLine("   CNT INT;").AppendLine("BEGIN ")
				.Append("SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END INTO CNT FROM ")
				.Append(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
				.Append(" WHERE ")
				.Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
				.Append(" = ")
				.Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(migrationId))
				.AppendLine(";")
				.AppendLine("IF CNT == 1 THEN ")
				.ToString();
		}

		public override string GetEndIfScript()
		{
			return new StringBuilder().Append("END").AppendLine(SqlGenerationHelper.StatementTerminator).ToString();
		}

		public override IMigrationsDatabaseLock AcquireDatabaseLock()
		{
			RelationalLoggerExtensions.AcquiringMigrationLock(Dependencies.MigrationsLogger);
			return new DmMigrationDatabaseLock(this, Dependencies);
		}

		public override Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			RelationalLoggerExtensions.AcquiringMigrationLock(Dependencies.MigrationsLogger);
			return Task.FromResult<IMigrationsDatabaseLock>(new DmMigrationDatabaseLock(this, Dependencies));
		}
	}
}
