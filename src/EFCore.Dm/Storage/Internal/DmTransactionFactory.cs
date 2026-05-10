using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmTransactionFactory : IRelationalTransactionFactory
	{
		protected virtual RelationalTransactionFactoryDependencies Dependencies { get; }

		public DmTransactionFactory(RelationalTransactionFactoryDependencies dependencies)
		{
			Check.NotNull(dependencies, "dependencies");
			Dependencies = dependencies;
		}

		public virtual RelationalTransaction Create(IRelationalConnection connection, DbTransaction transaction, Guid transactionId, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned)
		{
			return (RelationalTransaction)(object)new DmTransaction(connection, transaction, transactionId, logger, transactionOwned, Dependencies.SqlGenerationHelper);
		}
	}
}
