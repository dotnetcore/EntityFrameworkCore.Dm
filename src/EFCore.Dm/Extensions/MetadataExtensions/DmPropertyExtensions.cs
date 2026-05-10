using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Dm.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Utilities;
using System;
using System.Linq;

namespace Microsoft.EntityFrameworkCore
{
    public static class DmPropertyExtensions
    {
        public static string GetHiLoSequenceName([NotNull] this IReadOnlyProperty property)
        {
            return (string)((IReadOnlyAnnotatable)property)["Dm:HiLoSequenceName"];
        }

        public static void SetHiLoSequenceName([NotNull] this IMutableProperty property, [CanBeNull] string name)
        {
            property.SetOrRemoveAnnotation("Dm:HiLoSequenceName", Check.NullButNotEmpty(name, "name"));
        }

        public static void SetHiLoSequenceName([NotNull] this IConventionProperty property, [CanBeNull] string name, bool fromDataAnnotation = false)
        {
            property.SetOrRemoveAnnotation("Dm:HiLoSequenceName", Check.NullButNotEmpty(name, "name"), fromDataAnnotation);
        }

        public static ConfigurationSource? GetHiLoSequenceNameConfigurationSource([NotNull] this IConventionProperty property)
        {
            IConventionAnnotation annotation = property.FindAnnotation("Dm:HiLoSequenceName");
            if (annotation == null)
            {
                return null;
            }
            return annotation.GetConfigurationSource();
        }

        public static string GetHiLoSequenceSchema([NotNull] this IReadOnlyProperty property)
        {
            return (string)((IReadOnlyAnnotatable)property)["Dm:HiLoSequenceSchema"];
        }

        public static void SetHiLoSequenceSchema([NotNull] this IMutableProperty property, [CanBeNull] string schema)
        {
            property.SetOrRemoveAnnotation("Dm:HiLoSequenceSchema", Check.NullButNotEmpty(schema, "schema"));
        }

        public static void SetHiLoSequenceSchema([NotNull] this IConventionProperty property, [CanBeNull] string schema, bool fromDataAnnotation = false)
        {
            property.SetOrRemoveAnnotation("Dm:HiLoSequenceSchema", Check.NullButNotEmpty(schema, "schema"), fromDataAnnotation);
        }

        public static ConfigurationSource? GetHiLoSequenceSchemaConfigurationSource([NotNull] this IConventionProperty property)
        {
            IConventionAnnotation annotation = property.FindAnnotation("Dm:HiLoSequenceSchema");
            if (annotation == null)
            {
                return null;
            }
            return annotation.GetConfigurationSource();
        }

        public static IReadOnlySequence FindHiLoSequence([NotNull] this IReadOnlyProperty property)
        {
            IReadOnlyModel model = property.DeclaringType.Model;
            if (property.GetValueGenerationStrategy() != DmValueGenerationStrategy.SequenceHiLo)
            {
                return null;
            }
            string sequenceName = property.GetHiLoSequenceName() ?? model.GetHiLoSequenceName();
            string sequenceSchema = property.GetHiLoSequenceSchema() ?? model.GetHiLoSequenceSchema();
            return RelationalModelExtensions.FindSequence(model, sequenceName, sequenceSchema);
        }

        public static string? GetSequenceName(this IReadOnlyProperty property)
        {
            return (string)((IReadOnlyAnnotatable)property)["Dm:SequenceName"];
        }

        public static string? GetSequenceName(this IReadOnlyProperty property, in StoreObjectIdentifier storeObject)
        {
            IAnnotation annotation = property.FindAnnotation("Dm:SequenceName");
            if (annotation != null)
            {
                return (string)annotation.Value;
            }
            return RelationalPropertyExtensions.FindSharedStoreObjectRootProperty(property, in storeObject)?.GetSequenceName(in storeObject);
        }

        public static string? GetSequenceSchema(this IReadOnlyProperty property)
        {
            return (string)((IReadOnlyAnnotatable)property)["Dm:SequenceSchema"];
        }

        public static string? GetSequenceSchema(this IReadOnlyProperty property, in StoreObjectIdentifier storeObject)
        {
            IAnnotation annotation = property.FindAnnotation("Dm:SequenceSchema");
            if (annotation != null)
            {
                return (string)annotation.Value;
            }
            return RelationalPropertyExtensions.FindSharedStoreObjectRootProperty(property, in storeObject)?.GetSequenceSchema(in storeObject);
        }

        public static int? GetIdentitySeed([NotNull] this IReadOnlyProperty property)
        {
            return (int?)((IReadOnlyAnnotatable)property)["Dm:IdentitySeed"];
        }

        public static void SetIdentitySeed([NotNull] this IMutableProperty property, int? seed)
        {
            property.SetOrRemoveAnnotation("Dm:IdentitySeed", seed);
        }

        public static void SetIdentitySeed([NotNull] this IConventionProperty property, int? seed, bool fromDataAnnotation = false)
        {
            property.SetOrRemoveAnnotation("Dm:IdentitySeed", seed, fromDataAnnotation);
        }

        public static ConfigurationSource? GetIdentitySeedConfigurationSource([NotNull] this IConventionProperty property)
        {
            IConventionAnnotation annotation = property.FindAnnotation("Dm:IdentitySeed");
            if (annotation == null)
            {
                return null;
            }
            return annotation.GetConfigurationSource();
        }

        public static int? GetIdentityIncrement([NotNull] this IReadOnlyProperty property)
        {
            return (int?)((IReadOnlyAnnotatable)property)["Dm:IdentityIncrement"];
        }

        public static void SetIdentityIncrement([NotNull] this IMutableProperty property, int? increment)
        {
            property.SetOrRemoveAnnotation("Dm:IdentityIncrement", increment);
        }

        public static void SetIdentityIncrement([NotNull] this IConventionProperty property, int? increment, bool fromDataAnnotation = false)
        {
            property.SetOrRemoveAnnotation("Dm:IdentityIncrement", increment, fromDataAnnotation);
        }

        public static ConfigurationSource? GetIdentityIncrementConfigurationSource([NotNull] this IConventionProperty property)
        {
            IConventionAnnotation annotation = property.FindAnnotation("Dm:IdentityIncrement");
            if (annotation == null)
            {
                return null;
            }
            return annotation.GetConfigurationSource();
        }

        public static DmValueGenerationStrategy GetValueGenerationStrategy([NotNull] this IReadOnlyProperty property)
        {
            IAnnotation annotation = property.FindAnnotation("Dm:ValueGenerationStrategy");
            if (annotation != null)
            {
                return (DmValueGenerationStrategy)(annotation.Value ?? (DmValueGenerationStrategy.None));
            }
            DmValueGenerationStrategy defaultValueGenerationStrategy = GetDefaultValueGenerationStrategy(property);
            if ((int)property.ValueGenerated != 1 || property.IsForeignKey() || RelationalPropertyExtensions.TryGetDefaultValue(property, out object defaultValue) || (defaultValueGenerationStrategy != DmValueGenerationStrategy.Sequence && RelationalPropertyExtensions.GetDefaultValueSql(property) != null) || RelationalPropertyExtensions.GetComputedColumnSql(property) != null)
            {
                return DmValueGenerationStrategy.None;
            }
            return defaultValueGenerationStrategy;
        }

        public static DmValueGenerationStrategy GetValueGenerationStrategy(this IReadOnlyProperty property, in StoreObjectIdentifier storeObject)
        {
            return property.GetValueGenerationStrategy(in storeObject, null);
        }

        internal static DmValueGenerationStrategy GetValueGenerationStrategy(this IReadOnlyProperty property, in StoreObjectIdentifier storeObject, ITypeMappingSource? typeMappingSource)
        {
            IReadOnlyRelationalPropertyOverrides overrides = RelationalPropertyExtensions.FindOverrides(property, in storeObject);
            IAnnotation overrideAnnotation = overrides?.FindAnnotation("Dm:ValueGenerationStrategy");
            if (overrideAnnotation != null)
            {
                return ((DmValueGenerationStrategy?)overrideAnnotation.Value).GetValueOrDefault();
            }
            IAnnotation propertyAnnotation = property.FindAnnotation("Dm:ValueGenerationStrategy");
            if (propertyAnnotation != null && propertyAnnotation.Value != null)
            {
                StoreObjectIdentifier? declaringStoreObject = StoreObjectIdentifier.Create(property.DeclaringType, storeObject.StoreObjectType);
                StoreObjectIdentifier currentStoreObject = storeObject;
                if (declaringStoreObject.HasValue && declaringStoreObject.GetValueOrDefault() == currentStoreObject)
                {
                    return (DmValueGenerationStrategy)propertyAnnotation.Value;
                }
            }
            StoreObjectIdentifier table = storeObject;
            IReadOnlyProperty rootProperty = RelationalPropertyExtensions.FindSharedStoreObjectRootProperty(property, in storeObject);
            if (rootProperty != null)
            {
                if (rootProperty.GetValueGenerationStrategy(in storeObject, typeMappingSource) != DmValueGenerationStrategy.IdentityColumn || (int)((StoreObjectIdentifier)table).StoreObjectType != 0 || property.GetContainingForeignKeys().Any(fk =>
                {
                    if (fk.IsBaseLinking())
                    {
                        StoreObjectIdentifier? principalStoreObject = StoreObjectIdentifier.Create((IReadOnlyTypeBase)(object)fk.PrincipalEntityType, 0);
                        if (principalStoreObject.HasValue)
                        {
                            StoreObjectIdentifier valueOrDefault2 = principalStoreObject.GetValueOrDefault();
                            return RelationalForeignKeyExtensions.GetConstraintName(fk, in table, in valueOrDefault2) != null;
                        }
                        return false;
                    }
                    return true;
                }))
                {
                    return DmValueGenerationStrategy.None;
                }
                return DmValueGenerationStrategy.IdentityColumn;
            }
            if ((int)property.ValueGenerated != 1 || table.StoreObjectType != 0 || RelationalPropertyExtensions.TryGetDefaultValue(property, in storeObject, out object defaultValue2) || RelationalPropertyExtensions.GetDefaultValueSql(property, in storeObject) != null || RelationalPropertyExtensions.GetComputedColumnSql(property, in storeObject) != null || property.GetContainingForeignKeys().Any(fk =>
            {
                if (fk.IsBaseLinking())
                {
                    StoreObjectIdentifier? principalStoreObject = StoreObjectIdentifier.Create((IReadOnlyTypeBase)(object)fk.PrincipalEntityType, (StoreObjectType)0);
                    if (principalStoreObject.HasValue)
                    {
                        StoreObjectIdentifier valueOrDefault = principalStoreObject.GetValueOrDefault();
                        return RelationalForeignKeyExtensions.GetConstraintName(fk, in table, in valueOrDefault) != null;
                    }
                    return false;
                }
                return true;
            }))
            {
                return DmValueGenerationStrategy.None;
            }
            DmValueGenerationStrategy defaultValueGenerationStrategy = GetDefaultValueGenerationStrategy(property, in storeObject, typeMappingSource);
            if (defaultValueGenerationStrategy != 0 && propertyAnnotation != null)
            {
                return ((DmValueGenerationStrategy?)propertyAnnotation.Value).GetValueOrDefault();
            }
            return defaultValueGenerationStrategy;
        }

        public static DmValueGenerationStrategy? GetValueGenerationStrategy(this IReadOnlyRelationalPropertyOverrides overrides)
        {
            IAnnotation annotation = overrides.FindAnnotation("Dm:ValueGenerationStrategy");
            return (DmValueGenerationStrategy?)(annotation?.Value);
        }

        private static DmValueGenerationStrategy GetDefaultValueGenerationStrategy(IReadOnlyProperty property)
        {
            DmValueGenerationStrategy? valueGenerationStrategy = property.DeclaringType.Model.GetValueGenerationStrategy();
            bool flag;
            switch (valueGenerationStrategy)
            {
                case DmValueGenerationStrategy.SequenceHiLo:
                case DmValueGenerationStrategy.Sequence:
                    flag = true;
                    break;
                default:
                    flag = false;
                    break;
            }
            if (flag && IsCompatibleWithValueGeneration(property))
            {
                return valueGenerationStrategy.Value;
            }
            if (valueGenerationStrategy.GetValueOrDefault() != DmValueGenerationStrategy.IdentityColumn || !IsCompatibleWithValueGeneration(property))
            {
                return DmValueGenerationStrategy.None;
            }
            return DmValueGenerationStrategy.IdentityColumn;
        }

        private static DmValueGenerationStrategy GetDefaultValueGenerationStrategy(IReadOnlyProperty property, in StoreObjectIdentifier storeObject, ITypeMappingSource? typeMappingSource)
        {
            DmValueGenerationStrategy? valueGenerationStrategy = property.DeclaringType.Model.GetValueGenerationStrategy();
            bool flag;
            switch (valueGenerationStrategy)
            {
                case DmValueGenerationStrategy.SequenceHiLo:
                case DmValueGenerationStrategy.Sequence:
                    flag = true;
                    break;
                default:
                    flag = false;
                    break;
            }
            if (flag && IsCompatibleWithValueGeneration(property, in storeObject, typeMappingSource))
            {
                return valueGenerationStrategy.Value;
            }
            if (valueGenerationStrategy.GetValueOrDefault() != DmValueGenerationStrategy.IdentityColumn || !IsCompatibleWithValueGeneration(property, in storeObject, typeMappingSource))
            {
                return DmValueGenerationStrategy.None;
            }
            if (!(RelationalTypeBaseExtensions.GetMappingStrategy(property.DeclaringType) == "TPC"))
            {
                return DmValueGenerationStrategy.IdentityColumn;
            }
            return DmValueGenerationStrategy.Sequence;
        }

        public static void SetValueGenerationStrategy([NotNull] this IMutableProperty property, DmValueGenerationStrategy? value)
        {
            CheckValueGenerationStrategy((IReadOnlyProperty)(object)property, value);
            property.SetOrRemoveAnnotation("Dm:ValueGenerationStrategy", value);
        }

        public static void SetValueGenerationStrategy([NotNull] this IConventionProperty property, DmValueGenerationStrategy? value, bool fromDataAnnotation = false)
        {
            CheckValueGenerationStrategy((IReadOnlyProperty)(object)property, value);
            property.SetOrRemoveAnnotation("Dm:ValueGenerationStrategy", value, fromDataAnnotation);
        }

        private static void CheckValueGenerationStrategy(IReadOnlyProperty property, DmValueGenerationStrategy? value)
        {
            if (value.HasValue)
            {
                Type clrType = property.ClrType;
                if (value.GetValueOrDefault() == DmValueGenerationStrategy.IdentityColumn && !IsCompatibleWithValueGeneration(property))
                {
                    throw new ArgumentException(DmStrings.IdentityBadType(property.Name, property.DeclaringType.DisplayName(), TypeExtensions.ShortDisplayName(clrType)));
                }
                if (value.GetValueOrDefault() == DmValueGenerationStrategy.SequenceHiLo && !IsCompatibleWithValueGeneration(property))
                {
                    throw new ArgumentException(DmStrings.SequenceBadType(property.Name, property.DeclaringType.DisplayName(), TypeExtensions.ShortDisplayName(clrType)));
                }
            }
        }

        public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource([NotNull] this IConventionProperty property)
        {
            IConventionAnnotation annotation = property.FindAnnotation("Dm:ValueGenerationStrategy");
            if (annotation == null)
            {
                return null;
            }
            return annotation.GetConfigurationSource();
        }

        public static bool IsCompatibleWithValueGeneration(IReadOnlyProperty property)
        {
            object converter = property.GetValueConverter();
            if (converter == null)
            {
                CoreTypeMapping typeMapping = property.FindTypeMapping();
                converter = (typeMapping?.Converter);
            }
            Type type = (((converter != null) ? ((ValueConverter)converter).ProviderClrType : null) ?? property.ClrType).UnwrapNullableType();
            if (!type.IsInteger() && !type.IsEnum)
            {
                return type == typeof(decimal);
            }
            return true;
        }

        private static bool IsCompatibleWithValueGeneration(IReadOnlyProperty property, in StoreObjectIdentifier storeObject, ITypeMappingSource? typeMappingSource)
        {
            if (storeObject.StoreObjectType != 0)
            {
                return false;
            }
            object converter = property.GetValueConverter();
            if (converter == null)
            {
                object typeMapping = (RelationalPropertyExtensions.FindRelationalTypeMapping(property, in storeObject)) ?? (((typeMappingSource != null) ? typeMappingSource!.FindMapping((IProperty)property) : null));
                converter = ((typeMapping != null) ? ((CoreTypeMapping)typeMapping).Converter : null);
            }
            Type type = (((converter != null) ? ((ValueConverter)converter).ProviderClrType : null) ?? property.ClrType).UnwrapNullableType();
            if (!type.IsInteger() && !type.IsEnum)
            {
                return type == typeof(decimal);
            }
            return true;
        }
    }
}
