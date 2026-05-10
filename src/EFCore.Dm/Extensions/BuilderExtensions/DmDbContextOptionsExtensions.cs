using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Dm.Storage.Interceptors;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Utilities;
using System;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore
{
    public static class DmDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseDm(this DbContextOptionsBuilder optionsBuilder, Action<DmDbContextOptionsBuilder>? dmOptionsAction = null)
        {
            Check.NotNull(optionsBuilder, "optionsBuilder");

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
                .AddOrUpdateExtension(GetOrCreateExtension(optionsBuilder));
            ConfigureWarnings(optionsBuilder);
            dmOptionsAction?.Invoke(new DmDbContextOptionsBuilder(optionsBuilder));
            optionsBuilder.AddInterceptors(new DmIdentityInsertInterceptor());
            return optionsBuilder;
        }

        public static DbContextOptionsBuilder UseDm([NotNull] this DbContextOptionsBuilder optionsBuilder, [NotNull] string connectionString, [CanBeNull] Action<DmDbContextOptionsBuilder>? dmOptionsAction = null)
        {
            Check.NotNull(optionsBuilder, "optionsBuilder");
            Check.NotEmpty(connectionString, "connectionString");

            DmOptionsExtension dmOptionsExtension = (DmOptionsExtension)(GetOrCreateExtension(optionsBuilder)).WithConnectionString(connectionString);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(dmOptionsExtension);
            ConfigureWarnings(optionsBuilder);
            dmOptionsAction?.Invoke(new DmDbContextOptionsBuilder(optionsBuilder));
            optionsBuilder.AddInterceptors(new DmIdentityInsertInterceptor());
            return optionsBuilder;
        }

        public static DbContextOptionsBuilder UseDm([NotNull] this DbContextOptionsBuilder optionsBuilder, [NotNull] DbConnection connection, [CanBeNull] Action<DmDbContextOptionsBuilder>? dmOptionsAction = null)
        {
            Check.NotNull(optionsBuilder, "optionsBuilder");
            Check.NotNull(connection, "connection");
            DmOptionsExtension dmOptionsExtension = (DmOptionsExtension)(GetOrCreateExtension(optionsBuilder)).WithConnection(connection);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(dmOptionsExtension);
            ConfigureWarnings(optionsBuilder);
            dmOptionsAction?.Invoke(new DmDbContextOptionsBuilder(optionsBuilder));
            optionsBuilder.AddInterceptors(new DmIdentityInsertInterceptor());
            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseDm<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder, [NotNull] string connectionString, [CanBeNull] Action<DmDbContextOptionsBuilder>? dmOptionsAction = null) where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)((DbContextOptionsBuilder)optionsBuilder).UseDm(connectionString, dmOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseDm<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder, [NotNull] DbConnection connection, [CanBeNull] Action<DmDbContextOptionsBuilder>? dmOptionsAction = null) where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)((DbContextOptionsBuilder)optionsBuilder).UseDm(connection, dmOptionsAction);
        }

        private static DmOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        {
            return (DmOptionsExtension)((optionsBuilder.Options.FindExtension<DmOptionsExtension>()) ?? ((new DmOptionsExtension()).WithMinBatchSize(4)));
        }

        private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
        {
            CoreOptionsExtension coreOptions = ((optionsBuilder.Options.FindExtension<CoreOptionsExtension>()) ?? (new CoreOptionsExtension()));
            coreOptions = coreOptions.WithWarningsConfiguration(coreOptions.WarningsConfiguration.TryWithExplicit(RelationalEventId.AmbientTransactionWarning, (WarningBehavior)2));
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptions);
        }
    }
}
