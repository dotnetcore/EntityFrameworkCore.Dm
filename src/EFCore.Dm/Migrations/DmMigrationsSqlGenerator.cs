using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using System;
using System.Linq;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    public class DmMigrationsSqlGenerator : MigrationsSqlGenerator
    {
        public DmMigrationsSqlGenerator([NotNull] MigrationsSqlGeneratorDependencies dependencies, [NotNull] IMigrationsAnnotationProvider migrationsAnnotations)
            : base(dependencies)
        {
        }

        protected override void ColumnDefinition(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            ColumnDefinition(operation.Schema, operation.Table, operation.Name, operation, model, builder);
        }

        protected override void Generate(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            if (operation is DmCreateUserOperation dmCreateUserOperation)
            {
                Generate(dmCreateUserOperation, model, builder);
                return;
            }
            if (operation is DmDropSchemaOperation dmDropSchemaOperation)
            {
                Generate(dmDropSchemaOperation, model, builder);
                return;
            }
            if (operation is DmCreateSchemaOperation dmCreateSchemaOperation)
            {
                Generate(dmCreateSchemaOperation, model, builder);
                return;
            }
            if (operation is DmDropUserOperation dmDropUserOperation)
            {
                Generate(dmDropUserOperation, model, builder);
                return;
            }
            if (operation is CreateTableOperation createTableOperation)
            {
                Generate(createTableOperation, model, builder, true);
            }
            else
            {
                base.Generate(operation, model, builder);
            }
        }

        protected override void Generate([NotNull] AlterDatabaseOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            throw new NotSupportedException("AlterDatabaseOperation does not support");
        }

        protected override void Generate([NotNull] CreateSequenceOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            builder.Append("CREATE SEQUENCE ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema));
            RelationalTypeMapping mapping = RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, operation.ClrType);
            builder.Append(" START WITH ").Append(mapping.GenerateSqlLiteral(operation.StartValue));
            SequenceOptions(operation, model, builder);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            EndStatement(builder, false);
        }

        protected override void Generate(AlterColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            var col = (ColumnOperation)operation;
            if (model != null)
            {
                ITable table = RelationalModelExtensions.GetRelationalModel(model).FindTable(col.Table, col.Schema);
                table?.Columns.FirstOrDefault(c => c.Name == col.Name);
            }
            if (col.ComputedColumnSql != null)
            {
                DropColumnOperation dropColumnOperation = new DropColumnOperation();
                dropColumnOperation.Schema = col.Schema;
                dropColumnOperation.Table = col.Table;
                dropColumnOperation.Name = col.Name;
                AddColumnOperation addColumnOperation = new AddColumnOperation();
                addColumnOperation.Schema = col.Schema;
                addColumnOperation.Table = col.Table;
                addColumnOperation.Name = col.Name;
                addColumnOperation.ClrType = col.ClrType;
                addColumnOperation.ColumnType = col.ColumnType;
                addColumnOperation.IsUnicode = col.IsUnicode;
                addColumnOperation.MaxLength = col.MaxLength;
                addColumnOperation.IsRowVersion = col.IsRowVersion;
                addColumnOperation.IsNullable = col.IsNullable;
                addColumnOperation.DefaultValue = col.DefaultValue;
                addColumnOperation.DefaultValueSql = col.DefaultValueSql;
                addColumnOperation.ComputedColumnSql = col.ComputedColumnSql;
                addColumnOperation.IsFixedLength = col.IsFixedLength;
                addColumnOperation.Comment = col.Comment;
                addColumnOperation.AddAnnotations(operation.GetAnnotations());
                Generate(dropColumnOperation, model, builder, true);
                Generate(addColumnOperation, model, builder);
                return;
            }
            bool isIdentity = (((AnnotatableBase)operation)["Dm:ValueGenerationStrategy"] as DmValueGenerationStrategy?).GetValueOrDefault() == DmValueGenerationStrategy.IdentityColumn;
            if (IsOldColumnSupported(model))
            {
                if ((((AnnotatableBase)operation.OldColumn)["Dm:ValueGenerationStrategy"] as DmValueGenerationStrategy?).GetValueOrDefault() == DmValueGenerationStrategy.IdentityColumn && !isIdentity)
                {
                    DropIdentity(operation, builder);
                }
                if (operation.OldColumn.DefaultValue != null || (operation.OldColumn.DefaultValueSql != null && (col.DefaultValue == null || col.DefaultValueSql == null)))
                {
                    DropDefaultConstraint(col.Schema, col.Table, col.Name, builder);
                }
            }
            else
            {
                if (!isIdentity)
                {
                    DropIdentity(operation, builder);
                }
                if (col.DefaultValue == null && col.DefaultValueSql == null)
                {
                    DropDefaultConstraint(col.Schema, col.Table, col.Name, builder);
                }
            }
            builder.Append("ALTER TABLE ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(col.Table, col.Schema)).Append(" MODIFY ");
            ColumnDefinition(col.Schema, col.Table, col.Name, col, model, builder);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand(false);
            if (operation.OldColumn.Comment != col.Comment && col.Comment != null)
            {
                Comment(builder, col.Comment, col.Schema, col.Table, col.Name);
            }
        }

        private static void DropIdentity(AlterColumnOperation operation, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            var col = (ColumnOperation)operation;
            string text = $@"
DECLARE
   v_Count INTEGER;
BEGIN
 SELECT
        COUNT(*) INTO v_Count
FROM
        SYSCOLUMNS
WHERE
        ID =
        (
                SELECT
                        ID
                FROM
                        SYSOBJECTS
                WHERE
                        NAME    ='{col.Table}'
                    AND TYPE$   ='SCHOBJ'
                    AND SUBTYPE$='UTAB'
                    AND SCHID   =
                        (
                                SELECT ID FROM SYSOBJECTS WHERE NAME = '{col.Schema}' AND TYPE$='SCH'
                        )
        )
    AND NAME         = '{col.Name}'
    AND INFO2 & 0X01 = 1;
  IF v_Count > 0 THEN
    EXECUTE IMMEDIATE 'ALTER  TABLE ""{col.Table}"" DROP IDENTITY';
  END IF;
END;";
            builder.AppendLine(text).EndCommand(false);
        }

        protected override void Generate(RenameIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            if (operation.NewName != null)
            {
                builder.Append("ALTER INDEX ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name)).Append(" RENAME TO ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName))
                    .Append(Dependencies.SqlGenerationHelper.StatementTerminator);
            }
            builder.EndCommand(false);
        }

        protected override void SequenceOptions(string schema, string name, SequenceOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotEmpty(name, "name");
            Check.NotNull(operation.IncrementBy, "IncrementBy");
            Check.NotNull(operation.IsCyclic, "IsCyclic");
            Check.NotNull(builder, "builder");
            RelationalTypeMapping mapping = RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(int));
            RelationalTypeMapping mapping2 = RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(long));
            builder.Append(" INCREMENT BY ").Append(mapping.GenerateSqlLiteral(operation.IncrementBy));
            if (operation.MinValue.HasValue)
            {
                builder.Append(" MINVALUE ").Append(mapping2.GenerateSqlLiteral(operation.MinValue));
            }
            else
            {
                builder.Append(" NOMINVALUE");
            }
            if (operation.MaxValue.HasValue)
            {
                builder.Append(" MAXVALUE ").Append(mapping2.GenerateSqlLiteral(operation.MaxValue));
            }
            else
            {
                builder.Append(" NOMAXVALUE");
            }
            builder.Append(operation.IsCyclic ? " CYCLE" : " NOCYCLE");
        }

        protected override void Generate(RenameSequenceOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            throw new NotSupportedException("RenameSequenceOperation does not support");
        }

        protected override void Generate([NotNull] RestartSequenceOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            throw new NotSupportedException("RestartSequenceOperation does not support");
        }

        protected override void Generate(RenameTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            if (operation.NewSchema != null)
            {
                throw new NotSupportedException("RenameTableOperation does not support rename newschema");
            }
            if (operation.NewName != null && operation.NewName != operation.Name)
            {
                builder.Append("ALTER TABLE ").Append((operation.Schema != null) ? Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema) : Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name)).Append(" RENAME TO ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName))
                    .Append(Dependencies.SqlGenerationHelper.StatementTerminator)
                    .EndCommand(false);
            }
        }

        protected override void Generate(EnsureSchemaOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            builder.Append($@"DECLARE
                    B BOOLEAN ;
                  BEGIN
                    SELECT COUNT(NAME) INTO B FROM SYSOBJECTS WHERE TYPE$= 'SCH' AND NAME = '{operation.Name}';
                    IF B == 0 THEN
                            EXECUTE IMMEDIATE 'CREATE SCHEMA ""{operation.Name}"" ';
                    END IF;
                    END;").EndCommand(false);
        }

        protected virtual void Generate([NotNull] DmCreateUserOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            builder.Append($@"BEGIN
                             EXECUTE IMMEDIATE 'CREATE USER {operation.UserName} IDENTIFIED BY {operation.Password}';
                             EXECUTE IMMEDIATE 'GRANT DBA TO {operation.UserName}';
                             EXECUTE IMMEDIATE 'CREATE SCHEMA {operation.Schema} AUTHORIZATION {operation.UserName}';
                           END;").EndCommand(true);
        }

        protected virtual void Generate([NotNull] DmDropUserOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            builder.Append("BEGIN\n                         EXECUTE IMMEDIATE 'DROP USER " + operation.UserName + " CASCADE';\n                       END;").EndCommand(true);
        }

        protected virtual void Generate([NotNull] DmCreateSchemaOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            builder.Append($@"BEGIN
                        EXECUTE IMMEDIATE 'CREATE SCHEMA {operation.Schema} AUTHORIZATION {operation.UserName}';
                        END;").EndCommand(true);
        }

        protected virtual void Generate([NotNull] DmDropSchemaOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            builder.Append("BEGIN\n                         EXECUTE IMMEDIATE 'DROP SCHEMA " + operation.Schema + " CASCADE';\n                       END;").EndCommand(true);
        }

        protected override void Generate([NotNull] CreateIndexOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder, bool terminate = true)
        {
            if (operation.Filter != null)
            {
                throw new NotSupportedException("CreateIndexOperation does not support filter clause");
            }
            base.Generate(operation, model, builder, true);
        }

        protected void Generate(DropIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Generate(operation, model, builder, true);
        }

        protected override void Generate([NotNull] DropIndexOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder, bool terminate)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            builder.Append("DROP INDEX ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));
            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator).EndCommand(false);
            }
        }

        protected override void Generate(RenameColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            StringBuilder stringBuilder = new StringBuilder();
            if (operation.Schema != null)
            {
                stringBuilder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Schema)).Append(".");
            }
            stringBuilder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table));
            builder.Append("ALTER TABLE ").Append(stringBuilder.ToString()).Append(" ALTER COLUMN ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
                .Append(" RENAME TO ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName))
                .Append(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand(false);
        }

        protected override void Generate(InsertDataOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            builder.AppendLine("DECLARE").AppendLine("   CNT  INT;").AppendLine("BEGIN ");
            builder.Append(" SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END INTO CNT FROM ").Append(" SYSCOLUMNS WHERE ID = (SELECT ID FROM SYSOBJECTS WHERE TYPE$ = 'SCHOBJ' AND SUBTYPE$ = 'UTAB' AND ");
            if (operation.Schema != null)
            {
                builder.Append(" SCHID = (SELECT ID FROM SYS.SYSOBJECTS WHERE TYPE$ = 'SCH' AND NAME = ").Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(operation.Schema)).Append(") AND ");
            }
            else
            {
                builder.Append(" SCHID = CURRENT_SCHID() AND ");
            }
            builder.Append("NAME = ").Append(RelationalTypeMappingSourceExtensions.GetMapping(Dependencies.TypeMappingSource, typeof(string)).GenerateSqlLiteral(operation.Table)).AppendLine(" ) AND INFO2&0X01 = 1;");
            builder.AppendLine("IF CNT > 0 THEN ");
            using (builder.Indent())
            {
                builder.Append("EXECUTE IMMEDIATE 'SET IDENTITY_INSERT ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema)).Append(" ON '")
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            }
            builder.AppendLine("END IF;");
            base.Generate(operation, model, builder, false);
            builder.AppendLine("IF CNT > 0 THEN ");
            using (builder.Indent())
            {
                builder.Append("EXECUTE IMMEDIATE 'SET IDENTITY_INSERT ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema)).Append(" OFF'")
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            }
            builder.AppendLine("END IF;").AppendLine("END;");
            builder.EndCommand(false);
        }

        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
            bool hasComments = operation.Comment != null || operation.Columns.Any(c => c.Comment != null);
            foreach (var c in operation.Columns)
            {
                if (c.ColumnType != null)
                {
                    if (c.ColumnType.StartsWith("DOUBLE PRECISION", StringComparison.OrdinalIgnoreCase))
                    {
                        c.ColumnType = "DOUBLE PRECISION";
                    }
                    else if (c.ColumnType.StartsWith("ROWID", StringComparison.OrdinalIgnoreCase))
                    {
                        c.ColumnType = "ROWID";
                    }
                }
            }
            if (!hasComments)
            {
                base.Generate(operation, model, builder, true);
            }
            else
            {
                if (!terminate)
                {
                    throw new NotSupportedException("can not Produce Unterminated SQL With Comments");
                }
                base.Generate(operation, model, builder, true);
                if (hasComments)
                {
                    if (operation.Comment != null)
                    {
                        Comment(builder, operation.Comment, operation.Schema, operation.Name);
                    }
                    foreach (AddColumnOperation column in operation.Columns)
                    {
                        if (column.Comment != null)
                        {
                            Comment(builder, column.Comment, operation.Schema, operation.Name, column.Name);
                        }
                    }
                }
            }
            AddColumnOperation[] rowVersionColumns = (from c in operation.Columns
                                                      where c.IsRowVersion
                                                      select c).ToArray();
            if (rowVersionColumns.Length != 0)
            {
                builder.Append("CREATE OR REPLACE TRIGGER ").AppendLine(Dependencies.SqlGenerationHelper.DelimitIdentifier("rowversion_" + operation.Name, operation.Schema)).Append("BEFORE INSERT OR UPDATE ON ")
                    .AppendLine(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                    .AppendLine("FOR EACH ROW")
                    .AppendLine("BEGIN");
                foreach (AddColumnOperation rowVersionCol in rowVersionColumns)
                {
                    builder.Append("  :NEW.").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(rowVersionCol.Name)).Append(" := NVL(:OLD.")
                        .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(rowVersionCol.Name))
                        .Append(", '00000000') + 1")
                        .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                }
                builder.Append("END").AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            }
            EndStatement(builder, false);
        }

        protected override void UniqueConstraint([NotNull] AddUniqueConstraintOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            operation.Name ??= Guid.NewGuid().ToString();
            base.UniqueConstraint(operation, model, builder);
        }

        protected override void ColumnDefinition([CanBeNull] string schema, [NotNull] string table, [NotNull] string name, ColumnOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotEmpty(name, "name");
            Check.NotNull(operation, "operation");
            Check.NotNull(operation.ClrType, "ClrType");
            Check.NotNull(builder, "builder");
            if (operation.ComputedColumnSql != null)
            {
                builder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name)).Append(" AS (").Append(operation.ComputedColumnSql)
                    .Append(")");
                return;
            }
            builder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name)).Append(" ").Append(operation.ColumnType ?? GetColumnType(schema, table, name, operation, model));
            string identityAnnotation = ((AnnotatableBase)operation)["Dm:Identity"] as string;
            if (identityAnnotation != null || (((AnnotatableBase)operation)["Dm:ValueGenerationStrategy"] as DmValueGenerationStrategy?).GetValueOrDefault() == DmValueGenerationStrategy.IdentityColumn)
            {
                builder.Append(" IDENTITY");
                if (!string.IsNullOrEmpty(identityAnnotation) && identityAnnotation != "1, 1")
                {
                    builder.Append("(").Append(identityAnnotation).Append(")");
                }
            }
            else
            {
                DefaultValue(operation.DefaultValue, operation.DefaultValueSql, operation.ColumnType, builder);
            }
            builder.Append(operation.IsNullable ? " NULL" : " NOT NULL");
        }

        protected override void PrimaryKeyConstraint([NotNull] AddPrimaryKeyOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            operation.Name ??= Guid.NewGuid().ToString();
            base.PrimaryKeyConstraint(operation, model, builder);
        }

        protected override void ForeignKeyConstraint(AddForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, "operation");
            Check.NotNull(builder, "builder");
            if (operation.PrincipalColumns == null)
            {
                throw new NotSupportedException("AddForeignKeyOperation does not support references columns is null");
            }
            operation.Name ??= Guid.NewGuid().ToString();
            base.ForeignKeyConstraint(operation, model, builder);
        }

        protected virtual void DropDefaultConstraint([CanBeNull] string schema, [NotNull] string tableName, [NotNull] string columnName, [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotEmpty(tableName, "tableName");
            Check.NotEmpty(columnName, "columnName");
            Check.NotNull(builder, "builder");
            builder.Append("ALTER TABLE ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tableName, schema)).Append(" MODIFY ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(columnName))
                .Append(" DEFAULT NULL")
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                .EndCommand(false);
        }

        protected override void Generate(AddColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate)
        {
            if (operation.Comment == null)
            {
                base.Generate(operation, model, builder, terminate);
                return;
            }
            if (!terminate)
            {
                throw new NotSupportedException("can not Produce Unterminated SQL With Comments");
            }
            base.Generate(operation, model, builder, true);
            Comment(builder, operation.Comment, operation.Schema, operation.Table, operation.Name);
        }

        protected override void Generate(AlterTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
        {
            base.Generate(operation, model, builder);
            if (operation.OldTable.Comment != operation.Comment && operation.Comment != null)
            {
                Comment(builder, operation.Comment, operation.Schema, operation.Name);
                builder.EndCommand(false);
            }
        }

        protected virtual void Comment(MigrationCommandListBuilder builder, string description, string? schema, string table, string? column = null)
        {
            if (column != null)
            {
                builder.Append("comment on column ");
            }
            else
            {
                builder.Append("comment on table ");
            }
            builder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(table, schema));
            if (column != null)
            {
                builder.Append("." + Dependencies.SqlGenerationHelper.DelimitIdentifier(column));
            }
            builder.Append(" is ");
            builder.Append("'" + description.Replace("'", "''") + "'");
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand(false);
        }
    }
}
