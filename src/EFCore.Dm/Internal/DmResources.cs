using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Diagnostics.Internal;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace Microsoft.EntityFrameworkCore.Dm.Internal
{
    public static class DmResources
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("Microsoft.EntityFrameworkCore.Dm.Properties.DmStrings", typeof(DmResources).GetTypeInfo().Assembly);

        public static EventDefinition<string, string> LogDefaultDecimalTypeColumn([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogDefaultDecimalTypeColumn;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogDefaultDecimalTypeColumn, () => new EventDefinition<string, string>(logger.Options, DmEventId.DecimalTypeDefaultWarning, LogLevel.Warning, "DmEventId.DecimalTypeDefaultWarning", level => LoggerMessage.Define<string, string>(level, DmEventId.DecimalTypeDefaultWarning, _resourceManager.GetString("LogDefaultDecimalTypeColumn"))));
            return (EventDefinition<string, string>)def;
        }

        public static EventDefinition<string, string> LogByteIdentityColumn([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogByteIdentityColumn;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogByteIdentityColumn, () => new EventDefinition<string, string>(logger.Options, DmEventId.ByteIdentityColumnWarning, LogLevel.Warning, "DmEventId.ByteIdentityColumnWarning", level => LoggerMessage.Define<string, string>(level, DmEventId.ByteIdentityColumnWarning, _resourceManager.GetString("LogByteIdentityColumn"))));
            return (EventDefinition<string, string>)def;
        }

        public static EventDefinition<string, string, string, string> LogConflictingValueGenerationStrategies(IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogConflictingValueGenerationStrategies;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogConflictingValueGenerationStrategies, () => new EventDefinition<string, string, string, string>(logger.Options, DmEventId.ConflictingValueGenerationStrategiesWarning, LogLevel.Warning, "DmEventId.ConflictingValueGenerationStrategiesWarning", level => LoggerMessage.Define<string, string, string, string>(level, DmEventId.ConflictingValueGenerationStrategiesWarning, _resourceManager.GetString("LogConflictingValueGenerationStrategies"))));
            return (EventDefinition<string, string, string, string>)def;
        }

        public static EventDefinition<string> LogFoundDefaultSchema([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundDefaultSchema;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundDefaultSchema, () => new EventDefinition<string>(logger.Options, DmEventId.DefaultSchemaFound, LogLevel.Debug, "DmEventId.DefaultSchemaFound", level => LoggerMessage.Define<string>(level, DmEventId.DefaultSchemaFound, _resourceManager.GetString("LogFoundDefaultSchema"))));
            return (EventDefinition<string>)def;
        }

        public static FallbackEventDefinition LogFoundColumn([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundColumn;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundColumn, () => new FallbackEventDefinition(logger.Options, DmEventId.ColumnFound, LogLevel.Debug, "DmEventId.ColumnFound", _resourceManager.GetString("LogFoundColumn")));
            return (FallbackEventDefinition)def;
        }

        public static EventDefinition<string, string, string, string> LogFoundForeignKey([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundForeignKey;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundForeignKey, () => new EventDefinition<string, string, string, string>(logger.Options, DmEventId.ForeignKeyFound, LogLevel.Debug, "DmEventId.ForeignKeyFound", level => LoggerMessage.Define<string, string, string, string>(level, DmEventId.ForeignKeyFound, _resourceManager.GetString("LogFoundForeignKey"))));
            return (EventDefinition<string, string, string, string>)def;
        }

        public static EventDefinition<string, string, bool> LogFoundIndex([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundIndex;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundIndex, () => new EventDefinition<string, string, bool>(logger.Options, DmEventId.IndexFound, LogLevel.Debug, "DmEventId.IndexFound", level => LoggerMessage.Define<string, string, bool>(level, DmEventId.IndexFound, _resourceManager.GetString("LogFoundIndex"))));
            return (EventDefinition<string, string, bool>)def;
        }

        public static EventDefinition<string, string> LogFoundPrimaryKey([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundPrimaryKey;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundPrimaryKey, () => new EventDefinition<string, string>(logger.Options, DmEventId.PrimaryKeyFound, LogLevel.Debug, "DmEventId.PrimaryKeyFound", level => LoggerMessage.Define<string, string>(level, DmEventId.PrimaryKeyFound, _resourceManager.GetString("LogFoundPrimaryKey"))));
            return (EventDefinition<string, string>)def;
        }

        public static EventDefinition<string> LogFoundTable([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundTable;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundTable, () => new EventDefinition<string>(logger.Options, DmEventId.TableFound, LogLevel.Debug, "DmEventId.TableFound", level => LoggerMessage.Define<string>(level, DmEventId.TableFound, _resourceManager.GetString("LogFoundTable"))));
            return (EventDefinition<string>)def;
        }

        public static EventDefinition<string, string> LogFoundUniqueConstraint([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundUniqueConstraint;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundUniqueConstraint, () => new EventDefinition<string, string>(logger.Options, DmEventId.UniqueConstraintFound, LogLevel.Debug, "DmEventId.UniqueConstraintFound", level => LoggerMessage.Define<string, string>(level, DmEventId.UniqueConstraintFound, _resourceManager.GetString("LogFoundUniqueConstraint"))));
            return (EventDefinition<string, string>)def;
        }

        public static EventDefinition<string> LogMissingSchema([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogMissingSchema;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogMissingSchema, () => new EventDefinition<string>(logger.Options, DmEventId.MissingSchemaWarning, LogLevel.Warning, "DmEventId.MissingSchemaWarning", level => LoggerMessage.Define<string>(level, DmEventId.MissingSchemaWarning, _resourceManager.GetString("LogMissingSchema"))));
            return (EventDefinition<string>)def;
        }

        public static EventDefinition<string> LogMissingTable([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogMissingTable;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogMissingTable, () => new EventDefinition<string>(logger.Options, DmEventId.MissingTableWarning, LogLevel.Warning, "DmEventId.MissingTableWarning", level => LoggerMessage.Define<string>(level, DmEventId.MissingTableWarning, _resourceManager.GetString("LogMissingTable"))));
            return (EventDefinition<string>)def;
        }

        public static EventDefinition<string, string, string, string> LogPrincipalColumnNotFound([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogPrincipalColumnNotFound;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogPrincipalColumnNotFound, () => new EventDefinition<string, string, string, string>(logger.Options, DmEventId.ForeignKeyPrincipalColumnMissingWarning, LogLevel.Warning, "DmEventId.ForeignKeyPrincipalColumnMissingWarning", level => LoggerMessage.Define<string, string, string, string>(level, DmEventId.ForeignKeyPrincipalColumnMissingWarning, _resourceManager.GetString("LogPrincipalColumnNotFound"))));
            return (EventDefinition<string, string, string, string>)def;
        }

        public static EventDefinition<string, string, string> LogPrincipalTableNotInSelectionSet([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogPrincipalTableNotInSelectionSet;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogPrincipalTableNotInSelectionSet, () => new EventDefinition<string, string, string>(logger.Options, DmEventId.ForeignKeyReferencesMissingPrincipalTableWarning, LogLevel.Warning, "DmEventId.ForeignKeyReferencesMissingPrincipalTableWarning", level => LoggerMessage.Define<string, string, string>(level, DmEventId.ForeignKeyReferencesMissingPrincipalTableWarning, _resourceManager.GetString("LogPrincipalTableNotInSelectionSet"))));
            return (EventDefinition<string, string, string>)def;
        }

        public static FallbackEventDefinition LogFoundSequence([NotNull] IDiagnosticsLogger logger)
        {
            var definitions = (DmLoggingDefinitions)logger.Definitions;
            EventDefinitionBase def = definitions.LogFoundSequence;
            def ??= LazyInitializer.EnsureInitialized(ref definitions.LogFoundSequence, () => new FallbackEventDefinition(logger.Options, DmEventId.SequenceFound, LogLevel.Debug, "DmEventId.SequenceFound", _resourceManager.GetString("LogFoundSequence")));
            return (FallbackEventDefinition)def;
        }
    }
}
