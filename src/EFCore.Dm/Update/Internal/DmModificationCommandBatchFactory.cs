using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Dm.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Utilities;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Dm.Update.Internal
{
    public class DmModificationCommandBatchFactory : IModificationCommandBatchFactory
    {
        private readonly ModificationCommandBatchFactoryDependencies _dependencies;

        private readonly IDbContextOptions _options;

        public DmModificationCommandBatchFactory([NotNull] ModificationCommandBatchFactoryDependencies dependencies, [NotNull] IDbContextOptions options)
        {
            Check.NotNull(dependencies, "dependencies");
            Check.NotNull(options, "options");
            _dependencies = dependencies;
            _options = options;
        }

        public virtual ModificationCommandBatch Create()
        {
            DmOptionsExtension dmOptionsExtension = _options.Extensions.OfType<DmOptionsExtension>().FirstOrDefault();
            return new DmModificationCommandBatch(_dependencies, (dmOptionsExtension != null) ? dmOptionsExtension.MaxBatchSize : null);
        }
    }
}
