using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmTransaction : RelationalTransaction
	{
		private static readonly bool _useOldBehavior = AppContext.TryGetSwitch("Microsoft.EntityFrameworkCore.Issue23305", out var isEnabled) && isEnabled;

		public DmTransaction(IRelationalConnection connection, DbTransaction transaction, Guid transactionId, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned, ISqlGenerationHelper sqlGenerationHelper)
			: base(connection, transaction, transactionId, logger, transactionOwned, sqlGenerationHelper)
		{
		}

		public override void ReleaseSavepoint(string name)
		{
		}
	}
}
