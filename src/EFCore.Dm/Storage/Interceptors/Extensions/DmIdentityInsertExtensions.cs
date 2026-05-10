using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Interceptors;

internal static class DmIdentityInsertExtensions
{
    private static readonly Regex InsertRegex = new(
    @"^\s*INSERT\s+INTO\s+(?:[""'\[][\w]+[""'\]]\.)?[""'\[[]?([\w]+)[""'\]\]]?",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static HashSet<string> GetIdentityInsertTable(this DbContext context)
    {
        var tableNames = new HashSet<string>();
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }
            var pk = entry.Metadata.FindPrimaryKey();
            if (pk is null || pk.Properties.Count > 1)
            {
                continue;
            }
            var pkProp = pk.Properties[0];
            if ((pkProp.ClrType != typeof(int) && pkProp.ClrType != typeof(long))
                || pkProp.ValueGenerated != ValueGenerated.OnAdd)
            {
                continue;
            }
            if (!long.TryParse(pkProp.PropertyInfo!.GetValue(entry.Entity)?.ToString(), out var number))
            {
                continue;
            }
            if (number <= 0)
            {
                continue;
            }
            var tableName = pkProp.DeclaringType.GetTableName();
            if (string.IsNullOrEmpty(tableName))
            {
                continue;
            }
            tableNames.Add(tableName);
        }
        return tableNames;
    }

    internal static string WrapIdentityInsert(this HashSet<string> tableNames, string commandText)
    {
        if (tableNames is null || tableNames.Count == 0)
        {
            return commandText;
        }

        if (string.IsNullOrWhiteSpace(commandText))
        {
            return commandText;
        }

        var normalizedTables = new HashSet<string>(tableNames, StringComparer.OrdinalIgnoreCase);

        var lines = commandText.Split(Environment.NewLine);
        var sb = new StringBuilder();

        // 当前 IDENTITY_INSERT 块所属的表（null = 不在任何块内）
        string currentTable = null;

        // 是否处于某条 INSERT 语句的内部（INSERT INTO 头部已匹配，未遇到语句结束符 ;）
        bool insideInsertBody = false;

        foreach (var line in lines)
        {
            // 处在 INSERT 语句内部时跳过头部匹配，直到语句结束
            if (!insideInsertBody && TryParseInsertTable(line, out var tableName))
            {
                if (normalizedTables.Contains(tableName))
                {
                    // 表切换：先关闭旧块
                    if (!string.Equals(currentTable, tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        CloseCurrentBlock();
                        currentTable = tableName;
                        sb.AppendLine($"SET IDENTITY_INSERT {tableName} ON;");
                    }

                    sb.AppendLine(line);
                    insideInsertBody = true;
                    continue;
                }

                // INSERT INTO 的表不在目标集合中 → 关闭当前块，原样输出
                CloseCurrentBlock();
                sb.AppendLine(line);
                insideInsertBody = true;
                continue;
            }

            // INSERT 语句内部的续行（VALUES、ON CONFLICT 等） → 保持块不变
            if (insideInsertBody)
            {
                sb.AppendLine(line);
                // 遇到语句结束符 ; 时退出内部状态，但不关闭 IDENTITY_INSERT 块
                // （下一条可能是同一张表的连续 INSERT）
                if (line.TrimEnd().EndsWith(';'))
                {
                    insideInsertBody = false;
                }

                continue;
            }
            // 非 INSERT 语句（UPDATE / DELETE / SELECT / 注释 / 空行等） → 关闭当前块
            CloseCurrentBlock();
            sb.AppendLine(line);
        }
        CloseCurrentBlock();
        return sb.ToString();
        void CloseCurrentBlock()
        {
            if (currentTable is not null)
            {
                sb.AppendLine($"SET IDENTITY_INSERT {currentTable} OFF;");
                currentTable = null;
            }
        }
    }

    private static bool TryParseInsertTable(string line, out string tableName)
    {
        tableName = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var match = InsertRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        tableName = match.Groups[1].Value;
        return true;
    }
}
