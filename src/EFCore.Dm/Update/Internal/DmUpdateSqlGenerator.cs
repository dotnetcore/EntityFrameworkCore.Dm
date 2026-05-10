using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Dm.Update.Internal
{
    public class DmUpdateSqlGenerator : UpdateSqlGenerator, IDmUpdateSqlGenerator, IUpdateSqlGenerator
    {
        private const string InsertedTableBaseName = "@inserted";

        private const string ToInsertTableAlias = "i";

        private const string PositionColumnName = "_Position";

        private const string PositionColumnDeclaration = "\"_Position\" int";

        private const string FullPositionColumnName = "i._Position";

        public DmUpdateSqlGenerator([NotNull] UpdateSqlGeneratorDependencies dependencies)
            : base(dependencies)
        {
        }

        public virtual ResultSetMapping AppendBulkInsertOperation(StringBuilder commandStringBuilder, IReadOnlyList<IReadOnlyModificationCommand> modificationCommands, int commandPosition)
        {
            if (modificationCommands.Count == 1 && modificationCommands[0].ColumnModifications.All(o =>
            {
                if (o.IsKey && o.IsRead)
                {
                    IProperty property2 = o.Property;
                    if (property2 == null)
                    {
                        return false;
                    }
                    return property2.GetValueGenerationStrategy() == DmValueGenerationStrategy.IdentityColumn;
                }
                return true;
            }))
            {
                return AppendInsertOperation(commandStringBuilder, modificationCommands[0], commandPosition);
            }
            List<IColumnModification> readOperations = (from o in modificationCommands[0].ColumnModifications
                                                        where o.IsRead
                                                        select o).ToList();
            List<IColumnModification> writeOperations = (from o in modificationCommands[0].ColumnModifications
                                                         where o.IsWrite
                                                         select o).ToList();
            List<IColumnModification> keyOperations = (from o in modificationCommands[0].ColumnModifications
                                                       where o.IsKey
                                                       select o).ToList();
            bool noWriteOps = writeOperations.Count == 0;
            List<IColumnModification> nonIdentityOps = modificationCommands[0].ColumnModifications.Where(o =>
            {
                IProperty property = o.Property;
                return property == null || property.GetValueGenerationStrategy() != DmValueGenerationStrategy.IdentityColumn;
            }).ToList();
            if (noWriteOps)
            {
                if (nonIdentityOps.Count == 0 || readOperations.Count == 0)
                {
                    foreach (IReadOnlyModificationCommand modificationCommand in modificationCommands)
                    {
                        AppendInsertOperation(commandStringBuilder, modificationCommand, commandPosition);
                    }
                    if (readOperations.Count == 0)
                    {
                        return 0;
                    }
                    return (ResultSetMapping)5;
                }
                if (nonIdentityOps.Count > 1)
                {
                    nonIdentityOps.RemoveRange(1, nonIdentityOps.Count - 1);
                }
            }
            if (readOperations.Count == 0)
            {
                return AppendBulkInsertWithoutServerValues(commandStringBuilder, modificationCommands, writeOperations);
            }
            if (noWriteOps)
            {
                return AppendBulkInsertWithOnlyDefaultValues(commandStringBuilder, modificationCommands, commandPosition, nonIdentityOps, keyOperations, readOperations);
            }
            return AppendBulkInsertWithServerValues(commandStringBuilder, modificationCommands, commandPosition, writeOperations, keyOperations, readOperations);
        }

        private ResultSetMapping AppendBulkInsertWithoutServerValues(StringBuilder commandStringBuilder, IReadOnlyList<IReadOnlyModificationCommand> modificationCommands, List<IColumnModification> writeOperations)
        {
            string tableName = modificationCommands[0].TableName;
            string schema = modificationCommands[0].Schema;
            AppendInsertCommandHeader(commandStringBuilder, tableName, schema, writeOperations);
            AppendValuesHeader(commandStringBuilder, writeOperations);
            AppendValues(commandStringBuilder, writeOperations, null);
            for (int i = 1; i < modificationCommands.Count; i++)
            {
                commandStringBuilder.Append(",").AppendLine();
                AppendValues(commandStringBuilder, (from o in modificationCommands[i].ColumnModifications
                                                    where o.IsWrite
                                                    select o).ToList(), null);
            }
            commandStringBuilder.Append(SqlGenerationHelper.StatementTerminator).AppendLine();
            return (ResultSetMapping)0;
        }

        private void AppendSelectIdentity(StringBuilder commandStringBuilder, IReadOnlyList<IReadOnlyModificationCommand> modificationCommands, List<IColumnModification> nonwrite_keys, int commandPosition)
        {
            StringBuilder stringBuilder = commandStringBuilder.Append(" RETURN ").AppendJoin(nonwrite_keys, SqlGenerationHelper, (StringBuilder sb, IColumnModification o, ISqlGenerationHelper helper) =>
            {
                helper.DelimitIdentifier(sb, o.ColumnName);
            }, ",");
            StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder);
            handler.AppendLiteral(" INTO c");
            handler.AppendFormatted(commandPosition);
            stringBuilder.Append(ref handler).Append(SqlGenerationHelper.StatementTerminator).AppendLine();
        }

        private ResultSetMapping AppendBulkInsertWithOnlyDefaultValues(StringBuilder commandStringBuilder, IReadOnlyList<IReadOnlyModificationCommand> modificationCommands, int commandPosition, List<IColumnModification> writeOperations, List<IColumnModification> keyOperations, List<IColumnModification> readOperations)
        {
            if (readOperations != null && readOperations.Count > 0)
            {
                AppendDeclareTable(commandStringBuilder, InsertedTableBaseName, commandPosition, readOperations, PositionColumnDeclaration);
            }
            string tableName = modificationCommands[0].TableName;
            string schema = modificationCommands[0].Schema;
            commandStringBuilder.AppendLine("BEGIN");
            if (readOperations != null && readOperations.Count > 0)
            {
                commandStringBuilder.Append("c").Append(commandPosition).Append(" = NEW rrr")
                    .Append(commandPosition)
                    .Append("[")
                    .Append(modificationCommands.Count.ToString())
                    .AppendLine("];");
            }
            AppendInsertCommandHeader(commandStringBuilder, tableName, schema, writeOperations);
            AppendValuesHeader(commandStringBuilder, writeOperations);
            AppendValues(commandStringBuilder, writeOperations, null);
            for (int i = 1; i < modificationCommands.Count; i++)
            {
                commandStringBuilder.Append(",").AppendLine();
                AppendValues(commandStringBuilder, writeOperations, null);
            }
            if (readOperations != null && readOperations.Count > 0)
            {
                AppendSelectIdentity(commandStringBuilder, modificationCommands, readOperations, commandPosition);
                StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, commandStringBuilder);
                handler.AppendLiteral(" SELECT * FROM ARRAY c");
                handler.AppendFormatted(commandPosition);
                commandStringBuilder.Append(ref handler);
            }
            commandStringBuilder.Append(SqlGenerationHelper.StatementTerminator).AppendLine();
            commandStringBuilder.AppendLine(" END;").AppendLine();
            return (ResultSetMapping)3;
        }

        private ResultSetMapping AppendBulkInsertWithServerValues(StringBuilder commandStringBuilder, IReadOnlyList<IReadOnlyModificationCommand> modificationCommands, int commandPosition, List<IColumnModification> writeOperations, List<IColumnModification> keyOperations, List<IColumnModification> readOperations)
        {
            if (readOperations != null && readOperations.Count > 0)
            {
                AppendDeclareTable(commandStringBuilder, InsertedTableBaseName, commandPosition, readOperations, PositionColumnDeclaration);
            }
            string tableName = modificationCommands[0].TableName;
            string schema = modificationCommands[0].Schema;
            commandStringBuilder.AppendLine("BEGIN");
            if (readOperations != null && readOperations.Count > 0)
            {
                commandStringBuilder.Append("c").Append(commandPosition).Append(" = NEW rrr")
                    .Append(commandPosition)
                    .Append("[")
                    .Append(modificationCommands.Count.ToString())
                    .AppendLine("];");
            }
            AppendInsertCommandHeader(commandStringBuilder, tableName, schema, writeOperations);
            AppendValuesHeader(commandStringBuilder, writeOperations);
            AppendValues(commandStringBuilder, writeOperations, null);
            for (int i = 1; i < modificationCommands.Count; i++)
            {
                commandStringBuilder.Append(",").AppendLine();
                AppendValues(commandStringBuilder, (from o in modificationCommands[i].ColumnModifications
                                                    where o.IsWrite
                                                    select o).ToList(), null);
            }
            if (readOperations != null && readOperations.Count > 0)
            {
                AppendSelectIdentity(commandStringBuilder, modificationCommands, readOperations, commandPosition);
                StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, commandStringBuilder);
                handler.AppendLiteral(" SELECT * FROM ARRAY c");
                handler.AppendFormatted(commandPosition);
                commandStringBuilder.Append(ref handler);
            }
            commandStringBuilder.Append(SqlGenerationHelper.StatementTerminator).AppendLine();
            commandStringBuilder.AppendLine(" END;").AppendLine();
            return (ResultSetMapping)3;
        }

        private void AppendValues(StringBuilder commandStringBuilder, IReadOnlyList<IColumnModification> operations, string? additionalLiteral)
        {
            if (operations.Count <= 0)
            {
                return;
            }
            commandStringBuilder.Append("(").AppendJoin(operations, SqlGenerationHelper, (StringBuilder sb, IColumnModification o, ISqlGenerationHelper helper) =>
            {
                if (o.IsWrite)
                {
                    helper.GenerateParameterName(sb, o.ParameterName);
                }
                else if (RelationalPropertyExtensions.GetDefaultValueSql(o.Property) != null)
                {
                    string value = RelationalPropertyExtensions.GetDefaultValueSql(o.Property).ToString();
                    sb.Append(value);
                }
                else
                {
                    sb.Append("DEFAULT");
                }
            });
            if (additionalLiteral != null)
            {
                commandStringBuilder.Append(", ").Append(additionalLiteral);
            }
            commandStringBuilder.Append(")");
        }

        private void AppendDeclareTable(StringBuilder commandStringBuilder, string name, int index, IReadOnlyList<IColumnModification> readOperations, string? additionalColumns = null)
        {
            commandStringBuilder.Append("DECLARE ");
            if (readOperations == null || readOperations.Count <= 0)
            {
                return;
            }
            commandStringBuilder.Append(" TYPE rrr").Append(index).Append(" IS RECORD (")
                .AppendJoin(readOperations, this, (StringBuilder sb, IColumnModification o, DmUpdateSqlGenerator generator) =>
                {
                    generator.SqlGenerationHelper.DelimitIdentifier(sb, o.ColumnName);
                    if (generator.GetTypeNameForCopy(o.Property).Equals("INTEGER identity(1, 1)"))
                    {
                        sb.Append(" ").Append("integer");
                    }
                    else
                    {
                        sb.Append(" ").Append(generator.GetTypeNameForCopy(o.Property));
                    }
                });
            commandStringBuilder.Append(")").Append(SqlGenerationHelper.StatementTerminator).AppendLine();
            commandStringBuilder.Append("TYPE ccc").Append(index).Append(" IS ARRAY rrr")
                .Append(index)
                .AppendLine("[];")
                .AppendLine()
                .Append("c")
                .Append(index)
                .Append(" ccc")
                .Append(index)
                .AppendLine("; ")
                .AppendLine();
        }

        private string GetTypeNameForCopy(IProperty property)
        {
            string columnType = RelationalPropertyExtensions.GetColumnType(property);
            if (columnType == null)
            {
                IProperty principalProperty = property.FindFirstPrincipal();
                object typeResult = ((principalProperty != null) ? RelationalPropertyExtensions.GetColumnType(principalProperty) : null);
                if (typeResult == null)
                {
                    RelationalTypeMapping typeMapping = Dependencies.TypeMappingSource.FindMapping(property.ClrType);
                    typeResult = ((typeMapping != null) ? typeMapping.StoreType : null);
                }
                columnType = (string)typeResult;
            }
            if (property.ClrType == typeof(byte[]) && columnType != null && (columnType.Equals("rowversion", StringComparison.OrdinalIgnoreCase) || columnType.Equals("timestamp", StringComparison.OrdinalIgnoreCase)))
            {
                if (!property.IsNullable)
                {
                    return "binary(8)";
                }
                return "varbinary(8)";
            }
            return columnType;
        }

        public override void AppendBatchHeader(StringBuilder commandStringBuilder)
        {
            commandStringBuilder.AppendLine();
        }

        public override string GenerateNextSequenceValueOperation(string name, string schema)
        {
            StringBuilder stringBuilder = new StringBuilder();
            AppendNextSequenceValueOperation(stringBuilder, name, schema);
            return stringBuilder.ToString();
        }

        public override void AppendNextSequenceValueOperation(StringBuilder commandStringBuilder, string name, string schema)
        {
            commandStringBuilder.Append("SELECT ");
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, name, schema);
            commandStringBuilder.Append(".NEXTVAL");
        }

        public override ResultSetMapping AppendInsertReturningOperation(StringBuilder commandStringBuilder, IReadOnlyModificationCommand command, int commandPosition, out bool requiresTransaction)
        {
            string tableName = command.TableName;
            string schema = command.Schema;
            IReadOnlyList<IColumnModification> columnModifications = command.ColumnModifications;
            List<IColumnModification> writeOperations = columnModifications.Where(o => o.IsWrite).ToList();
            List<IColumnModification> readOperations = columnModifications.Where(o => o.IsRead).ToList();
            AppendInsertCommand(commandStringBuilder, tableName, schema, writeOperations);
            requiresTransaction = false;
            if (readOperations.Count > 0)
            {
                List<IColumnModification> conditionOperations = columnModifications.Where(o => o.IsKey).ToList();
                return AppendSelectAffectedCommand(commandStringBuilder, tableName, schema, readOperations, conditionOperations, commandPosition);
            }
            return 0;
        }

        protected override ResultSetMapping AppendUpdateReturningOperation(StringBuilder commandStringBuilder, IReadOnlyModificationCommand command, int commandPosition, out bool requiresTransaction)
        {
            string tableName = command.TableName;
            string schema = command.Schema;
            IReadOnlyList<IColumnModification> columnModifications = command.ColumnModifications;
            List<IColumnModification> writeOperations = columnModifications.Where(o => o.IsWrite).ToList();
            List<IColumnModification> conditionOperations = columnModifications.Where(o => o.IsCondition).ToList();
            List<IColumnModification> readOperations = columnModifications.Where(o => o.IsRead).ToList();
            requiresTransaction = false;
            AppendUpdateCommand(commandStringBuilder, tableName, schema, writeOperations, conditionOperations);
            if (readOperations.Count > 0)
            {
                List<IColumnModification> keyConditionOperations = columnModifications.Where(o => o.IsKey).ToList();
                return AppendSelectAffectedCommand(commandStringBuilder, tableName, schema, readOperations, keyConditionOperations, commandPosition);
            }
            return AppendSelectAffectedCountCommand(commandStringBuilder);
        }

        protected override ResultSetMapping AppendDeleteReturningOperation(StringBuilder commandStringBuilder, IReadOnlyModificationCommand command, int commandPosition, out bool requiresTransaction)
        {
            string tableName = command.TableName;
            string schema = command.Schema;
            List<IColumnModification> conditionOperations = (from o in command.ColumnModifications
                                                             where o.IsCondition
                                                             select o).ToList();
            requiresTransaction = false;
            AppendDeleteCommand(commandStringBuilder, tableName, schema, conditionOperations);
            return AppendSelectAffectedCountCommand(commandStringBuilder);
        }

        protected void AppendInsertCommand(StringBuilder commandStringBuilder, string name, string? schema, IReadOnlyList<IColumnModification> writeOperations)
        {
            AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
            AppendValuesHeader(commandStringBuilder, writeOperations);
            AppendValues(commandStringBuilder, name, schema, writeOperations);
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
        }

        protected void AppendUpdateCommand(StringBuilder commandStringBuilder, string name, string? schema, IReadOnlyList<IColumnModification> writeOperations, IReadOnlyList<IColumnModification> conditionOperations)
        {
            AppendUpdateCommandHeader(commandStringBuilder, name, schema, writeOperations);
            AppendWhereClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
        }

        protected void AppendDeleteCommand(StringBuilder commandStringBuilder, string name, string? schema, IReadOnlyList<IColumnModification> conditionOperations)
        {
            AppendDeleteCommandHeader(commandStringBuilder, name, schema);
            AppendWhereClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
        }

        protected virtual ResultSetMapping AppendSelectAffectedCommand(StringBuilder commandStringBuilder, string name, string? schema, IReadOnlyList<IColumnModification> readOperations, IReadOnlyList<IColumnModification> conditionOperations, int commandPosition)
        {
            AppendSelectCommandHeader(commandStringBuilder, readOperations);
            AppendFromClause(commandStringBuilder, name, schema);
            AppendWhereAffectedClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator).AppendLine();
            return (ResultSetMapping)5;
        }

        protected void AppendSelectCommandHeader(StringBuilder commandStringBuilder, IReadOnlyList<IColumnModification> operations)
        {
            commandStringBuilder.Append("SELECT ").AppendJoin(operations, SqlGenerationHelper, (StringBuilder sb, IColumnModification o, ISqlGenerationHelper helper) =>
            {
                helper.DelimitIdentifier(sb, o.ColumnName);
            });
        }

        protected void AppendFromClause(StringBuilder commandStringBuilder, string name, string? schema)
        {
            commandStringBuilder.AppendLine().Append("FROM ");
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, name, schema);
        }

        protected void AppendWhereAffectedClause(StringBuilder commandStringBuilder, IReadOnlyList<IColumnModification> operations)
        {
            commandStringBuilder.AppendLine().Append("WHERE ");
            AppendRowsAffectedWhereCondition(commandStringBuilder, 1);
            if (operations.Count <= 0)
            {
                return;
            }
            commandStringBuilder.Append(" AND ").AppendJoin(operations, (StringBuilder sb, IColumnModification v) =>
            {
                if (v.IsKey && !v.IsRead)
                {
                    AppendWhereCondition(sb, v, v.UseOriginalValueParameter);
                }
                else if (IsIdentityOperation(v))
                {
                    AppendIdentityWhereCondition(sb, v);
                }
            }, " AND ");
        }

        protected void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
        {
            commandStringBuilder.Append("sql%ROWCOUNT = ").Append(expectedRowsAffected.ToString(CultureInfo.InvariantCulture));
        }

        protected bool IsIdentityOperation(IColumnModification modification)
        {
            if (modification.IsKey)
            {
                return modification.IsRead;
            }
            return false;
        }

        protected void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, IColumnModification columnModification)
        {
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, columnModification.ColumnName);
            commandStringBuilder.Append(" = ");
            object defaultValueSql = ((IReadOnlyAnnotatable)columnModification.Property)["Relational:DefaultValueSql"];
            if (defaultValueSql != null)
            {
                string text = Convert.ToString(defaultValueSql);
                text = text.Replace(".NEXTVAL", ".CURRVAL");
                commandStringBuilder.Append(text);
            }
            else
            {
                commandStringBuilder.Append("scope_identity()");
            }
        }

        protected ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder)
        {
            commandStringBuilder.Append("/*EFCOREROWCOUNT*/SELECT sql%ROWCOUNT").Append(SqlGenerationHelper.StatementTerminator).AppendLine()
                .AppendLine();
            return (ResultSetMapping)5;
        }

        public override void AppendObtainNextSequenceValueOperation(StringBuilder commandStringBuilder, string name, string? schema)
        {
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, name, schema);
            commandStringBuilder.Append(".NEXTVAL");
        }
    }
}
