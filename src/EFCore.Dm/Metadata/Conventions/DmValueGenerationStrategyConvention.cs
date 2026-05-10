using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    public class DmValueGenerationStrategyConvention : IModelInitializedConvention, IConvention, IModelFinalizingConvention
    {
        protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

        protected virtual RelationalConventionSetBuilderDependencies RelationalDependencies { get; }

        public DmValueGenerationStrategyConvention([NotNull] ProviderConventionSetBuilderDependencies dependencies, [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies)
        {
            Dependencies = dependencies;
            RelationalDependencies = relationalDependencies;
        }

        public virtual void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            modelBuilder.HasValueGenerationStrategy(DmValueGenerationStrategy.IdentityColumn);
        }

        public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            var readOnlyModel = (IReadOnlyModel)modelBuilder.Metadata;
            foreach (IConventionEntityType entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                foreach (IConventionProperty declaredProperty in entityType.GetDeclaredProperties())
                {
                    var readOnlyProp = (IReadOnlyProperty)declaredProperty;
                    DmValueGenerationStrategy? dmValueGenerationStrategy = null;
                    StoreObjectIdentifier tableStoreObject = RelationalPropertyExtensions.GetMappedStoreObjects(readOnlyProp, (StoreObjectType)0).FirstOrDefault();
                    if (tableStoreObject.Name != null)
                    {
                        dmValueGenerationStrategy = readOnlyProp.GetValueGenerationStrategy(in tableStoreObject, Dependencies.TypeMappingSource);
                        if (dmValueGenerationStrategy == DmValueGenerationStrategy.None && !IsStrategyNoneNeeded(readOnlyProp, tableStoreObject))
                        {
                            dmValueGenerationStrategy = null;
                        }
                    }
                    else
                    {
                        StoreObjectIdentifier viewStoreObject = RelationalPropertyExtensions.GetMappedStoreObjects(readOnlyProp, (StoreObjectType)1).FirstOrDefault();
                        if (viewStoreObject.Name != null)
                        {
                            dmValueGenerationStrategy = readOnlyProp.GetValueGenerationStrategy(in viewStoreObject, Dependencies.TypeMappingSource);
                            if (dmValueGenerationStrategy == DmValueGenerationStrategy.None && !IsStrategyNoneNeeded(readOnlyProp, viewStoreObject))
                            {
                                dmValueGenerationStrategy = null;
                            }
                        }
                    }
                    if (dmValueGenerationStrategy.HasValue && tableStoreObject.Name != null)
                    {
                        declaredProperty.Builder.HasValueGenerationStrategy(dmValueGenerationStrategy);
                        if (dmValueGenerationStrategy.GetValueOrDefault() == DmValueGenerationStrategy.Sequence)
                        {
                            IConventionSequence sequence = RelationalModelBuilderExtensions.HasSequence(modelBuilder, readOnlyProp.GetSequenceName(in tableStoreObject) ?? ((entityType.GetRootType()).ShortName() + readOnlyModel.GetSequenceNameSuffix()), readOnlyProp.GetSequenceSchema(in tableStoreObject) ?? readOnlyModel.GetSequenceSchema(), false).Metadata;
                            RelationalPropertyBuilderExtensions.HasDefaultValueSql(declaredProperty.Builder, RelationalDependencies.UpdateSqlGenerator.GenerateObtainNextSequenceValueOperation((sequence).Name, (sequence).Schema), false);
                        }
                    }
                }
            }
            bool IsStrategyNoneNeeded(IReadOnlyProperty property, StoreObjectIdentifier storeObject)
            {
                if ((int)property.ValueGenerated == 1 && !RelationalPropertyExtensions.TryGetDefaultValue(property, in storeObject, out _) && RelationalPropertyExtensions.GetDefaultValueSql(property, in storeObject) == null && RelationalPropertyExtensions.GetComputedColumnSql(property, in storeObject) == null && property.DeclaringType.Model.GetValueGenerationStrategy()
                    .GetValueOrDefault() == DmValueGenerationStrategy.IdentityColumn)
                {
                    ValueConverter converter = property.GetValueConverter();
                    if (converter == null)
                    {
                        CoreTypeMapping typeMapping = RelationalPropertyExtensions.FindRelationalTypeMapping(property, in storeObject) ?? Dependencies.TypeMappingSource.FindMapping((IProperty)property);
                        converter = typeMapping?.Converter;
                    }
                    Type type = converter?.ProviderClrType.UnwrapNullableType();
                    return type != null && (type.IsInteger() || type == typeof(decimal));
                }
                return false;
            }
        }
    }
}
