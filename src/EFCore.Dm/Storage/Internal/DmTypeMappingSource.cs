using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
    public class DmTypeMappingSource : RelationalTypeMappingSource
    {
        private readonly DmByteArrayTypeMapping _rowversion = new DmByteArrayTypeMapping("BINARY(8)", System.Data.DbType.Binary, 8, fixedLength: false, (ValueComparer)(object)new ValueComparer<byte[]>((Expression<Func<byte[], byte[], bool>>)((byte[] v1, byte[] v2) => StructuralComparisons.StructuralEqualityComparer.Equals(v1, v2)), (Expression<Func<byte[], int>>)((byte[] v) => StructuralComparisons.StructuralEqualityComparer.GetHashCode(v)), (Expression<Func<byte[], byte[]>>)((byte[] v) => (v == null) ? null : v.ToArray())), (StoreTypePostfix)0);

        private readonly BoolTypeMapping _bool = new DmBoolTypeMapping("BIT", DbType.Boolean);

        private readonly ByteTypeMapping _byte = new DmByteTypeMapping("SMALLINT", DbType.Byte);

        private readonly SByteTypeMapping _sByte = new DmSByteTypeMapping("TINYINT", System.Data.DbType.SByte);

        private readonly ShortTypeMapping _short = new ShortTypeMapping("SMALLINT", (DbType?)DbType.Int16);

        private readonly IntTypeMapping _int = new IntTypeMapping("INT", (DbType?)DbType.Int32);

        private readonly LongTypeMapping _long = new LongTypeMapping("BIGINT", (DbType?)DbType.Int64);

        private readonly DecimalTypeMapping _decimal = new DmDecimalTypeMapping("DECIMAL(29, 4)", DbType.Decimal);

        private readonly FloatTypeMapping _real = new DmFloatTypeMapping("REAL", DbType.Single);

        private readonly DoubleTypeMapping _double = new DmDoubleTypeMapping("FLOAT", DbType.Double);

        private readonly DmStringTypeMapping _fixedLengthUnicodeString = new DmStringTypeMapping("NCHAR", DbType.String, unicode: true, null, fixedLength: true);

        private readonly DmStringTypeMapping _variableLengthUnicodeString = new DmStringTypeMapping("NVARCHAR", null, unicode: true);

        private readonly DmStringTypeMapping _fixedLengthAnsiString = new DmStringTypeMapping("CHAR", DbType.AnsiString, unicode: false, null, fixedLength: true);

        private readonly DmStringTypeMapping _variableLengthAnsiString = new DmStringTypeMapping("VARCHAR", DbType.AnsiString);

        private readonly DmStringTypeMapping _variableLengthAnsiString2 = new DmStringTypeMapping("VARCHAR2", DbType.AnsiString);

        private readonly DmStringTypeMapping _variableLengthUnicodeString2 = new DmStringTypeMapping("NVARCHAR2", null, unicode: true);

        private readonly DmStringTypeMapping _rowId = new DmStringTypeMapping("ROWID", DbType.AnsiString);

        private readonly DmStringTypeMapping _textAnsiString = new DmStringTypeMapping("text", DbType.AnsiString);

        private readonly DmStringTypeMapping _xml = new DmStringTypeMapping("VARCHAR(8188)", DbType.String);

        private readonly DmStringTypeMapping _json = new DmStringTypeMapping("JSON", DbType.String);

        private readonly GuidTypeMapping _guid = new GuidTypeMapping("CHAR(36)", (DbType?)DbType.Guid);

        private readonly DmByteArrayTypeMapping _blobBinary = new DmByteArrayTypeMapping("blob", System.Data.DbType.Binary);

        private readonly DmByteArrayTypeMapping _fixedLengthBinary = new DmByteArrayTypeMapping("BINARY", System.Data.DbType.Binary, null, fixedLength: true);

        private readonly DmByteArrayTypeMapping _variableLengthBinary = new DmByteArrayTypeMapping("VARBINARY", System.Data.DbType.Binary);

        private readonly DmDateOnlyTypeMapping _dateonly = new DmDateOnlyTypeMapping("DATE", DbType.Date);

        private readonly DmTimeOnlyTypeMapping _timeonly = new DmTimeOnlyTypeMapping("TIME", DbType.Time);

        private readonly TimeSpanTypeMapping _timespan = new DmTimeSpanTypeMapping("TIME");

        private readonly DmDateTimeTypeMapping _datetime = new DmDateTimeTypeMapping("TIMESTAMP", DbType.DateTime);

        private readonly DmDateTimeTypeMapping _datetime2 = new DmDateTimeTypeMapping("TIMESTAMP", DbType.DateTime2);

        private readonly DmDateTimeOffsetTypeMapping _datetimeoffset = new DmDateTimeOffsetTypeMapping("DATETIME WITH TIME ZONE", System.Data.DbType.DateTimeOffset);

        private readonly LongTypeMapping _intervalYM = new LongTypeMapping("INTERVAL YEAR TO MONTH", (DbType?)DbType.Int64);

        private readonly TimeSpanTypeMapping _intervalDT = new DmTimeSpanTypeMapping("INTERVAL DAY TO SECOND");

        private readonly Dictionary<string, RelationalTypeMapping[]> _storeTypeMappings;

        private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;

        private readonly HashSet<string> _disallowedMappings;

        public DmTypeMappingSource([NotNull] TypeMappingSourceDependencies dependencies, [NotNull] RelationalTypeMappingSourceDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
            _storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "bit",
                    new RelationalTypeMapping[1] { _bool }
                },
                {
                    "byte",
                    new RelationalTypeMapping[1] { _sByte }
                },
                {
                    "tinyint",
                    new RelationalTypeMapping[1] { _sByte }
                },
                {
                    "smallint",
                    new RelationalTypeMapping[1] { _short }
                },
                {
                    "int",
                    new RelationalTypeMapping[1] { _int }
                },
                {
                    "integer",
                    new RelationalTypeMapping[1] { _int }
                },
                {
                    "bigint",
                    new RelationalTypeMapping[1] { _long }
                },
                {
                    "dec",
                    new RelationalTypeMapping[1] { _decimal }
                },
                {
                    "decimal",
                    new RelationalTypeMapping[1] { _decimal }
                },
                {
                    "money",
                    new RelationalTypeMapping[1] { _decimal }
                },
                {
                    "numeric",
                    new RelationalTypeMapping[1] { _decimal }
                },
                {
                    "smallmoney",
                    new RelationalTypeMapping[1] { _decimal }
                },
                {
                    "number",
                    new RelationalTypeMapping[1] { _decimal }
                },
                {
                    "real",
                    new RelationalTypeMapping[1] { _real }
                },
                {
                    "float",
                    new RelationalTypeMapping[1] { _double }
                },
                {
                    "double",
                    new RelationalTypeMapping[1] { _double }
                },
                {
                    "double precision",
                    new RelationalTypeMapping[1] { _double }
                },
                {
                    "national character",
                    new RelationalTypeMapping[1] { _fixedLengthUnicodeString }
                },
                {
                    "nchar",
                    new RelationalTypeMapping[1] { _fixedLengthUnicodeString }
                },
                {
                    "national char varying",
                    new RelationalTypeMapping[1] { _variableLengthUnicodeString }
                },
                {
                    "national character varying",
                    new RelationalTypeMapping[1] { _variableLengthUnicodeString }
                },
                {
                    "nvarchar",
                    new RelationalTypeMapping[1] { _variableLengthUnicodeString }
                },
                {
                    "char",
                    new RelationalTypeMapping[1] { _fixedLengthAnsiString }
                },
                {
                    "character",
                    new RelationalTypeMapping[1] { _fixedLengthAnsiString }
                },
                {
                    "char varying",
                    new RelationalTypeMapping[1] { _variableLengthAnsiString }
                },
                {
                    "character varying",
                    new RelationalTypeMapping[1] { _variableLengthAnsiString }
                },
                {
                    "varchar",
                    new RelationalTypeMapping[1] { _variableLengthAnsiString }
                },
                {
                    "varchar2",
                    new RelationalTypeMapping[1] { _variableLengthAnsiString2 }
                },
                {
                    "nvarchar2",
                    new RelationalTypeMapping[1] { _variableLengthUnicodeString2 }
                },
                {
                    "rowid",
                    new RelationalTypeMapping[1] { _rowId }
                },
                {
                    "ntext",
                    new RelationalTypeMapping[1] { _textAnsiString }
                },
                {
                    "text",
                    new RelationalTypeMapping[1] { _textAnsiString }
                },
                {
                    "long",
                    new RelationalTypeMapping[1] { _textAnsiString }
                },
                {
                    "longvarchar",
                    new RelationalTypeMapping[1] { _textAnsiString }
                },
                {
                    "clob",
                    new RelationalTypeMapping[1] { _textAnsiString }
                },
                {
                    "xmltype",
                    new RelationalTypeMapping[1] { _textAnsiString }
                },
                {
                    "xml",
                    new RelationalTypeMapping[1] { _xml }
                },
                {
                    "json",
                    new RelationalTypeMapping[2]
                    {
                        DmJsonTypeMapping.Default,
                        _json
                    }
                },
                {
                    "jsonb",
                    new RelationalTypeMapping[2]
                    {
                        DmJsonTypeMapping.Default,
                        _json
                    }
                },
                {
                    "char(36)",
                    new RelationalTypeMapping[1] { _guid }
                },
                {
                    "blob",
                    new RelationalTypeMapping[1] { _blobBinary }
                },
                {
                    "binary",
                    new RelationalTypeMapping[1] { _fixedLengthBinary }
                },
                {
                    "binary varying",
                    new RelationalTypeMapping[1] { _variableLengthBinary }
                },
                {
                    "varbinary",
                    new RelationalTypeMapping[1] { _variableLengthBinary }
                },
                {
                    "image",
                    new RelationalTypeMapping[1] { _variableLengthBinary }
                },
                {
                    "longvarbinary",
                    new RelationalTypeMapping[1] { _variableLengthBinary }
                },
                {
                    "bfile",
                    new RelationalTypeMapping[1] { _variableLengthBinary }
                },
                {
                    "date",
                    new RelationalTypeMapping[2]
                    {
                        _dateonly,
                        _datetime
                    }
                },
                {
                    "time",
                    new RelationalTypeMapping[2]
                    {
                        _timeonly,
                        _timespan
                    }
                },
                {
                    "datetime",
                    new RelationalTypeMapping[1] { _datetime }
                },
                {
                    "timestamp with local time zone",
                    new RelationalTypeMapping[1] { _datetime }
                },
                {
                    "smalldatetime",
                    new RelationalTypeMapping[1] { _datetime }
                },
                {
                    "timestamp",
                    new RelationalTypeMapping[1] { _datetime }
                },
                {
                    "datetime2",
                    new RelationalTypeMapping[1] { _datetime2 }
                },
                {
                    "time with time zone",
                    new RelationalTypeMapping[1] { _datetimeoffset }
                },
                {
                    "datetime with time zone",
                    new RelationalTypeMapping[1] { _datetimeoffset }
                },
                {
                    "timestamp with time zone",
                    new RelationalTypeMapping[1] { _datetimeoffset }
                },
                {
                    "interval year",
                    new RelationalTypeMapping[1] { _intervalYM }
                },
                {
                    "interval year to month",
                    new RelationalTypeMapping[1] { _intervalYM }
                },
                {
                    "interval month",
                    new RelationalTypeMapping[1] { _intervalYM }
                },
                {
                    "interval day",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval day to hour",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval day to minute",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval day to second",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval hour",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval hour to minute",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval hour to second",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval minute",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval minute to second",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "interval second",
                    new RelationalTypeMapping[1] { _intervalDT }
                },
                {
                    "rowversion",
                    new RelationalTypeMapping[1] { _rowversion }
                }
            };
            _clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
            {
                {
                    typeof(int),
                    _int
                },
                {
                    typeof(long),
                    _long
                },
                {
                    typeof(DateOnly),
                    _dateonly
                },
                {
                    typeof(TimeOnly),
                    _timeonly
                },
                {
                    typeof(DateTime),
                    _datetime2
                },
                {
                    typeof(Guid),
                    _guid
                },
                {
                    typeof(bool),
                    _bool
                },
                {
                    typeof(sbyte),
                    _sByte
                },
                {
                    typeof(byte),
                    _byte
                },
                {
                    typeof(double),
                    _double
                },
                {
                    typeof(DateTimeOffset),
                    _datetimeoffset
                },
                {
                    typeof(short),
                    _short
                },
                {
                    typeof(float),
                    _real
                },
                {
                    typeof(decimal),
                    _decimal
                },
                {
                    typeof(TimeSpan),
                    _intervalDT
                },
                {
                    typeof(JsonElement),
                    DmJsonTypeMapping.Default
                }
            };
            _disallowedMappings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "binary varying", "binary", "char varying", "char", "character varying", "character", "national char varying", "national character varying", "national character", "nchar",
                "nvarchar", "varbinary", "varchar"
            };
        }

        protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            RelationalTypeMapping mapping = FindRawMapping(mappingInfo);
            if (mapping == null)
            {
                return null;
            }
            RelationalTypeMappingInfo? mappingInfoCopy = mappingInfo;
            return mapping.Clone(in mappingInfoCopy, null, null, null, null, null, null, null, null);
        }

        private RelationalTypeMapping FindRawMapping(RelationalTypeMappingInfo mappingInfo)
        {
            Type clrType = mappingInfo.ClrType;
            string storeTypeName = mappingInfo.StoreTypeName;
            string storeTypeNameBase = mappingInfo.StoreTypeNameBase;
            if (storeTypeName != null)
            {
                if (clrType == typeof(float) && mappingInfo.Size.HasValue && mappingInfo.Size <= 24 && (storeTypeNameBase.Equals("float", StringComparison.OrdinalIgnoreCase) || storeTypeNameBase.Equals("double precision", StringComparison.OrdinalIgnoreCase)))
                {
                    return _real;
                }
                if (_storeTypeMappings.TryGetValue(storeTypeName, out var candidates) || _storeTypeMappings.TryGetValue(storeTypeNameBase, out candidates))
                {
                    if (clrType is null)
                    {
                        return candidates[0];
                    }
                    foreach (RelationalTypeMapping candidate in candidates)
                    {
                        if (candidate.ClrType == clrType)
                        {
                            return candidate;
                        }
                    }
                    return null;
                }
            }
            if (clrType != null)
            {
                if (_clrTypeMappings.TryGetValue(clrType, out var clrMapping))
                {
                    return clrMapping;
                }
                if (clrType == typeof(string))
                {
                    bool isAnsi = mappingInfo.IsUnicode == false;
                    bool isFixedLength = mappingInfo.IsFixedLength.GetValueOrDefault();
                    string baseTypeName = (isAnsi ? "" : "N") + (isFixedLength ? "CHAR" : "VARCHAR2");
                    int maxSize = 32767;
                    int defaultSize = (isFixedLength ? 1 : maxSize);
                    StoreTypePostfix? storeTypePostfix = null;
                    int? size = mappingInfo.Size ?? ((!mappingInfo.IsKeyOrIndex) ? defaultSize : (isAnsi ? 900 : 450));
                    if (size > maxSize)
                    {
                        size = null;
                        storeTypePostfix = (StoreTypePostfix)0;
                    }
                    else
                    {
                        storeTypePostfix = (StoreTypePostfix)1;
                    }
                    string storeType;
                    if (storeTypePostfix == (StoreTypePostfix?)0)
                    {
                        storeType = (isAnsi ? "clob" : "nclob");
                    }
                    else
                    {
                        storeType = baseTypeName + "(" + size + ")";
                    }
                    return new DmStringTypeMapping(storeType, isAnsi ? new DbType?(DbType.AnsiString) : null, !isAnsi, size, isFixedLength, storeTypePostfix);
                }
                if (clrType == typeof(byte[]))
                {
                    if (mappingInfo.IsRowVersion.GetValueOrDefault())
                    {
                        return _rowversion;
                    }
                    bool isFixedLength = mappingInfo.IsFixedLength.GetValueOrDefault();
                    string baseTypeName = (isFixedLength ? "BINARY" : "VARBINARY");
                    int maxSize = 32767;
                    int defaultSize = (isFixedLength ? 1 : maxSize);
                    int? size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? new int?(900) : new int?(defaultSize));
                    StoreTypePostfix? storeTypePostfix = null;
                    if (size > maxSize)
                    {
                        size = null;
                        storeTypePostfix = (StoreTypePostfix)0;
                    }
                    else
                    {
                        storeTypePostfix = (StoreTypePostfix)1;
                    }
                    string storeType;
                    if (storeTypePostfix != (StoreTypePostfix?)0)
                    {
                        storeType = baseTypeName + "(" + size + ")";
                    }
                    else
                    {
                        storeType = "blob";
                    }
                    return new DmByteArrayTypeMapping(storeType, DbType.Binary, size, fixedLength: false, null, storeTypePostfix);
                }
            }
            return null;
        }

        protected override string? ParseStoreTypeName(string? storeTypeName, ref bool? unicode, ref int? size, ref int? precision, ref int? scale)
        {
            unicode = null;
            size = null;
            precision = null;
            scale = null;
            if (storeTypeName != null && storeTypeName!.StartsWith("interval", StringComparison.OrdinalIgnoreCase))
            {
                return processInterval(storeTypeName, out precision, ref scale);
            }
            if (storeTypeName != null && storeTypeName!.EndsWith("time zone", StringComparison.OrdinalIgnoreCase))
            {
                return processTimeZone(storeTypeName, out precision);
            }
            return base.ParseStoreTypeName(storeTypeName, ref unicode, ref size, ref precision, ref scale);
        }

        private static string? processTimeZone(string storeTypeName, out int? precision)
        {
            string[] parts = storeTypeName.Split(' ');
            int parenOpen = parts[0].IndexOf("(");
            int parenClose = parts[0].IndexOf(")");
            if (parenOpen < 0)
            {
                precision = 6;
                return storeTypeName;
            }
            precision = int.Parse(parts[0].Substring(parenOpen + 1, parenClose - parenOpen - 1));
            parts[0] = parts[0].Substring(0, parenOpen);
            return string.Join(" ", parts);
        }

        private static string? processInterval(string storeTypeName, out int? precision, ref int? scale)
        {
            if (storeTypeName.StartsWith("interval second(", StringComparison.OrdinalIgnoreCase))
            {
                int parenOpen = storeTypeName.IndexOf("(");
                int parenClose = storeTypeName.IndexOf(")");
                string innerText = storeTypeName.Substring(parenOpen + 1, parenClose - parenOpen - 1);
                if (!innerText.Contains(',', StringComparison.OrdinalIgnoreCase))
                {
                    precision = int.Parse(innerText);
                }
                else
                {
                    string[] segments = innerText.Split(',');
                    precision = int.Parse(segments[0].Trim());
                    scale = int.Parse(segments[1].Trim());
                }
                return storeTypeName.Substring(0, parenOpen);
            }
            string[] parts = storeTypeName.Split(' ');
            int num3 = parts[1].IndexOf("(");
            int num4 = parts[1].IndexOf(")");
            if (num3 < 0)
            {
                precision = 2;
                if (parts[1].Equals("second", StringComparison.OrdinalIgnoreCase))
                {
                    scale = 6;
                }
            }
            else
            {
                precision = int.Parse(parts[1].Substring(num3 + 1, num4 - num3 - 1));
                parts[1] = parts[1].Substring(0, num3);
            }
            if (parts.Length != 2)
            {
                int num5 = parts[3].IndexOf("(");
                int num6 = parts[3].IndexOf(")");
                if (num5 < 0)
                {
                    if (parts[3].Equals("second", StringComparison.OrdinalIgnoreCase))
                    {
                        scale = 6;
                    }
                }
                else
                {
                    scale = int.Parse(parts[3].Substring(num5 + 1, num6 - num5 - 1));
                    parts[3] = parts[3].Substring(0, num5);
                }
            }
            return string.Join(" ", parts);
        }
    }
}
