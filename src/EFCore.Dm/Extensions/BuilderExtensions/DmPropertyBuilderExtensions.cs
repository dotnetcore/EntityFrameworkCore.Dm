using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Utilities;
using System;

namespace Microsoft.EntityFrameworkCore
{
    public static class DmPropertyBuilderExtensions
    {
        public static PropertyBuilder UseHiLo([NotNull] this PropertyBuilder propertyBuilder, [CanBeNull] string name = null, [CanBeNull] string schema = null)
        {
            Check.NotNull(propertyBuilder, "propertyBuilder");
            Check.NullButNotEmpty(name, "name");
            Check.NullButNotEmpty(schema, "schema");
            IMutableProperty metadata = propertyBuilder.Metadata;
            name ??= "EntityFrameworkHiLoSequence";
            IMutableModel model = metadata.DeclaringType.Model;
            if (RelationalModelExtensions.FindSequence(model, name, schema) == null)
            {
                RelationalModelExtensions.AddSequence(model, name, schema).IncrementBy = 10;
            }
            metadata.SetValueGenerationStrategy(DmValueGenerationStrategy.SequenceHiLo);
            metadata.SetHiLoSequenceName(name);
            metadata.SetHiLoSequenceSchema(schema);
            metadata.SetIdentitySeed(null);
            metadata.SetIdentityIncrement(null);
            return propertyBuilder;
        }

        public static PropertyBuilder<TProperty> UseHiLo<TProperty>([NotNull] this PropertyBuilder<TProperty> propertyBuilder, [CanBeNull] string name = null, [CanBeNull] string schema = null)
        {
            return (PropertyBuilder<TProperty>)(object)((PropertyBuilder)(object)propertyBuilder).UseHiLo(name, schema);
        }

        public static IConventionSequenceBuilder HasHiLoSequence([NotNull] this IConventionPropertyBuilder propertyBuilder, [CanBeNull] string name, [CanBeNull] string schema, bool fromDataAnnotation = false)
        {
            if (!propertyBuilder.CanSetHiLoSequence(name, schema, fromDataAnnotation))
            {
                return null;
            }
            propertyBuilder.Metadata.SetHiLoSequenceName(name, fromDataAnnotation);
            propertyBuilder.Metadata.SetHiLoSequenceSchema(schema, fromDataAnnotation);
            if (name != null)
            {
                return RelationalModelBuilderExtensions.HasSequence((propertyBuilder.Metadata.DeclaringType).Model.Builder, name, schema, fromDataAnnotation);
            }
            return null;
        }

        public static bool CanSetHiLoSequence([NotNull] this IConventionPropertyBuilder propertyBuilder, [CanBeNull] string name, [CanBeNull] string schema, bool fromDataAnnotation = false)
        {
            Check.NotNull(propertyBuilder, "propertyBuilder");
            Check.NullButNotEmpty(name, "name");
            Check.NullButNotEmpty(schema, "schema");
            if ((propertyBuilder).CanSetAnnotation("Dm:HiLoSequenceName", name, fromDataAnnotation))
            {
                return (propertyBuilder).CanSetAnnotation("Dm:HiLoSequenceSchema", schema, fromDataAnnotation);
            }
            return false;
        }

        public static PropertyBuilder UseIdentityColumn([NotNull] this PropertyBuilder propertyBuilder, int seed = 1, int increment = 1)
        {
            Check.NotNull(propertyBuilder, "propertyBuilder");
            IMutableProperty metadata = propertyBuilder.Metadata;
            metadata.SetValueGenerationStrategy(DmValueGenerationStrategy.IdentityColumn);
            metadata.SetIdentitySeed(seed);
            metadata.SetIdentityIncrement(increment);
            metadata.SetHiLoSequenceName(null);
            metadata.SetHiLoSequenceSchema(null);
            return propertyBuilder;
        }

        public static PropertyBuilder<TProperty> UseIdentityColumn<TProperty>([NotNull] this PropertyBuilder<TProperty> propertyBuilder, int seed = 1, int increment = 1)
        {
            return propertyBuilder.UseIdentityColumn(seed, increment);
        }

        public static IConventionPropertyBuilder HasIdentityColumnSeed([NotNull] this IConventionPropertyBuilder propertyBuilder, int? seed, bool fromDataAnnotation = false)
        {
            if (propertyBuilder.CanSetIdentityColumnSeed(seed, fromDataAnnotation))
            {
                propertyBuilder.Metadata.SetIdentitySeed(seed, fromDataAnnotation);
                return propertyBuilder;
            }
            return null;
        }

        public static bool CanSetIdentityColumnSeed([NotNull] this IConventionPropertyBuilder propertyBuilder, int? seed, bool fromDataAnnotation = false)
        {
            Check.NotNull(propertyBuilder, "propertyBuilder");
            return propertyBuilder.CanSetAnnotation("Dm:IdentitySeed", seed, fromDataAnnotation);
        }

        public static IConventionPropertyBuilder HasIdentityColumnIncrement([NotNull] this IConventionPropertyBuilder propertyBuilder, int? increment, bool fromDataAnnotation = false)
        {
            if (propertyBuilder.CanSetIdentityColumnIncrement(increment, fromDataAnnotation))
            {
                propertyBuilder.Metadata.SetIdentityIncrement(increment, fromDataAnnotation);
                return propertyBuilder;
            }
            return null;
        }

        public static bool CanSetIdentityColumnIncrement([NotNull] this IConventionPropertyBuilder propertyBuilder, int? increment, bool fromDataAnnotation = false)
        {
            Check.NotNull(propertyBuilder, "propertyBuilder");
            return propertyBuilder.CanSetAnnotation("Dm:IdentityIncrement", increment, fromDataAnnotation);
        }

        public static IConventionPropertyBuilder HasValueGenerationStrategy([NotNull] this IConventionPropertyBuilder propertyBuilder, DmValueGenerationStrategy? valueGenerationStrategy, bool fromDataAnnotation = false)
        {
            if ((propertyBuilder).CanSetAnnotation("Dm:ValueGenerationStrategy", valueGenerationStrategy, fromDataAnnotation))
            {
                propertyBuilder.Metadata.SetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);
                if (valueGenerationStrategy.GetValueOrDefault() != DmValueGenerationStrategy.IdentityColumn)
                {
                    propertyBuilder.HasIdentityColumnSeed(null, fromDataAnnotation);
                    propertyBuilder.HasIdentityColumnIncrement(null, fromDataAnnotation);
                }
                if (valueGenerationStrategy.GetValueOrDefault() != DmValueGenerationStrategy.SequenceHiLo)
                {
                    propertyBuilder.HasHiLoSequence(null, null, fromDataAnnotation);
                }
                return propertyBuilder;
            }
            return null;
        }

        public static bool CanSetValueGenerationStrategy([NotNull] this IConventionPropertyBuilder propertyBuilder, DmValueGenerationStrategy? valueGenerationStrategy, bool fromDataAnnotation = false)
        {
            Check.NotNull(propertyBuilder, "propertyBuilder");
            if (!valueGenerationStrategy.HasValue || DmPropertyExtensions.IsCompatibleWithValueGeneration((IReadOnlyProperty)(object)propertyBuilder.Metadata))
            {
                return (propertyBuilder).CanSetAnnotation("Dm:ValueGenerationStrategy", valueGenerationStrategy, fromDataAnnotation);
            }
            return false;
        }

        [Obsolete("Use UseHiLo")]
        public static PropertyBuilder ForSqlServerUseSequenceHiLo([NotNull] this PropertyBuilder propertyBuilder, [CanBeNull] string name = null, [CanBeNull] string schema = null)
        {
            return propertyBuilder.UseHiLo(name, schema);
        }

        [Obsolete("Use UseHiLo")]
        public static PropertyBuilder<TProperty> ForSqlServerUseSequenceHiLo<TProperty>([NotNull] this PropertyBuilder<TProperty> propertyBuilder, [CanBeNull] string name = null, [CanBeNull] string schema = null)
        {
            return propertyBuilder.UseHiLo<TProperty>(name, schema);
        }

        [Obsolete("Use HasHiLoSequence")]
        public static IConventionSequenceBuilder ForSqlServerHasHiLoSequence([NotNull] this IConventionPropertyBuilder propertyBuilder, [CanBeNull] string name, [CanBeNull] string schema, bool fromDataAnnotation = false)
        {
            return propertyBuilder.HasHiLoSequence(name, schema);
        }

        [Obsolete("Use UseIdentityColumn")]
        public static PropertyBuilder UseSqlServerIdentityColumn([NotNull] this PropertyBuilder propertyBuilder, int seed = 1, int increment = 1)
        {
            return propertyBuilder.UseIdentityColumn(seed, increment);
        }

        [Obsolete("Use UseIdentityColumn")]
        public static PropertyBuilder<TProperty> UseSqlServerIdentityColumn<TProperty>([NotNull] this PropertyBuilder<TProperty> propertyBuilder, int seed = 1, int increment = 1)
        {
            return propertyBuilder.UseIdentityColumn<TProperty>(seed, increment);
        }

        [Obsolete("Use HasIdentityColumnSeed")]
        public static IConventionPropertyBuilder ForSqlServerHasIdentitySeed([NotNull] this IConventionPropertyBuilder propertyBuilder, int? seed, bool fromDataAnnotation = false)
        {
            return propertyBuilder.HasIdentityColumnSeed(seed, fromDataAnnotation);
        }

        [Obsolete("Use HasIdentityColumnIncrement")]
        public static IConventionPropertyBuilder ForSqlServerHasIdentityIncrement([NotNull] this IConventionPropertyBuilder propertyBuilder, int? increment, bool fromDataAnnotation = false)
        {
            return propertyBuilder.HasIdentityColumnIncrement(increment, fromDataAnnotation);
        }

        [Obsolete("Use HasValueGenerationStrategy")]
        public static IConventionPropertyBuilder ForSqlServerHasValueGenerationStrategy([NotNull] this IConventionPropertyBuilder propertyBuilder, DmValueGenerationStrategy? valueGenerationStrategy, bool fromDataAnnotation = false)
        {
            return propertyBuilder.HasValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);
        }
    }
}
