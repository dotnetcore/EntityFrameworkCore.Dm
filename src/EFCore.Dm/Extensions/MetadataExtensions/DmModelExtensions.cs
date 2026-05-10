using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{
    public static class DmModelExtensions
    {
        public const string DefaultHiLoSequenceName = "EntityFrameworkHiLoSequence";

        public const string DefaultSequenceNameSuffix = "Sequence";

        public static string GetHiLoSequenceName([NotNull] this IReadOnlyModel model)
        {
            return ((string)((IReadOnlyAnnotatable)model)["Dm:HiLoSequenceName"]) ?? "EntityFrameworkHiLoSequence";
        }

        public static void SetHiLoSequenceName([NotNull] this IMutableModel model, [CanBeNull] string name)
        {
            Check.NullButNotEmpty(name, "name");
            model.SetOrRemoveAnnotation("Dm:HiLoSequenceName", name);
        }

        public static void SetHiLoSequenceName([NotNull] this IConventionModel model, [CanBeNull] string name, bool fromDataAnnotation = false)
        {
            Check.NullButNotEmpty(name, "name");
            model.SetOrRemoveAnnotation("Dm:HiLoSequenceName", name, fromDataAnnotation);
        }

        public static ConfigurationSource? GetHiLoSequenceNameConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:HiLoSequenceName");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }

        public static string GetHiLoSequenceSchema([NotNull] this IReadOnlyModel model)
        {
            return (string)((IReadOnlyAnnotatable)model)["Dm:HiLoSequenceSchema"];
        }

        public static void SetHiLoSequenceSchema([NotNull] this IMutableModel model, [CanBeNull] string value)
        {
            Check.NullButNotEmpty(value, "value");
            model.SetOrRemoveAnnotation("Dm:HiLoSequenceSchema", value);
        }

        public static void SetHiLoSequenceSchema([NotNull] this IConventionModel model, [CanBeNull] string value, bool fromDataAnnotation = false)
        {
            Check.NullButNotEmpty(value, "value");
            model.SetOrRemoveAnnotation("Dm:HiLoSequenceSchema", value, fromDataAnnotation);
        }

        public static ConfigurationSource? GetHiLoSequenceSchemaConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:HiLoSequenceSchema");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }

        public static string GetSequenceNameSuffix(this IReadOnlyModel model)
        {
            return ((string)((IReadOnlyAnnotatable)model)["Dm:SequenceNameSuffix"]) ?? "Sequence";
        }

        public static string? GetSequenceSchema(this IReadOnlyModel model)
        {
            return (string)((IReadOnlyAnnotatable)model)["Dm:SequenceSchema"];
        }

        public static void SetSequenceSchema(this IMutableModel model, string? value)
        {
            Check.NullButNotEmpty(value, "value");
            model.SetOrRemoveAnnotation("Dm:SequenceSchema", value);
        }

        public static int GetIdentitySeed([NotNull] this IReadOnlyModel model)
        {
            return ((int?)((IReadOnlyAnnotatable)model)["Dm:IdentitySeed"]).GetValueOrDefault(1);
        }

        public static void SetIdentitySeed([NotNull] this IMutableModel model, int? seed)
        {
            model.SetOrRemoveAnnotation("Dm:IdentitySeed", seed);
        }

        public static void SetIdentitySeed([NotNull] this IConventionModel model, int? seed, bool fromDataAnnotation = false)
        {
            model.SetOrRemoveAnnotation("Dm:IdentitySeed", seed, fromDataAnnotation);
        }

        public static ConfigurationSource? GetIdentitySeedConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:IdentitySeed");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }

        public static int GetIdentityIncrement([NotNull] this IReadOnlyModel model)
        {
            return ((int?)((IReadOnlyAnnotatable)model)["Dm:IdentityIncrement"]).GetValueOrDefault(1);
        }

        public static void SetIdentityIncrement([NotNull] this IMutableModel model, int? increment)
        {
            model.SetOrRemoveAnnotation("Dm:IdentityIncrement", increment);
        }

        public static void SetIdentityIncrement([NotNull] this IConventionModel model, int? increment, bool fromDataAnnotation = false)
        {
            model.SetOrRemoveAnnotation("Dm:IdentityIncrement", increment, fromDataAnnotation);
        }

        public static ConfigurationSource? GetIdentityIncrementConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:IdentityIncrement");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }

        public static DmValueGenerationStrategy? GetValueGenerationStrategy([NotNull] this IReadOnlyModel model)
        {
            return (DmValueGenerationStrategy?)((IReadOnlyAnnotatable)model)["Dm:ValueGenerationStrategy"];
        }

        public static void SetValueGenerationStrategy([NotNull] this IMutableModel model, DmValueGenerationStrategy? value)
        {
            model.SetOrRemoveAnnotation("Dm:ValueGenerationStrategy", value);
        }

        public static void SetValueGenerationStrategy([NotNull] this IConventionModel model, DmValueGenerationStrategy? value, bool fromDataAnnotation = false)
        {
            model.SetOrRemoveAnnotation("Dm:ValueGenerationStrategy", value, fromDataAnnotation);
        }

        public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:ValueGenerationStrategy");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }

        public static string GetDatabaseMaxSize([NotNull] this IReadOnlyModel model)
        {
            return (string)((IReadOnlyAnnotatable)model)["Dm:DatabaseMaxSize"];
        }

        public static void SetDatabaseMaxSize([NotNull] this IMutableModel model, [CanBeNull] string value)
        {
            model.SetOrRemoveAnnotation("Dm:DatabaseMaxSize", value);
        }

        public static void SetDatabaseMaxSize([NotNull] this IConventionModel model, [CanBeNull] string value, bool fromDataAnnotation = false)
        {
            model.SetOrRemoveAnnotation("Dm:DatabaseMaxSize", value, fromDataAnnotation);
        }

        public static ConfigurationSource? GetDatabaseMaxSizeConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:DatabaseMaxSize");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }

        public static string GetServiceTierSql([NotNull] this IReadOnlyModel model)
        {
            return (string)((IReadOnlyAnnotatable)model)["Dm:ServiceTierSql"];
        }

        public static void SetServiceTierSql([NotNull] this IMutableModel model, [CanBeNull] string value)
        {
            model.SetOrRemoveAnnotation("Dm:ServiceTierSql", value);
        }

        public static void SetServiceTierSql([NotNull] this IConventionModel model, [CanBeNull] string value, bool fromDataAnnotation = false)
        {
            model.SetOrRemoveAnnotation("Dm:ServiceTierSql", value, fromDataAnnotation);
        }

        public static ConfigurationSource? GetServiceTierSqlConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:ServiceTierSql");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }

        public static string GetPerformanceLevelSql([NotNull] this IReadOnlyModel model)
        {
            return (string)((IReadOnlyAnnotatable)model)["Dm:PerformanceLevelSql"];
        }

        public static void SetPerformanceLevelSql([NotNull] this IMutableModel model, [CanBeNull] string value)
        {
            model.SetOrRemoveAnnotation("Dm:PerformanceLevelSql", value);
        }

        public static void SetPerformanceLevelSql([NotNull] this IConventionModel model, [CanBeNull] string value, bool fromDataAnnotation = false)
        {
            model.SetOrRemoveAnnotation("Dm:PerformanceLevelSql", value, fromDataAnnotation);
        }

        public static ConfigurationSource? GetPerformanceLevelSqlConfigurationSource([NotNull] this IConventionModel model)
        {
            IConventionAnnotation obj = model.FindAnnotation("Dm:PerformanceLevelSql");
            if (obj == null)
            {
                return null;
            }
            return obj.GetConfigurationSource();
        }
    }
}
