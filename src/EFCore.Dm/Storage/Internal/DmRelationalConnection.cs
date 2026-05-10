using Dm;
using Dm.util;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmRelationalConnection : RelationalConnection, IDmRelationalConnection, IRelationalConnection, IRelationalTransactionManager, IDbContextTransactionManager, IResettableService, IDisposable, IAsyncDisposable
	{
		public const string DBAUser = "SYSDBA";

		internal const int DefaultMasterConnectionCommandTimeout = 60;

		protected override bool SupportsAmbientTransactions => true;

		public DmRelationalConnection([NotNull] RelationalConnectionDependencies dependencies)
			: base(dependencies)
		{
		}

		protected override DbConnection CreateDbConnection()
		{
			return new DmConnection(ConnectionString, true);
		}

		public virtual IDmRelationalConnection CreateMasterConnection()
		{
            DmConnectionStringBuilder connectionStringBuilder = new(ConnectionString)
            {
                User = "SYSDBA"
            };
			if (ExtensionUtil.isEmpty(connectionStringBuilder.DBAPassword))
			{
				throw new Exception("未配置SYSBDA用户密码(参数DBAPassword)，无法创建MasterConnection");
			}
			if (connectionStringBuilder.Schema != null)
			{
				connectionStringBuilder.Schema = null;
			}
			connectionStringBuilder.Password = connectionStringBuilder.DBAPassword;
			DbContextOptions options = DmDbContextOptionsExtensions.UseDm(new DbContextOptionsBuilder(), connectionStringBuilder.ConnectionString, b =>
			{
				b.CommandTimeout(CommandTimeout.GetValueOrDefault(60));
			}).Options;
			RelationalConnectionDependencies deps = Dependencies with { ContextOptions = options };
			return new DmRelationalConnection(deps);
		}
	}
}
