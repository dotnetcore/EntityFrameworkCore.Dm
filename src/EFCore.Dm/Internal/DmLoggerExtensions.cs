using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Internal
{
    public static class DmLoggerExtensions
    {
        public static void DecimalTypeDefaultWarning([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics, [NotNull] IProperty property)
        {
            EventDefinition<string, string> eventDef = DmResources.LogDefaultDecimalTypeColumn(diagnostics);
            if ((diagnostics).ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, property.Name, property.DeclaringType.DisplayName());
            }
            if (diagnostics.NeedsEventData(eventDef, out bool sourceEnabled, out bool simpleLogEnabled))
            {
                PropertyEventData eventData = new PropertyEventData(eventDef, DecimalTypeDefaultWarning, property);
                diagnostics.DispatchEventData(eventDef, eventData, sourceEnabled, simpleLogEnabled);
            }
        }

        private static string DecimalTypeDefaultWarning(EventDefinitionBase definition, EventData payload)
        {
            EventDefinition<string, string> typedDef = (EventDefinition<string, string>)definition;
            PropertyEventData eventData = (PropertyEventData)payload;
            return typedDef.GenerateMessage((eventData.Property).Name, (eventData.Property.DeclaringType).DisplayName());
        }

        public static void ByteIdentityColumnWarning([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics, [NotNull] IProperty property)
        {
            EventDefinition<string, string> eventDef = DmResources.LogByteIdentityColumn(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, property.Name, property.DeclaringType.DisplayName());
            }
            if (diagnostics.NeedsEventData(eventDef, out bool sourceEnabled, out bool simpleLogEnabled))
            {
                PropertyEventData eventData = new PropertyEventData(eventDef, ByteIdentityColumnWarning, property);
                diagnostics.DispatchEventData(eventDef, eventData, sourceEnabled, simpleLogEnabled);
            }
        }

        private static string ByteIdentityColumnWarning(EventDefinitionBase definition, EventData payload)
        {
            EventDefinition<string, string> typedDef = (EventDefinition<string, string>)definition;
            PropertyEventData eventData = (PropertyEventData)payload;
            return typedDef.GenerateMessage((eventData.Property).Name, eventData.Property.DeclaringType.DisplayName());
        }

        public static void ConflictingValueGenerationStrategiesWarning(this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics, DmValueGenerationStrategy DmValueGenerationStrategy, string otherValueGenerationStrategy, IReadOnlyProperty property)
        {
            EventDefinition<string, string, string, string> eventDef = DmResources.LogConflictingValueGenerationStrategies(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, DmValueGenerationStrategy.ToString(), otherValueGenerationStrategy, property.Name, property.DeclaringType.DisplayName());
            }
            if ((diagnostics).NeedsEventData(eventDef, out bool sourceEnabled, out bool simpleLogEnabled))
            {
                ConflictingValueGenerationStrategiesEventData eventData = new ConflictingValueGenerationStrategiesEventData(eventDef, ConflictingValueGenerationStrategiesWarning, DmValueGenerationStrategy, otherValueGenerationStrategy, property);
                diagnostics.DispatchEventData(eventDef, eventData, sourceEnabled, simpleLogEnabled);
            }
        }

        private static string ConflictingValueGenerationStrategiesWarning(EventDefinitionBase definition, EventData payload)
        {
            EventDefinition<string, string, string, string> typedDef = (EventDefinition<string, string, string, string>)definition;
            ConflictingValueGenerationStrategiesEventData eventData = (ConflictingValueGenerationStrategiesEventData)payload;
            return typedDef.GenerateMessage(eventData.DmValueGenerationStrategy.ToString(), eventData.OtherValueGenerationStrategy, ((IReadOnlyPropertyBase)eventData.Property).Name, eventData.Property.DeclaringType.DisplayName());
        }

        public static void ColumnFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string tableName, [NotNull] string columnName, int ordinal, [NotNull] string dataTypeName, int maxLength, int precision, int scale, bool nullable, bool identity, [CanBeNull] string defaultValue, [CanBeNull] string computedValue, bool? stored)
        {
            FallbackEventDefinition definition = DmResources.LogFoundColumn(diagnostics);
            if (diagnostics.ShouldLog(definition))
            {
                definition.Log(diagnostics, l =>
                {
                    l.LogDebug(definition.EventId, null, definition.MessageFormat, tableName, columnName, ordinal, dataTypeName, maxLength, precision, scale, nullable, identity, defaultValue, computedValue, stored);
                });
            }
        }

        public static void ForeignKeyFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string foreignKeyName, [NotNull] string tableName, [NotNull] string principalTableName, [NotNull] string onDeleteAction)
        {
            EventDefinition<string, string, string, string> eventDef = DmResources.LogFoundForeignKey(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, foreignKeyName, tableName, principalTableName, onDeleteAction);
            }
        }

        public static void DefaultSchemaFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string schemaName)
        {
            EventDefinition<string> eventDef = DmResources.LogFoundDefaultSchema(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, schemaName);
            }
        }

        public static void PrimaryKeyFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string primaryKeyName, [NotNull] string tableName)
        {
            EventDefinition<string, string> eventDef = DmResources.LogFoundPrimaryKey(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, primaryKeyName, tableName);
            }
        }

        public static void UniqueConstraintFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string uniqueConstraintName, [NotNull] string tableName)
        {
            EventDefinition<string, string> eventDef = DmResources.LogFoundUniqueConstraint(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, uniqueConstraintName, tableName);
            }
        }

        public static void IndexFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string indexName, [NotNull] string tableName, bool unique)
        {
            EventDefinition<string, string, bool> eventDef = DmResources.LogFoundIndex(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, indexName, tableName, unique, null);
            }
        }

        public static void ForeignKeyReferencesMissingPrincipalTableWarning([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [CanBeNull] string foreignKeyName, [CanBeNull] string tableName, [CanBeNull] string principalTableName)
        {
            EventDefinition<string, string, string> eventDef = DmResources.LogPrincipalTableNotInSelectionSet((IDiagnosticsLogger)diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, foreignKeyName, tableName, principalTableName, null);
            }
        }

        public static void ForeignKeyPrincipalColumnMissingWarning([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string foreignKeyName, [NotNull] string tableName, [NotNull] string principalColumnName, [NotNull] string principalTableName)
        {
            EventDefinition<string, string, string, string> eventDef = DmResources.LogPrincipalColumnNotFound((IDiagnosticsLogger)diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, foreignKeyName, tableName, principalColumnName, principalTableName);
            }
        }

        public static void MissingSchemaWarning([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [CanBeNull] string schemaName)
        {
            EventDefinition<string> eventDef = DmResources.LogMissingSchema(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, schemaName);
            }
        }

        public static void MissingTableWarning([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [CanBeNull] string tableName)
        {
            EventDefinition<string> eventDef = DmResources.LogMissingTable(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, tableName);
            }
        }

        public static void SequenceFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string sequenceName, [NotNull] string sequenceTypeName, bool? cyclic, int? increment, long? start, long? min, long? max)
        {
            FallbackEventDefinition definition = DmResources.LogFoundSequence(diagnostics);
            if (diagnostics.ShouldLog(definition))
            {
                definition.Log(diagnostics, l =>
                {
                    l.LogDebug(definition.EventId, null, definition.MessageFormat, sequenceName, sequenceTypeName, cyclic, increment, start, min, max);
                });
            }
        }

        public static void TableFound([NotNull] this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, [NotNull] string tableName)
        {
            EventDefinition<string> eventDef = DmResources.LogFoundTable(diagnostics);
            if (diagnostics.ShouldLog(eventDef))
            {
                eventDef.Log(diagnostics, tableName);
            }
        }
    }
}
