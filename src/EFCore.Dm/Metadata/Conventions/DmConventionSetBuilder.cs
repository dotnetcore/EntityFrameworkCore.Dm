using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    public class DmConventionSetBuilder : RelationalConventionSetBuilder
    {
        private readonly ISqlGenerationHelper _sqlGenerationHelper;

        public DmConventionSetBuilder([NotNull] ProviderConventionSetBuilderDependencies dependencies, [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies, [NotNull] ISqlGenerationHelper sqlGenerationHelper)
            : base(dependencies, relationalDependencies)
        {
            _sqlGenerationHelper = sqlGenerationHelper;
        }

        public override ConventionSet CreateConventionSet()
        {
            ConventionSet conventionSet = base.CreateConventionSet();
            DmValueGenerationStrategyConvention valueGenerationStrategyConvention = new(Dependencies, RelationalDependencies);
            conventionSet.ModelInitializedConventions.Add(valueGenerationStrategyConvention);
            conventionSet.ModelInitializedConventions.Add(new RelationalMaxIdentifierLengthConvention(128, Dependencies, RelationalDependencies));
            RelationalValueGenerationConvention valueGenerationConvention = new DmValueGenerationConvention(Dependencies, RelationalDependencies);
            ReplaceConvention(conventionSet.EntityTypeBaseTypeChangedConventions, valueGenerationConvention);
            ReplaceConvention(conventionSet.EntityTypeAnnotationChangedConventions, valueGenerationConvention);
            ReplaceConvention(conventionSet.EntityTypePrimaryKeyChangedConventions, valueGenerationConvention);
            ReplaceConvention(conventionSet.ForeignKeyAddedConventions, valueGenerationConvention);
            ReplaceConvention(conventionSet.ForeignKeyRemovedConventions, valueGenerationConvention);
            StoreGenerationConvention storeGenerationConvention = new DmStoreGenerationConvention(Dependencies, RelationalDependencies);
            ReplaceConvention(conventionSet.PropertyAnnotationChangedConventions, storeGenerationConvention);
            ReplaceConvention(conventionSet.PropertyAnnotationChangedConventions, valueGenerationConvention);
            conventionSet.ModelFinalizingConventions.Add(valueGenerationStrategyConvention);
            ReplaceConvention(conventionSet.ModelFinalizingConventions, storeGenerationConvention);
            return conventionSet;
        }

        public static ConventionSet Build()
        {
            using var serviceScope = CreateServiceScope();
            using var context = serviceScope.ServiceProvider.GetRequiredService<DbContext>();
            return ConventionSet.CreateConventionSet(context);
        }

        private static IServiceScope CreateServiceScope()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkDm()
                .AddDbContext<DbContext>(
                    (p, o) =>
                        o.UseDm("Server=.")
                            .UseInternalServiceProvider(p))
                .BuildServiceProvider();

            return serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}
