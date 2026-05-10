using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{
	public static class DmModelBuilderExtensions
	{
		public static ModelBuilder UseHiLo([NotNull] this ModelBuilder modelBuilder, [CanBeNull] string name = null, [CanBeNull] string schema = null)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			Check.NullButNotEmpty(name, "name");
			Check.NullButNotEmpty(schema, "schema");
			IMutableModel model = modelBuilder.Model;
			name ??= "EntityFrameworkHiLoSequence";
			if (RelationalModelExtensions.FindSequence(model, name, schema) == null)
			{
				RelationalModelBuilderExtensions.HasSequence(modelBuilder, name, schema).IncrementsBy(10);
			}
			model.SetValueGenerationStrategy(DmValueGenerationStrategy.SequenceHiLo);
			model.SetHiLoSequenceName(name);
			model.SetHiLoSequenceSchema(schema);
			model.SetIdentitySeed(null);
			model.SetIdentityIncrement(null);
			return modelBuilder;
		}

		public static IConventionSequenceBuilder HasHiLoSequence([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string name, [CanBeNull] string schema, bool fromDataAnnotation = false)
		{
			if (!modelBuilder.CanSetHiLoSequence(name, schema))
			{
				return null;
			}
			modelBuilder.Metadata.SetHiLoSequenceName(name, fromDataAnnotation);
			modelBuilder.Metadata.SetHiLoSequenceSchema(schema, fromDataAnnotation);
			if (name != null)
			{
				return RelationalModelBuilderExtensions.HasSequence(modelBuilder, name, schema, fromDataAnnotation);
			}
			return null;
		}

		public static bool CanSetHiLoSequence([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string name, [CanBeNull] string schema, bool fromDataAnnotation = false)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			Check.NullButNotEmpty(name, "name");
			Check.NullButNotEmpty(schema, "schema");
			if ((modelBuilder).CanSetAnnotation("Dm:HiLoSequenceName", name, fromDataAnnotation))
			{
				return (modelBuilder).CanSetAnnotation("Dm:HiLoSequenceSchema", schema, fromDataAnnotation);
			}
			return false;
		}

		public static ModelBuilder UseIdentityColumns([NotNull] this ModelBuilder modelBuilder, int seed = 1, int increment = 1)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			IMutableModel model = modelBuilder.Model;
			model.SetValueGenerationStrategy(DmValueGenerationStrategy.IdentityColumn);
			model.SetIdentitySeed(seed);
			model.SetIdentityIncrement(increment);
			model.SetHiLoSequenceName(null);
			model.SetHiLoSequenceSchema(null);
			return modelBuilder;
		}

		public static IConventionModelBuilder HasIdentityColumnSeed([NotNull] this IConventionModelBuilder modelBuilder, int? seed, bool fromDataAnnotation = false)
		{
			if (modelBuilder.CanSetIdentityColumnSeed(seed, fromDataAnnotation))
			{
				modelBuilder.Metadata.SetIdentitySeed(seed, fromDataAnnotation);
				return modelBuilder;
			}
			return null;
		}

		public static bool CanSetIdentityColumnSeed([NotNull] this IConventionModelBuilder modelBuilder, int? seed, bool fromDataAnnotation = false)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			return (modelBuilder).CanSetAnnotation("Dm:IdentitySeed", seed, fromDataAnnotation);
		}

		public static IConventionModelBuilder HasIdentityColumnIncrement([NotNull] this IConventionModelBuilder modelBuilder, int? increment, bool fromDataAnnotation = false)
		{
			if (modelBuilder.CanSetIdentityColumnIncrement(increment, fromDataAnnotation))
			{
				modelBuilder.Metadata.SetIdentityIncrement(increment, fromDataAnnotation);
				return modelBuilder;
			}
			return null;
		}

		public static bool CanSetIdentityColumnIncrement([NotNull] this IConventionModelBuilder modelBuilder, int? increment, bool fromDataAnnotation = false)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			return modelBuilder.CanSetAnnotation("Dm:IdentityIncrement", (object)increment, fromDataAnnotation);
		}

		public static IConventionModelBuilder HasValueGenerationStrategy([NotNull] this IConventionModelBuilder modelBuilder, DmValueGenerationStrategy? valueGenerationStrategy, bool fromDataAnnotation = false)
		{
			if (modelBuilder.CanSetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation))
			{
				modelBuilder.Metadata.SetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);
				if (valueGenerationStrategy.GetValueOrDefault() != DmValueGenerationStrategy.IdentityColumn)
				{
					modelBuilder.HasIdentityColumnSeed(null, fromDataAnnotation);
					modelBuilder.HasIdentityColumnIncrement(null, fromDataAnnotation);
				}
				if (valueGenerationStrategy.GetValueOrDefault() != DmValueGenerationStrategy.SequenceHiLo)
				{
					modelBuilder.HasHiLoSequence(null, null, fromDataAnnotation);
				}
				return modelBuilder;
			}
			return null;
		}

		public static bool CanSetValueGenerationStrategy([NotNull] this IConventionModelBuilder modelBuilder, DmValueGenerationStrategy? valueGenerationStrategy, bool fromDataAnnotation = false)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			return modelBuilder.CanSetAnnotation("Dm:ValueGenerationStrategy", valueGenerationStrategy, fromDataAnnotation);
		}

		public static ModelBuilder HasDatabaseMaxSize([NotNull] this ModelBuilder modelBuilder, [NotNull] string maxSize)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			Check.NotNull(maxSize, "maxSize");
			modelBuilder.Model.SetDatabaseMaxSize(maxSize);
			return modelBuilder;
		}

		public static IConventionModelBuilder HasDatabaseMaxSize([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string maxSize, bool fromDataAnnotation = false)
		{
			if (modelBuilder.CanSetDatabaseMaxSize(maxSize, fromDataAnnotation))
			{
				modelBuilder.Metadata.SetDatabaseMaxSize(maxSize, fromDataAnnotation);
				return modelBuilder;
			}
			return null;
		}

		public static bool CanSetDatabaseMaxSize([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string maxSize, bool fromDataAnnotation = false)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			return modelBuilder.CanSetAnnotation("Dm:DatabaseMaxSize", maxSize, fromDataAnnotation);
		}

		public static ModelBuilder HasServiceTier([NotNull] this ModelBuilder modelBuilder, [NotNull] string serviceTier)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			Check.NotNull(serviceTier, "serviceTier");
			modelBuilder.Model.SetServiceTierSql("'" + serviceTier.Replace("'", "''") + "'");
			return modelBuilder;
		}

		public static ModelBuilder HasServiceTierSql([NotNull] this ModelBuilder modelBuilder, [NotNull] string serviceTier)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			Check.NotNull(serviceTier, "serviceTier");
			modelBuilder.Model.SetServiceTierSql(serviceTier);
			return modelBuilder;
		}

		public static IConventionModelBuilder HasServiceTierSql([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string serviceTier, bool fromDataAnnotation = false)
		{
			if (modelBuilder.CanSetServiceTierSql(serviceTier, fromDataAnnotation))
			{
				modelBuilder.Metadata.SetServiceTierSql(serviceTier, fromDataAnnotation);
				return modelBuilder;
			}
			return null;
		}

		public static bool CanSetServiceTierSql([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string serviceTier, bool fromDataAnnotation = false)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			return modelBuilder.CanSetAnnotation("Dm:ServiceTierSql", serviceTier, fromDataAnnotation);
		}

		public static ModelBuilder HasPerformanceLevel([NotNull] this ModelBuilder modelBuilder, [NotNull] string performanceLevel)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			Check.NotNull(performanceLevel, "performanceLevel");
			modelBuilder.Model.SetPerformanceLevelSql("'" + performanceLevel.Replace("'", "''") + "'");
			return modelBuilder;
		}

		public static ModelBuilder HasPerformanceLevelSql([NotNull] this ModelBuilder modelBuilder, [NotNull] string performanceLevel)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			Check.NotNull(performanceLevel, "performanceLevel");
			modelBuilder.Model.SetPerformanceLevelSql(performanceLevel);
			return modelBuilder;
		}

		public static IConventionModelBuilder HasPerformanceLevelSql([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string performanceLevel, bool fromDataAnnotation = false)
		{
			if (modelBuilder.CanSetPerformanceLevelSql(performanceLevel, fromDataAnnotation))
			{
				modelBuilder.Metadata.SetPerformanceLevelSql(performanceLevel, fromDataAnnotation);
				return modelBuilder;
			}
			return null;
		}

		public static bool CanSetPerformanceLevelSql([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string performanceLevel, bool fromDataAnnotation = false)
		{
			Check.NotNull(modelBuilder, "modelBuilder");
			return modelBuilder.CanSetAnnotation("Dm:PerformanceLevelSql", performanceLevel, fromDataAnnotation);
		}

		[Obsolete("Use UseHiLo")]
		public static ModelBuilder ForSqlServerUseSequenceHiLo([NotNull] this ModelBuilder modelBuilder, [CanBeNull] string name = null, [CanBeNull] string schema = null)
		{
			return modelBuilder.UseHiLo(name, schema);
		}

		[Obsolete("Use HasHiLoSequence")]
		public static IConventionSequenceBuilder ForSqlServerHasHiLoSequence([NotNull] this IConventionModelBuilder modelBuilder, [CanBeNull] string name, [CanBeNull] string schema, bool fromDataAnnotation = false)
		{
			return modelBuilder.HasHiLoSequence(name, schema, fromDataAnnotation);
		}

		[Obsolete("Use UseIdentityColumns")]
		public static ModelBuilder ForSqlServerUseIdentityColumns([NotNull] this ModelBuilder modelBuilder, int seed = 1, int increment = 1)
		{
			return modelBuilder.UseIdentityColumns(seed, increment);
		}

		[Obsolete("Use HasIdentityColumnSeed")]
		public static IConventionModelBuilder ForSqlServerHasIdentitySeed([NotNull] this IConventionModelBuilder modelBuilder, int? seed, bool fromDataAnnotation = false)
		{
			return modelBuilder.HasIdentityColumnSeed(seed, fromDataAnnotation);
		}

		[Obsolete("Use HasIdentityColumnIncrement")]
		public static IConventionModelBuilder ForSqlServerHasIdentityIncrement([NotNull] this IConventionModelBuilder modelBuilder, int? increment, bool fromDataAnnotation = false)
		{
			return modelBuilder.HasIdentityColumnIncrement(increment, fromDataAnnotation);
		}

		[Obsolete("Use HasValueGenerationStrategy")]
		public static IConventionModelBuilder ForSqlServerHasValueGenerationStrategy([NotNull] this IConventionModelBuilder modelBuilder, DmValueGenerationStrategy? valueGenerationStrategy, bool fromDataAnnotation = false)
		{
			return modelBuilder.HasValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);
		}
	}
}
