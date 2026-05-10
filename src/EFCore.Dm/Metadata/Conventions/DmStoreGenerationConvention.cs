using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    public class DmStoreGenerationConvention : StoreGenerationConvention
    {
        public DmStoreGenerationConvention([NotNull] ProviderConventionSetBuilderDependencies dependencies, [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
        }

        public override void ProcessPropertyAnnotationChanged(IConventionPropertyBuilder propertyBuilder, string name, IConventionAnnotation annotation, IConventionAnnotation oldAnnotation, IConventionContext<IConventionAnnotation> context)
        {
            if (annotation == null || (oldAnnotation != null && oldAnnotation.Value != null))
            {
                return;
            }
            bool flag = (int)annotation.GetConfigurationSource() != 2;
            switch (name)
            {
                case "Relational:DefaultValue":
                    if (propertyBuilder.HasValueGenerationStrategy(null, flag) == null && RelationalPropertyBuilderExtensions.HasDefaultValue(propertyBuilder, null, flag) != null)
                    {
                        context.StopProcessing();
                        return;
                    }
                    break;
                case "Relational:DefaultValueSql":
                    if (propertyBuilder.Metadata.GetValueGenerationStrategy() != DmValueGenerationStrategy.Sequence && propertyBuilder.HasValueGenerationStrategy(null, flag) == null && RelationalPropertyBuilderExtensions.HasDefaultValueSql(propertyBuilder, (string)null, flag) != null)
                    {
                        context.StopProcessing();
                        return;
                    }
                    break;
                case "Relational:ComputedColumnSql":
                    if (propertyBuilder.HasValueGenerationStrategy(null, flag) == null && RelationalPropertyBuilderExtensions.HasComputedColumnSql(propertyBuilder, (string)null, flag) != null)
                    {
                        context.StopProcessing();
                        return;
                    }
                    break;
                case "Dm:ValueGenerationStrategy":
                    if (((propertyBuilder.Metadata.GetValueGenerationStrategy() != DmValueGenerationStrategy.Sequence && (RelationalPropertyBuilderExtensions.HasDefaultValue(propertyBuilder, null, flag) == null || RelationalPropertyBuilderExtensions.HasDefaultValueSql(propertyBuilder, (string)null, flag) == null || RelationalPropertyBuilderExtensions.HasComputedColumnSql(propertyBuilder, (string)null, flag) == null)) || RelationalPropertyBuilderExtensions.HasDefaultValue(propertyBuilder, null, flag) == null || RelationalPropertyBuilderExtensions.HasComputedColumnSql(propertyBuilder, (string)null, flag) == null) && propertyBuilder.HasValueGenerationStrategy(null, flag) != null)
                    {
                        context.StopProcessing();
                        return;
                    }
                    break;
            }
            base.ProcessPropertyAnnotationChanged(propertyBuilder, name, annotation, oldAnnotation, context);
        }

        protected override void Validate(IConventionProperty property, in StoreObjectIdentifier storeObject)
        {
            if (property.GetValueGenerationStrategyConfigurationSource().HasValue)
            {
                DmValueGenerationStrategy valueGenerationStrategy = property.GetValueGenerationStrategy(in storeObject);
                if (valueGenerationStrategy == DmValueGenerationStrategy.None)
                {
                    base.Validate(property, in storeObject);
                    return;
                }
                if (RelationalPropertyExtensions.GetDefaultValue(property, in storeObject) != null)
                {
                    Dependencies.ValidationLogger.ConflictingValueGenerationStrategiesWarning(valueGenerationStrategy, "DefaultValue", (IReadOnlyProperty)property);
                }
                if (RelationalPropertyExtensions.GetDefaultValueSql(property, in storeObject) != null)
                {
                    Dependencies.ValidationLogger.ConflictingValueGenerationStrategiesWarning(valueGenerationStrategy, "DefaultValueSql", (IReadOnlyProperty)property);
                }
                if (RelationalPropertyExtensions.GetComputedColumnSql(property, in storeObject) != null)
                {
                    Dependencies.ValidationLogger.ConflictingValueGenerationStrategiesWarning(valueGenerationStrategy, "ComputedColumnSql", (IReadOnlyProperty)property);
                }
            }
            base.Validate(property, in storeObject);
        }
    }
}
