using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Dm;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Dm.Scaffolding.Internal
{
    public class DmDatabaseModelFactory : IDatabaseModelFactory
    {
        private readonly IDiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;

        private const string NamePartRegex = "(?:(?:\\[(?<part{0}>(?:(?:\\]\\])|[^\\]])+)\\])|(?<part{0}>[^\\.\\[\\]]+))";

        private static readonly Regex _partExtractor = new Regex(string.Format(CultureInfo.InvariantCulture, "^{0}(?:\\.{1})?$", string.Format(CultureInfo.InvariantCulture, "(?:(?:\\[(?<part{0}>(?:(?:\\]\\])|[^\\]])+)\\])|(?<part{0}>[^\\.\\[\\]]+))", 1), string.Format(CultureInfo.InvariantCulture, "(?:(?:\\[(?<part{0}>(?:(?:\\]\\])|[^\\]])+)\\])|(?<part{0}>[^\\.\\[\\]]+))", 2)), RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000.0));

        private static readonly ISet<string> _dateTimePrecisionTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "timestamp", "datetime", "time" };

        private static readonly ISet<string> _withoutScaleAndPrecTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bit", "byte", "tinyint", "smallint", "int", "integer", "bigint", "date", "rowid", "float",
            "real", "double", "double precision", "text", "long", "longvarchar", "image", "longvarbinary", "blob", "clob",
            "bfile"
        };

        private static readonly ISet<string> _decTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dec", "numeric", "decimal", "number" };

        private static readonly Dictionary<string, long[]> _defaultSequenceMinMax = new Dictionary<string, long[]>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "tinyint",
                new long[2] { 0L, 255L }
            },
            {
                "smallint",
                new long[2] { -32768L, 32767L }
            },
            {
                "int",
                new long[2] { -2147483648L, 2147483647L }
            },
            {
                "bigint",
                new long[2] { -9223372036854775808L, 9223372036854775807L }
            }
        };

        public DmDatabaseModelFactory(IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger)
        {
            Check.NotNull(logger, "logger");
            _logger = logger;
        }

        private IEnumerable<DatabaseSequence> GetSequences(DbConnection connection, Func<string, string> schemaFilter)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "\nSELECT\n        SCHEMAS.NAME                                             AS SCH_NAME       ,\n        SEQS.NAME                                                AS SEQ_NAME       ,\n        SEQS.INFO3                                               AS SEQ_START_VALUE,\n        DBA_SEQUENCES.MIN_VALUE                                  AS SEQ_MIN_VALUE  ,\n        DBA_SEQUENCES.MAX_VALUE                                  AS SEQ_MAX_VALUE  ,\n        DBA_SEQUENCES.INCREMENT_BY                               AS SEQ_INCREMENT  ,\n        CASE DBA_SEQUENCES.CYCLE_FLAG WHEN 'Y' THEN 1 ELSE 0 END AS SEQ_CYCLE\nFROM\n        (\n                SELECT NAME, ID FROM SYSOBJECTS WHERE PID = UID() AND TYPE$='SCH'\n        )\n        SCHEMAS,\n        (\n                SELECT * FROM SYSOBJECTS WHERE SUBTYPE$ ='SEQ'\n        )\n        SEQS,\n        DBA_SEQUENCES\nWHERE\n        SEQS.SCHID   = SCHEMAS.ID \n    AND SEQS.NAME    = DBA_SEQUENCES.SEQUENCE_NAME\n    AND SCHEMAS.NAME = DBA_SEQUENCES.SEQUENCE_OWNER";
            if (schemaFilter != null)
            {
                command.CommandText = command.CommandText + "\nAND " + schemaFilter("SCHEMAS.NAME");
            }
            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                DatabaseSequence sequence = new DatabaseSequence();
                sequence.Schema = reader.GetValueOrDefault<string>("SCH_NAME");
                sequence.Name = reader.GetValueOrDefault<string>("SEQ_NAME");
                sequence.StoreType = "BIGINT";
                sequence.IsCyclic = (bool?)(reader.GetValueOrDefault<int>("SEQ_CYCLE") > 0);
                sequence.IncrementBy = (int?)(int)reader.GetValueOrDefault<long>("SEQ_INCREMENT");
                sequence.StartValue = (long?)reader.GetValueOrDefault<long>("SEQ_START_VALUE");
                sequence.MinValue = (long?)reader.GetValueOrDefault<long>("SEQ_MIN_VALUE");
                sequence.MaxValue = (long?)reader.GetValueOrDefault<long>("SEQ_MAX_VALUE");
                _logger.SequenceFound(DisplayName(sequence.Schema, sequence.Name), sequence.StoreType, sequence.IsCyclic, sequence.IncrementBy, sequence.StartValue, sequence.MinValue, sequence.MaxValue);
                if (_defaultSequenceMinMax.ContainsKey(sequence.StoreType))
                {
                    long minDefault = _defaultSequenceMinMax[sequence.StoreType][0];
                    sequence.MinValue = (sequence.MinValue == minDefault) ? null : sequence.MinValue;
                    sequence.StartValue = (sequence.StartValue == minDefault) ? null : sequence.StartValue;
                    long maxDefault = _defaultSequenceMinMax[sequence.StoreType][1];
                    sequence.MaxValue = (sequence.MaxValue == maxDefault) ? null : sequence.MaxValue;
                }
                yield return sequence;
            }
        }

        private void GetTables(DbConnection connection, Func<string, string, string> tableFilter, DatabaseModel databaseModel)
        {
            using DbCommand dbCommand = connection.CreateCommand();
            string filterClause = "";
            dbCommand.CommandText = "\nSELECT SCH.NAME AS SCH_NAME, TAB.NAME AS TAB_NAME, TAB.SUBTYPE$ AS TAB_TYPE FROM\n(SELECT NAME,ID FROM SYSOBJECTS WHERE TYPE$ = 'SCH' AND PID = UID())SCH,\nSYSOBJECTS TAB\nWHERE TAB.SCHID = SCH.ID AND TAB.TYPE$ = 'SCHOBJ' AND TAB.SUBTYPE$ IN ('UTAB', 'VIEW') AND TAB.NAME <> '##HISTOGRAMS_TABLE' AND TAB.NAME <> '##PLAN_TABLE'AND  TAB.NAME <> '__EFMigrationsHistory' AND TAB.INFO3 & 0XFF <> 64";
            if (tableFilter != null)
            {
                filterClause = " AND " + tableFilter("SCH.NAME", "TAB.NAME");
            }
            dbCommand.CommandText += filterClause;
            using (DbDataReader dbDataReader = dbCommand.ExecuteReader())
            {
                while (dbDataReader.Read())
                {
                    string schemaName = dbDataReader.GetValueOrDefault<string>("SCH_NAME");
                    string tableName = dbDataReader.GetValueOrDefault<string>("TAB_NAME");
                    string tableType = dbDataReader.GetValueOrDefault<string>("TAB_TYPE");
                    DatabaseTable table;
                    if (tableType.Equals("UTAB"))
                    {
                        table = new DatabaseTable
                        {
                            Schema = schemaName,
                            Name = tableName
                        };
                    }
                    else
                    {
                        if (!tableType.Equals("VIEW"))
                        {
                            throw new Exception("Unknown table type: " + tableType);
                        }
                        DatabaseView view = new DatabaseView();
                        view.Schema = schemaName;
                        view.Name = tableName;
                        table = view;
                    }
                    _logger.TableFound(DisplayName(table.Schema, table.Name));
                    databaseModel.Tables.Add(table);
                }
            }
            GetColumns(connection, filterClause, databaseModel);
            GetKeys(connection, filterClause, databaseModel);
            GetIndexes(connection, filterClause, databaseModel);
            GetForeignKeys(connection, filterClause, databaseModel);
        }

        private void GetColumns(DbConnection connection, string tableFilter, DatabaseModel databaseModel)
        {
            DmCommand dmCommand = (DmCommand)connection.CreateCommand();
            try
            {
                dmCommand.CommandText = "SELECT\n        /*+ MAX_OPT_N_TABLES(5) */\n        SCH.NAME AS SCH_NAME   ,\n        TAB.NAME AS TAB_NAME   ,\n        COL.NAME AS COL_NAME   ,\n        COL.TYPE$                                     AS TYPE_NAME,\n        COL.LENGTH$                                   AS COL_LENGTH,\n        COL.SCALE AS COL_SCALE  ,\n        COL.DEFVAL AS COL_DEF    ,\n        COL.COLID + 1                                 AS COL_ORDINAL,\n        CASE COL.NULLABLE$ WHEN 'Y' THEN 1 ELSE 0 END AS IS_NULLABLE,\n        COL.INFO2 & 0X01                              AS IS_IDENTITY,\n        TAB.SUBTYPE$                                  AS TAB_TYPE,\n        (\n                SELECT\n                        SF_GET_INDEX_KEY_SEQ(IND.KEYNUM, IND.KEYINFO, COL.COLID)\n                FROM\n                        SYS.SYSINDEXES IND,\n                        (\n                                SELECT\n                                        OBJ.NAME,\n                                        CON.ID,\n                                        CON.TYPE$  ,\n                                        CON.TABLEID,\n                                        CON.COLID,\n                                        CON.INDEXID\n                                FROM\n                                        SYS.SYSCONS    AS CON,\n                                        SYS.SYSOBJECTS AS OBJ\n                                WHERE\n                                        CON.TYPE$   = 'P'\n                                    AND OBJ.SUBTYPE$= 'CONS'\n                                    AND OBJ.ID = CON.ID\n                        )\n                        CON,\n                        (\n                                SELECT ID, NAME FROM SYS.SYSOBJECTS WHERE SUBTYPE$= 'INDEX'\n                        )\n                        OBJ_IND\n                WHERE\n                        CON.INDEXID = IND.ID\n                    AND IND.ID = OBJ_IND.ID\n                    AND CON.TABLEID = TAB.ID\n                    AND SF_COL_IS_IDX_KEY(IND.KEYNUM, IND.KEYINFO, COL.COLID)= 1\n        ) AS PK_ORDINAL\nFROM\n        SYS.SYSCOLUMNS COL,\n        (\n                SELECT\n                        ID,\n                        PID,\n                        NAME\n                FROM\n                        SYS.SYSOBJECTS\n                WHERE\n                        TYPE$ = 'SCH'\n                    AND PID = UID()\n        )\n        SCH,\n        (\n                SELECT\n                        ID,\n                        SCHID,\n                        NAME,\n                        SUBTYPE$\n                FROM\n                        SYS.SYSOBJECTS\n                WHERE\n                        TYPE$     = 'SCHOBJ'\n                    AND SUBTYPE$ IN ('UTAB', 'VIEW')\n                    AND NAME<> '##HISTOGRAMS_TABLE'\n                    AND NAME<> '##PLAN_TABLE'\n                    AND INFO3 & 0XFF <> 64\n                    AND NAME<> '__EFMigrationsHistory'\n        )\n        TAB\nWHERE\n        SCH.ID = TAB.SCHID\n    AND TAB.ID = COL.ID " + tableFilter + "\nORDER BY\n        COL_NAME ASC";
                using DbDataReader source = dmCommand.ExecuteReader();
                foreach (IGrouping<(string, string), DbDataRecord> item2 in from DbDataRecord ddr in source
                                                                            group ddr by (ddr.GetValueOrDefault<string>("SCH_NAME"), ddr.GetValueOrDefault<string>("TAB_NAME")))
                {
                    string tableSchema = item2.Key.Item1;
                    string tableName = item2.Key.Item2;
                    DatabaseTable table = databaseModel.Tables.Single(t => t.Schema == tableSchema && t.Name == tableName);
                    foreach (DbDataRecord record in item2)
                    {
                        string columnName = record.GetValueOrDefault<string>("COL_NAME");
                        record.GetValueOrDefault<int>("COL_ORDINAL");
                        string typeName = record.GetValueOrDefault<string>("TYPE_NAME");
                        int colLength = record.GetValueOrDefault<int>("COL_LENGTH");
                        int precision = colLength;
                        short colScale = record.GetValueOrDefault<short>("COL_SCALE");
                        bool isNullable = record.GetValueOrDefault<int>("IS_NULLABLE") > 0;
                        string tabType = record.GetValueOrDefault<string>("TAB_TYPE");
                        bool isIdentity = record.GetValueOrDefault<short>("IS_IDENTITY") > 0;
                        if (tabType.Equals("VIEW"))
                        {
                            isIdentity = false;
                        }
                        string defaultValueSql = (!isIdentity) ? record.GetValueOrDefault<string>("COL_DEF") : null;
                        string computedColumnSql = defaultValueSql;
                        string dmClrType = GetDmClrType(typeName, colLength, precision, colScale);
                        if (string.IsNullOrWhiteSpace(defaultValueSql) || !string.IsNullOrWhiteSpace(computedColumnSql))
                        {
                            defaultValueSql = null;
                        }
                        DatabaseColumn column = new DatabaseColumn();
                        column.Table = table;
                        column.Name = columnName;
                        column.StoreType = dmClrType;
                        column.IsNullable = isNullable;
                        column.DefaultValueSql = defaultValueSql;
                        column.ComputedColumnSql = computedColumnSql;
                        column.ValueGenerated = isIdentity ? new ValueGenerated?((ValueGenerated)1) : null;
                        table.Columns.Add(column);
                    }
                }
            }
            finally
            {
                dmCommand?.Dispose();
            }
        }

        private void GetKeys(DbConnection connection, string tableFilter, DatabaseModel databaseModel)
        {
            using DbCommand dbCommand = connection.CreateCommand();
            dbCommand.CommandText = "SELECT\n        /*+ MAX_OPT_N_TABLES(5) */\n        SCH.NAME                                                AS SCH_NAME,\n        TAB.NAME                                                    AS TAB_NAME ,\n        COL.NAME                                                   AS COL_NAME,\n        SF_GET_INDEX_KEY_SEQ(IND.KEYNUM, IND.KEYINFO, COL.COLID) AS PK_ORDINAL    ,\n        CON.NAME                                                   AS CONSTRAINT_NAME,\n        IND.XTYPE & 0X01\t\t\t\t\t\t\t\t\t\t    AS IS_CLUSTER,\n        CON.TYPE$                                                  AS CONSTRAINT_TYPE\nFROM\n        SYS.SYSINDEXES IND,\n        (\n                SELECT\n                        OBJ.NAME   ,\n                        CON.ID     ,\n                        CON.TYPE$  ,\n                        CON.TABLEID,\n                        CON.COLID  ,\n                        CON.INDEXID\n                FROM\n                        SYS.SYSCONS    AS CON,\n                        SYS.SYSOBJECTS AS OBJ\n                WHERE\n                        CON.TYPE$   IN ('P', 'U')\n                    AND OBJ.SUBTYPE$='CONS'\n                    AND OBJ.ID      =CON.ID\n        )\n        CON               ,\n        SYS.SYSCOLUMNS COL,\n        (\n                SELECT\n                        ID ,\n                        PID,\n                        NAME\n                FROM\n                        SYS.SYSOBJECTS\n                WHERE\n                        TYPE$ = 'SCH' AND PID =UID()\n        )\n        SCH,\n        (\n                SELECT\n                        ID   ,\n                        SCHID,\n                        NAME\n                FROM\n                        SYS.SYSOBJECTS\n                WHERE\n                        TYPE$    = 'SCHOBJ'\n                    AND SUBTYPE$ IN ('UTAB', 'VIEW')\n                    AND NAME <> '##HISTOGRAMS_TABLE' \n                    AND NAME <> '##PLAN_TABLE'\n                    AND INFO3 & 0XFF <> 64\n                    AND NAME <> '__EFMigrationsHistory'\n        )\n        TAB,\n        (\n                SELECT ID, NAME FROM SYS.SYSOBJECTS WHERE SUBTYPE$='INDEX'\n        )\n        OBJ_IND\nWHERE\n        SCH.ID                                              = TAB.SCHID\n    AND CON.INDEXID                                            =IND.ID\n    AND IND.ID                                                 =OBJ_IND.ID\n    AND TAB.ID                                                  =COL.ID\n    AND CON.TABLEID                                            =TAB.ID\n    AND SF_COL_IS_IDX_KEY(IND.KEYNUM, IND.KEYINFO, COL.COLID)=1 " + tableFilter + " ORDER BY SCH_NAME ASC, TAB_NAME ASC, CONSTRAINT_NAME ASC";
            using DbDataReader source = dbCommand.ExecuteReader();
            foreach (IGrouping<(string, string), DbDataRecord> item3 in from DbDataRecord ddr in source
                                                                        group ddr by (ddr.GetValueOrDefault<string>("SCH_NAME"), ddr.GetValueOrDefault<string>("TAB_NAME")))
            {
                string tableSchema = item3.Key.Item1;
                string tableName = item3.Key.Item2;
                DatabaseTable table = databaseModel.Tables.Single(t => t.Schema == tableSchema && t.Name == tableName);
                IGrouping<string, DbDataRecord>[] primaryKeyGroups = (from ddr in item3
                                                                      where ddr.GetValueOrDefault<string>("CONSTRAINT_TYPE").Equals("P")
                                                                      group ddr by ddr.GetValueOrDefault<string>("CONSTRAINT_NAME")).ToArray();
                if (primaryKeyGroups.Length == 1)
                {
                    IGrouping<string, DbDataRecord> pkGroup = primaryKeyGroups[0];
                    _logger.PrimaryKeyFound(pkGroup.Key, DisplayName(tableSchema, tableName));
                    DatabasePrimaryKey primaryKey = new DatabasePrimaryKey();
                    primaryKey.Table = table;
                    primaryKey.Name = pkGroup.Key;
                    foreach (DbDataRecord record in pkGroup)
                    {
                        string columnName2 = record.GetValueOrDefault<string>("COL_NAME");
                        DatabaseColumn item = table.Columns.FirstOrDefault(c => c.Name == columnName2) ?? table.Columns.FirstOrDefault(c => c.Name.Equals(columnName2, StringComparison.OrdinalIgnoreCase));
                        primaryKey.Columns.Add(item);
                    }
                    table.PrimaryKey = primaryKey;
                }
                IGrouping<string, DbDataRecord>[] uniqueGroups = (from ddr in item3
                                                                  where ddr.GetValueOrDefault<string>("CONSTRAINT_TYPE").Equals("U")
                                                                  group ddr by ddr.GetValueOrDefault<string>("CONSTRAINT_NAME")).ToArray();
                foreach (IGrouping<string, DbDataRecord> ucGroup in uniqueGroups)
                {
                    _logger.UniqueConstraintFound(ucGroup.Key, DisplayName(tableSchema, tableName));
                    DatabaseUniqueConstraint uniqueConstraint = new DatabaseUniqueConstraint();
                    uniqueConstraint.Table = table;
                    uniqueConstraint.Name = ucGroup.Key;
                    foreach (DbDataRecord record in ucGroup)
                    {
                        string columnName = record.GetValueOrDefault<string>("COL_NAME");
                        DatabaseColumn item2 = table.Columns.FirstOrDefault(c => c.Name == columnName) ?? table.Columns.FirstOrDefault(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                        uniqueConstraint.Columns.Add(item2);
                    }
                    table.UniqueConstraints.Add(uniqueConstraint);
                }
            }
        }

        private void GetForeignKeys(DbConnection connection, string tableFilter, DatabaseModel databaseModel)
        {
            using DbCommand dbCommand = connection.CreateCommand();
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8439, 2);
            defaultInterpolatedStringHandler.AppendLiteral("SELECT\n        /*+ MAX_OPT_N_TABLES(5) */\n        T_REFED.SCHNAME          AS PKTABLE_SCHEM,\n        T_REFED.NAME             AS PKTABLE_NAME ,\n        T_REFED.REFED_COL_NAME   AS PKCOLUMN_NAME,\n        T_REF.SCHNAME            AS FKTABLE_SCHEM,\n        T_REF.NAME               AS FKTABLE_NAME ,\n        T_REF.REF_COL_NAME       AS FKCOLUMN_NAME,\n        T_REF.REF_KEYNO          AS KEY_SEQ      ,\n        SF_GET_UPD_RULE(FACTION) AS UPDATE_RULE  ,\n        SF_GET_DEL_RULE(FACTION) AS DELETE_RULE  ,\n        T_REF.REF_CON_NAME      AS FK_NAME\nFROM\n        (\n                SELECT\n                        T_REF_TAB.NAME                                                               AS NAME         ,\n                        T_REF_TAB.SCHNAME                                                            AS SCHNAME      ,\n                        T_REF_CON.FINDEXID                                                          AS REFED_IND_ID ,\n                        T_REF_CON.NAME                                                              AS REF_CON_NAME,\n                        SF_GET_INDEX_KEY_SEQ(T_REF_IND.KEYNUM, T_REF_IND.KEYINFO, T_REF_COL.COLID) AS REF_KEYNO    ,\n                        T_REF_COL.NAME                                                               AS REF_COL_NAME ,\n                        T_REF_CON.FACTION                                                           AS FACTION\n                FROM\n                        (\n                                SELECT\n                                        OBJ.NAME    ,\n                                        CON.TABLEID ,\n                                        CON.INDEXID ,\n                                        CON.FINDEXID,\n                                        CON.FACTION\n                                FROM\n                                        (\n                                                SELECT NAME, ID FROM SYS.SYSOBJECTS WHERE SUBTYPE$='CONS'\n                                        )\n                                        OBJ,\n                                        SYS.SYSCONS CON\n                                WHERE\n                                        CON.ID    = OBJ.ID\n                                    AND CON.TYPE$ = 'F'\n                        )AS T_REF_CON,\n                        (\n                                SELECT\n                                        TAB.NAME    AS NAME,\n                                        TAB.ID      AS ID  ,\n                                        SCH.NAME AS SCHNAME\n                                FROM\n                                        (\n                                                SELECT ID, NAME FROM SYS.SYSOBJECTS WHERE TYPE$ = 'SCH' AND PID = UID()\n                                        )\n                                        SCH,\n                                        (\n                                                SELECT\n                                                        ID   ,\n                                                        SCHID,\n                                                        NAME\n                                                FROM\n                                                        SYS.SYSOBJECTS\n                                                WHERE\n                                                        TYPE$    = 'SCHOBJ'\n                                                    AND SUBTYPE$ = 'UTAB'\n                                                    AND INFO3 & 0XFF <> 64\n                                        )\n                                        TAB\n                                WHERE\n                                        SCH.ID = TAB.SCHID ");
            defaultInterpolatedStringHandler.AppendFormatted(tableFilter);
            defaultInterpolatedStringHandler.AppendLiteral("\n                        )              AS T_REF_TAB ,\n                        SYS.SYSINDEXES AS T_REF_IND,\n                        (\n                                SELECT ID  , PID, NAME FROM SYS.SYSOBJECTS WHERE SUBTYPE$='INDEX'\n                        )              AS T_REF_IND_OBJ,\n                        SYS.SYSCOLUMNS AS T_REF_COL\n                WHERE\n                        T_REF_TAB.ID                                                             = T_REF_CON.TABLEID\n                    AND T_REF_TAB.ID                                                             = T_REF_IND_OBJ.PID\n                    AND T_REF_TAB.ID                                                             = T_REF_COL.ID\n                    AND T_REF_CON.INDEXID                                                       = T_REF_IND_OBJ.ID\n                    AND T_REF_IND.ID                                                            = T_REF_IND_OBJ.ID\n                    AND SF_COL_IS_IDX_KEY(T_REF_IND.KEYNUM, T_REF_IND.KEYINFO, T_REF_COL.COLID)=1\n        ) AS T_REF,\n        (\n                SELECT\n                        T_REFED_TAB.NAME                                                                   AS NAME           ,\n                        T_REFED_TAB.SCHNAME                                                                     AS SCHNAME        ,\n                        T_REFED_IND.ID                                                                    AS REFED_IND_ID   ,\n                        T_REFED_IND_OBJ.NAME                                                              AS REFED_CON_NAME,\n                        SF_GET_INDEX_KEY_SEQ(T_REFED_IND.KEYNUM, T_REFED_IND.KEYINFO, T_REFED_COL.COLID) AS REFED_KEYNO    ,\n                        T_REFED_COL.NAME                                                                   AS REFED_COL_NAME\n                FROM\n                        SYS.SYSCONS AS T_REFED_CON,\n                        (\n                                SELECT\n                                        TAB.NAME    AS NAME,\n                                        TAB.ID      AS ID  ,\n                                        SCH.NAME AS SCHNAME\n                                FROM\n                                        (\n                                                SELECT ID, NAME FROM SYS.SYSOBJECTS WHERE TYPE$ = 'SCH' AND PID = UID()\n                                        )\n                                        SCH,\n                                        (\n                                                SELECT\n                                                        ID   ,\n                                                        SCHID,\n                                                        NAME\n                                                FROM\n                                                        SYS.SYSOBJECTS\n                                                WHERE\n                                                        TYPE$    = 'SCHOBJ'\n                                                    AND SUBTYPE$ = 'UTAB'\n                                                    AND INFO3 & 0XFF <> 64 \n                                        )\n                                        TAB\n                                WHERE\n                                        SCH.ID = TAB.SCHID ");
            defaultInterpolatedStringHandler.AppendFormatted(tableFilter);
            defaultInterpolatedStringHandler.AppendLiteral("\n                        )              AS T_REFED_TAB ,\n                        SYS.SYSINDEXES AS T_REFED_IND,\n                        (\n                                SELECT ID, PID, NAME FROM SYS.SYSOBJECTS WHERE SUBTYPE$='INDEX'\n                        )              AS T_REFED_IND_OBJ,\n                        SYS.SYSCOLUMNS AS T_REFED_COL\n                WHERE\n                        T_REFED_TAB.ID                                                                 = T_REFED_CON.TABLEID\n                    AND T_REFED_CON.TYPE$                                                             IN('P','U')\n                    AND T_REFED_TAB.ID                                                                 = T_REFED_IND_OBJ.PID\n                    AND T_REFED_TAB.ID                                                                 = T_REFED_COL.ID\n                    AND T_REFED_CON.INDEXID                                                           = T_REFED_IND_OBJ.ID\n                    AND T_REFED_IND.ID                                                                = T_REFED_IND_OBJ.ID\n                    AND SF_COL_IS_IDX_KEY(T_REFED_IND.KEYNUM, T_REFED_IND.KEYINFO, T_REFED_COL.COLID)=1\n        ) AS T_REFED\nWHERE\n        T_REF.REFED_IND_ID = T_REFED.REFED_IND_ID\n    AND T_REF.REF_KEYNO    = T_REFED.REFED_KEYNO \nORDER BY\n        FKTABLE_SCHEM ASC,\n        FKTABLE_NAME ASC ,\n        FK_NAME ASC,\n        KEY_SEQ ASC");
            dbCommand.CommandText = defaultInterpolatedStringHandler.ToStringAndClear();
            using DbDataReader source = dbCommand.ExecuteReader();
            foreach (IGrouping<(string, string), DbDataRecord> item4 in from DbDataRecord ddr in source
                                                                        group ddr by (ddr.GetValueOrDefault<string>("FKTABLE_SCHEM"), ddr.GetValueOrDefault<string>("FKTABLE_NAME")))
            {
                string tableSchema = item4.Key.Item1;
                string tableName = item4.Key.Item2;
                DatabaseTable table = databaseModel.Tables.Single(t => t.Schema == tableSchema && t.Name == tableName);
                foreach (IGrouping<(string, string, string, string), DbDataRecord> fkGroup in from c in item4
                                                                                              group c by (c.GetValueOrDefault<string>("FK_NAME"), c.GetValueOrDefault<string>("PKTABLE_SCHEM"), c.GetValueOrDefault<string>("PKTABLE_NAME"), ConvertToStringReferentialAction(c.GetValueOrDefault<int>("DELETE_RULE"))))
                {
                    string fkName = fkGroup.Key.Item1;
                    string principalTableSchema = fkGroup.Key.Item2;
                    string principalTableName = fkGroup.Key.Item3;
                    string onDeleteAction = fkGroup.Key.Item4;
                    _logger.ForeignKeyFound(fkName, DisplayName(table.Schema, table.Name), DisplayName(principalTableSchema, principalTableName), onDeleteAction);
                    DatabaseTable principalTable = databaseModel.Tables.FirstOrDefault((t => t.Schema == principalTableSchema && t.Name == principalTableName))
                        ?? databaseModel.Tables.FirstOrDefault((t => t.Schema.Equals(principalTableSchema, StringComparison.OrdinalIgnoreCase) && t.Name.Equals(principalTableName, StringComparison.OrdinalIgnoreCase)));
                    if (principalTable == null)
                    {
                        _logger.ForeignKeyReferencesMissingPrincipalTableWarning(fkName, DisplayName(table.Schema, table.Name), DisplayName(principalTableSchema, principalTableName));
                        continue;
                    }
                    DatabaseForeignKey foreignKey = new DatabaseForeignKey();
                    foreignKey.Name = fkName;
                    foreignKey.Table = table;
                    foreignKey.PrincipalTable = principalTable;
                    foreignKey.OnDelete = ConvertToReferentialAction(onDeleteAction);
                    bool hasMissingColumn = false;
                    foreach (DbDataRecord record in fkGroup)
                    {
                        string columnName = record.GetValueOrDefault<string>("FKCOLUMN_NAME");
                        DatabaseColumn fkColumn = table.Columns.FirstOrDefault((c => c.Name == columnName)) ?? table.Columns.FirstOrDefault((c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase)));
                        string principalColumnName = record.GetValueOrDefault<string>("PKCOLUMN_NAME");
                        DatabaseColumn principalColumn = (foreignKey.PrincipalTable.Columns)
                            .FirstOrDefault(c => c.Name == principalColumnName) ??
                            (foreignKey.PrincipalTable.Columns).FirstOrDefault((c => c.Name.Equals(principalColumnName, StringComparison.OrdinalIgnoreCase)));
                        if (principalColumn == null)
                        {
                            hasMissingColumn = true;
                            _logger.ForeignKeyPrincipalColumnMissingWarning(fkName, DisplayName(table.Schema, table.Name), principalColumnName, DisplayName(principalTableSchema, principalTableName));
                            break;
                        }
                        foreignKey.Columns.Add(fkColumn);
                        foreignKey.PrincipalColumns.Add(principalColumn);
                    }
                    if (!hasMissingColumn)
                    {
                        table.ForeignKeys.Add(foreignKey);
                    }
                }
            }
        }

        private void GetIndexes(DbConnection connection, string tableFilter, DatabaseModel databaseModel)
        {
            using DbCommand dbCommand = connection.CreateCommand();
            dbCommand.CommandText = "SELECT\n        /*+ MAX_OPT_N_TABLES(5) */\n        DISTINCT \n        SCH.NAME                                                    AS SCH_NAME     ,\n        TAB.NAME                                                      AS TAB_NAME      ,\n        CASE IND.ISUNIQUE WHEN 'Y' THEN 1 ELSE 0 END                 AS IS_UNIQUE      ,\n        OBJ_IND.NAME                                                 AS IND_NAME      ,\n        IND.XTYPE & 0x01                \t\t\t\t\t\t  \t  AS IS_CLUSTER          ,\n        SF_GET_INDEX_KEY_SEQ(IND.KEYNUM, IND.KEYINFO, COL.COLID)   AS IND_ORDINAL,\n        COL.NAME                                                     AS COL_NAME     \nFROM\n        (\n                SELECT\n                        ID ,\n                        PID,\n                        NAME\n                FROM\n                        SYS.SYSOBJECTS\n                WHERE\n                        TYPE$ = 'SCH'\n                        AND PID =UID()\n        )\n        SCH,\n        (\n                SELECT\n                        ID   ,\n                        SCHID,\n                        NAME\n                FROM\n                        SYS.SYSOBJECTS\n                WHERE\n                        TYPE$    = 'SCHOBJ'\n                    AND SUBTYPE$ IN ('UTAB', 'VIEW')\n                    AND NAME <> '##HISTOGRAMS_TABLE' \n                    AND NAME <> '##PLAN_TABLE'\n                    AND INFO3 & 0XFF <> 64\n                    AND NAME <> '__EFMigrationsHistory'\n        )\n        TAB,\n        (\n                SELECT ID, PID, NAME FROM SYS.SYSOBJECTS WHERE SUBTYPE$='INDEX'\n        )              AS OBJ_IND    ,\n        SYS.SYSINDEXES AS IND        ,\n        SYS.SYSCOLUMNS AS COL\nWHERE\n        TAB.ID                                                  =COL.ID\n    AND TAB.ID                                                  =OBJ_IND.PID\n    AND IND.ID                                                 =OBJ_IND.ID\n    AND TAB.SCHID                                               = SCH.ID\n    AND IND.FLAG & 0X01 = 0\n    AND SF_COL_IS_IDX_KEY(IND.KEYNUM, IND.KEYINFO, COL.COLID)=1 " + tableFilter + "\nORDER BY\n        SCH_NAME ASC,\n        TAB_NAME ASC      ,\n        IND_NAME ASC,\n        IND_ORDINAL ASC;";
            using DbDataReader source = dbCommand.ExecuteReader();
            foreach (IGrouping<(string, string), DbDataRecord> item2 in from DbDataRecord ddr in source
                                                                        group ddr by (ddr.GetValueOrDefault<string>("SCH_NAME"), ddr.GetValueOrDefault<string>("TAB_NAME")))
            {
                string tableSchema = item2.Key.Item1;
                string tableName = item2.Key.Item2;
                DatabaseTable table = databaseModel.Tables.Single(t => t.Schema == tableSchema && t.Name == tableName);
                IGrouping<(string, bool), DbDataRecord>[] indexGroups = (from ddr in item2
                                                                         group ddr by (ddr.GetValueOrDefault<string>("IND_NAME"), ddr.GetValueOrDefault<int>("IS_UNIQUE") > 0)).ToArray();
                foreach (IGrouping<(string, bool), DbDataRecord> indexGroup in indexGroups)
                {
                    _logger.IndexFound(indexGroup.Key.Item1, DisplayName(tableSchema, tableName), indexGroup.Key.Item2);
                    DatabaseIndex index = new DatabaseIndex();
                    index.Table = table;
                    index.Name = indexGroup.Key.Item1;
                    index.IsUnique = indexGroup.Key.Item2;
                    foreach (DbDataRecord record in indexGroup)
                    {
                        string columnName = record.GetValueOrDefault<string>("COL_NAME");
                        DatabaseColumn item = (table.Columns).FirstOrDefault((c => c.Name == columnName)) ??
                            (table.Columns).FirstOrDefault(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                        index.Columns.Add(item);
                    }
                    table.Indexes.Add(index);
                }
            }
        }

        private static string DisplayName(string schema, string name)
        {
            return ((!string.IsNullOrEmpty(schema)) ? (schema + ".") : "") + name;
        }

        private string GetDefaultSchema(DbConnection connection)
        {
            using DbCommand dbCommand = connection.CreateCommand();
            dbCommand.CommandText = "SELECT SF_GET_SCHEMA_NAME_BY_ID(CURRENT_SCHID())";
            if (dbCommand.ExecuteScalar() is string schemaName)
            {
                _logger.DefaultSchemaFound(schemaName);
                return schemaName;
            }
            return null;
        }

        private static string GetDmClrType(string dataTypeName, int maxLength, int precision, int scale)
        {
            if (_decTypes.Contains(dataTypeName))
            {
                if (precision == 0)
                {
                    return dataTypeName;
                }
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 3);
                defaultInterpolatedStringHandler.AppendFormatted(dataTypeName);
                defaultInterpolatedStringHandler.AppendLiteral("(");
                defaultInterpolatedStringHandler.AppendFormatted(precision);
                defaultInterpolatedStringHandler.AppendLiteral(", ");
                defaultInterpolatedStringHandler.AppendFormatted(scale);
                defaultInterpolatedStringHandler.AppendLiteral(")");
                return defaultInterpolatedStringHandler.ToStringAndClear();
            }
            if (_dateTimePrecisionTypes.Contains(dataTypeName))
            {
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
                defaultInterpolatedStringHandler.AppendFormatted(dataTypeName);
                defaultInterpolatedStringHandler.AppendLiteral("(");
                defaultInterpolatedStringHandler.AppendFormatted(scale);
                defaultInterpolatedStringHandler.AppendLiteral(")");
                return defaultInterpolatedStringHandler.ToStringAndClear();
            }
            if (dataTypeName.StartsWith("interval", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessInterval(dataTypeName, scale);
            }
            if (dataTypeName.EndsWith("time zone", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessTimeZone(dataTypeName, scale);
            }
            if (!_withoutScaleAndPrecTypes.Contains(dataTypeName))
            {
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
                defaultInterpolatedStringHandler.AppendFormatted(dataTypeName);
                defaultInterpolatedStringHandler.AppendLiteral("(");
                defaultInterpolatedStringHandler.AppendFormatted(maxLength);
                defaultInterpolatedStringHandler.AppendLiteral(")");
                return defaultInterpolatedStringHandler.ToStringAndClear();
            }
            return dataTypeName;
        }

        private static string ProcessTimeZone(string dataTypeName, int scale)
        {
            if (dataTypeName.Contains("local", StringComparison.OrdinalIgnoreCase))
            {
                scale &= 0xF;
            }
            string[] array = dataTypeName.Split(' ');
            string text = null;
            for (int i = 0; i < array.Length; i++)
            {
                text += array[i];
                if (i == 0)
                {
                    string prefix = text;
                    DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
                    defaultInterpolatedStringHandler.AppendLiteral("(");
                    defaultInterpolatedStringHandler.AppendFormatted(scale);
                    defaultInterpolatedStringHandler.AppendLiteral(")");
                    text = prefix + defaultInterpolatedStringHandler.ToStringAndClear();
                }
                if (i != array.Length - 1)
                {
                    text += " ";
                }
            }
            return text;
        }

        private static string ProcessInterval(string dataTypeName, int scale)
        {
            int value = scale & 0xF;
            int value2 = (scale >> 4) & 0xF;
            string[] array = dataTypeName.Split(' ');
            string text;
            switch (array[1].ToLower())
            {
                case "year":
                case "month":
                    {
                        string part0 = array[0];
                        string part1 = array[1];
                        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
                        defaultInterpolatedStringHandler.AppendLiteral("(");
                        defaultInterpolatedStringHandler.AppendFormatted(value2);
                        defaultInterpolatedStringHandler.AppendLiteral(")");
                        text = part0 + " " + part1 + defaultInterpolatedStringHandler.ToStringAndClear();
                        if (array.Length == 4)
                        {
                            text = text + " " + array[2] + " " + array[3];
                        }
                        break;
                    }
                case "day":
                case "hour":
                case "minute":
                    {
                        string part0 = array[0];
                        string part1 = array[1];
                        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
                        defaultInterpolatedStringHandler.AppendLiteral("(");
                        defaultInterpolatedStringHandler.AppendFormatted(value2);
                        defaultInterpolatedStringHandler.AppendLiteral(")");
                        text = part0 + " " + part1 + defaultInterpolatedStringHandler.ToStringAndClear();
                        if (array.Length == 4)
                        {
                            text = text + " " + array[2] + " " + array[3];
                            if (array[3].Equals("second", StringComparison.OrdinalIgnoreCase))
                            {
                                string prefix = text;
                                defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
                                defaultInterpolatedStringHandler.AppendLiteral("(");
                                defaultInterpolatedStringHandler.AppendFormatted(value);
                                defaultInterpolatedStringHandler.AppendLiteral(")");
                                text = prefix + defaultInterpolatedStringHandler.ToStringAndClear();
                            }
                        }
                        break;
                    }
                case "second":
                    {
                        string part0 = array[0];
                        string part1 = array[1];
                        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 2);
                        defaultInterpolatedStringHandler.AppendLiteral("(");
                        defaultInterpolatedStringHandler.AppendFormatted(value2);
                        defaultInterpolatedStringHandler.AppendLiteral(", ");
                        defaultInterpolatedStringHandler.AppendFormatted(value);
                        defaultInterpolatedStringHandler.AppendLiteral(")");
                        text = part0 + " " + part1 + defaultInterpolatedStringHandler.ToStringAndClear();
                        break;
                    }
                default:
                    throw new InvalidOperationException();
            }
            return text;
        }

        private static ReferentialAction? ConvertToReferentialAction(string onDeleteAction)
        {
            return onDeleteAction switch
            {
                "NO ACTION" => (ReferentialAction)0,
                "CASCADE" => (ReferentialAction)2,
                "SET NULL" => (ReferentialAction)3,
                "SET DEFAULT" => (ReferentialAction)4,
                _ => null,
            };
        }

        private static string ConvertToStringReferentialAction(int onDeleteAction)
        {
            return onDeleteAction switch
            {
                3 => "NO ACTION",
                0 => "CASCADE",
                2 => "SET NULL",
                4 => "SET DEFAULT",
                _ => null,
            };
        }

        private static Func<string, string> GenerateSchemaFilter(IReadOnlyList<string> schemas)
        {
            if (schemas.Any())
            {
                return s =>
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(s);
                    stringBuilder.Append(" IN (");
                    stringBuilder.Append(string.Join(", ", schemas.Select(EscapeLiteral)));
                    stringBuilder.Append(")");
                    return stringBuilder.ToString();
                };
            }
            return null;
        }

        private static (string Schema, string Table) Parse(string table)
        {
            Match match = _partExtractor.Match(table.Trim());
            if (!match.Success)
            {
                throw new InvalidOperationException(DmStrings.InvalidTableToIncludeInScaffolding(table));
            }
            string part1 = match.Groups["part1"].Value.Replace("]]", "]");
            string part2 = match.Groups["part2"].Value.Replace("]]", "]");
            if (!string.IsNullOrEmpty(part2))
            {
                return (part1, part2);
            }
            return (null, part1);
        }

        private static Func<string, string, string> GenerateTableFilter(IReadOnlyList<(string Schema, string Table)> tables, Func<string, string> schemaFilter)
        {
            if (schemaFilter != null || tables.Any())
            {
                return (s, t) =>
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    bool flag = false;
                    if (schemaFilter != null)
                    {
                        stringBuilder.Append("(").Append(schemaFilter(s));
                        flag = true;
                    }
                    if (tables.Any())
                    {
                        if (flag)
                        {
                            stringBuilder.AppendLine().Append("OR ");
                        }
                        else
                        {
                            stringBuilder.Append("(");
                            flag = true;
                        }
                        if (tables.Any())
                        {
                            stringBuilder.Append(t);
                            stringBuilder.Append(" IN (");
                            stringBuilder.Append(string.Join(", ", tables.Select(((string Schema, string Table) e) => EscapeLiteral(e.Table))));
                            stringBuilder.Append(")");
                        }
                    }
                    if (flag)
                    {
                        stringBuilder.Append(")");
                    }
                    return stringBuilder.ToString();
                };
            }
            return null;
        }

        private static string EscapeLiteral(string s)
        {
            return "N'" + s + "'";
        }

        public DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
        {
            Check.NotEmpty(connectionString, "connectionString");
            Check.NotNull(options, "options");
            DmConnection dmConnection = new DmConnection(connectionString);
            try
            {
                return Create(dmConnection, options);
            }
            finally
            {
                dmConnection?.Dispose();
            }
        }

        public DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
        {
            Check.NotNull(connection, "connection");
            Check.NotNull(options, "options");
            DatabaseModel databaseModel = new DatabaseModel();
            bool wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen)
            {
                connection.Open();
            }
            try
            {
                databaseModel.DefaultSchema = GetDefaultSchema(connection);
                List<string> schemaList = options.Schemas.ToList();
                Func<string, string> schemaFilter = GenerateSchemaFilter(schemaList);
                List<string> tableList = options.Tables.ToList();
                Func<string, string, string> tableFilter = GenerateTableFilter(tableList.Select(Parse).ToList(), schemaFilter);
                foreach (DatabaseSequence sequence in GetSequences(connection, schemaFilter))
                {
                    sequence.Database = databaseModel;
                    databaseModel.Sequences.Add(sequence);
                }
                GetTables(connection, tableFilter, databaseModel);
                foreach (string item in schemaList.Except((from s in databaseModel.Sequences
                                                           select s.Schema).Concat(from t in databaseModel.Tables
                                                                                   select t.Schema)))
                {
                    _logger.MissingSchemaWarning(item);
                }
                foreach (string item2 in tableList)
                {
                    var (Schema, Table) = Parse(item2);
                    if (!databaseModel.Tables.Any(t => (!string.IsNullOrEmpty(Schema) && t.Schema == Schema) || t.Name == Table))
                    {
                        _logger.MissingTableWarning(item2);
                    }
                }
                return databaseModel;
            }
            finally
            {
                if (!wasOpen)
                {
                    connection.Close();
                }
            }
        }
    }
}
