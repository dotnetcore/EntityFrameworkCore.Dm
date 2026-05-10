using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    public class DmValueGenerationConvention : RelationalValueGenerationConvention
    {
        public DmValueGenerationConvention([NotNull] ProviderConventionSetBuilderDependencies dependencies, [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
        }

        public override void ProcessPropertyAnnotationChanged(IConventionPropertyBuilder propertyBuilder, string name, IConventionAnnotation annotation, IConventionAnnotation oldAnnotation, IConventionContext<IConventionAnnotation> context)
        {
            if (name == "Dm:ValueGenerationStrategy")
            {
                propertyBuilder.ValueGenerated(GetValueGenerated(propertyBuilder.Metadata), false);
            }
            else
            {
                base.ProcessPropertyAnnotationChanged(propertyBuilder, name, annotation, oldAnnotation, context);
            }
        }

        public override void ProcessEntityTypeAnnotationChanged(IConventionEntityTypeBuilder entityTypeBuilder, string name, IConventionAnnotation? annotation, IConventionAnnotation? oldAnnotation, IConventionContext<IConventionAnnotation> context)
        {
            if (name == "Dm:TemporalPeriodStartPropertyName" || name == "Dm:TemporalPeriodEndPropertyName")
            {
                if ((annotation?.Value) is string periodStartName)
                {
                    IConventionProperty startProp = entityTypeBuilder.Metadata.FindProperty(periodStartName);
                    startProp?.Builder.ValueGenerated(GetValueGenerated(startProp), false);
                    if ((oldAnnotation?.Value) is string oldPeriodName)
                    {
                        IConventionProperty oldProp = entityTypeBuilder.Metadata.FindProperty(oldPeriodName);
                        oldProp?.Builder.ValueGenerated(GetValueGenerated(oldProp), false);
                    }
                }
            }
            base.ProcessEntityTypeAnnotationChanged(entityTypeBuilder, name, annotation, oldAnnotation, context);
        }

        protected override ValueGenerated? GetValueGenerated(IConventionProperty property)
        {
            if (RelationalTypeBaseExtensions.IsMappedToJson(property.DeclaringType) && property.IsOrdinalKeyProperty())
            {
                IConventionTypeBase declaringType = property.DeclaringType;
                if (declaringType is IReadOnlyEntityType entityType && !entityType.FindOwnership().IsUnique)
                {
                    return (ValueGenerated)1;
                }
            }
            StoreObjectIdentifier storeObject = RelationalPropertyExtensions.GetMappedStoreObjects(property, (StoreObjectType)0).FirstOrDefault();
            if (storeObject.Name == null)
            {
                return null;
            }
            return GetValueGenerated(property, in storeObject, Dependencies.TypeMappingSource);
        }

        private static ValueGenerated? GetValueGenerated(IReadOnlyProperty property, in StoreObjectIdentifier storeObject, ITypeMappingSource typeMappingSource)
        {
            ValueGenerated? temporalValueGenerated = GetTemporalValueGenerated(property);
            if (!temporalValueGenerated.HasValue)
            {
                ValueGenerated? valueGenerated = RelationalValueGenerationConvention.GetValueGenerated(property, in storeObject);
                if (!valueGenerated.HasValue)
                {
                    if (property.GetValueGenerationStrategy(in storeObject, typeMappingSource) == DmValueGenerationStrategy.None)
                    {
                        return null;
                    }
                    return (ValueGenerated)1;
                }
                return valueGenerated;
            }
            return temporalValueGenerated;
        }

        private static ValueGenerated? GetTemporalValueGenerated(IReadOnlyProperty property)
        {
            IReadOnlyTypeBase declaringType = property.DeclaringType;
            if (declaringType is IReadOnlyEntityType entityType
                && entityType.IsTemporal()
                && (entityType.GetPeriodStartPropertyName() == property.Name || entityType.GetPeriodEndPropertyName() == property.Name))
            {
                return (ValueGenerated)3;
            }
            return null;
        }
    }
}
