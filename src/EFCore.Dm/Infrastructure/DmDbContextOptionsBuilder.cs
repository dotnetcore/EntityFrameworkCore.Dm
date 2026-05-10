using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Dm.Infrastructure.Internal;
using System;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    public class DmDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<DmDbContextOptionsBuilder, DmOptionsExtension>
    {
        public DmDbContextOptionsBuilder([NotNull] DbContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
        }

        public virtual DmDbContextOptionsBuilder EnableRetryOnFailure()
        {
            return ExecutionStrategy(c => new DmRetryingExecutionStrategy(c));
        }

        public virtual DmDbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount)
        {
            return ExecutionStrategy(c => new DmRetryingExecutionStrategy(c, maxRetryCount));
        }

        public virtual DmDbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount, TimeSpan maxRetryDelay, [NotNull] ICollection<int> errorNumbersToAdd)
        {
            ICollection<int> errorNumbersToAdd2 = errorNumbersToAdd;
            return ExecutionStrategy(c => new DmRetryingExecutionStrategy(c, maxRetryCount, maxRetryDelay, errorNumbersToAdd2));
        }
    }
}
