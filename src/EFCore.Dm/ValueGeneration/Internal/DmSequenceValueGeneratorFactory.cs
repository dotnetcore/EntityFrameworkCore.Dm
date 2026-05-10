using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Storage.Internal;
using Microsoft.EntityFrameworkCore.Dm.Update.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Microsoft.EntityFrameworkCore.Dm.ValueGeneration.Internal
{
    public class DmSequenceValueGeneratorFactory : IDmSequenceValueGeneratorFactory
    {
        private readonly IDmUpdateSqlGenerator _sqlGenerator;

        public DmSequenceValueGeneratorFactory([NotNull] IDmUpdateSqlGenerator sqlGenerator)
        {
            Check.NotNull(sqlGenerator, "sqlGenerator");
            _sqlGenerator = sqlGenerator;
        }

        public virtual ValueGenerator TryCreate(IProperty property, Type type, DmSequenceValueGeneratorState generatorState, IDmRelationalConnection connection, IRawSqlCommandBuilder rawSqlCommandBuilder, IRelationalCommandDiagnosticsLogger commandLogger)
        {
            Check.NotNull(property, "property");
            Check.NotNull(generatorState, "generatorState");
            Check.NotNull(connection, "connection");
            if (type == typeof(long))
            {
                return new DmSequenceHiLoValueGenerator<long>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(int))
            {
                return new DmSequenceHiLoValueGenerator<int>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(short))
            {
                return new DmSequenceHiLoValueGenerator<short>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(byte))
            {
                return new DmSequenceHiLoValueGenerator<byte>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(char))
            {
                return new DmSequenceHiLoValueGenerator<char>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(ulong))
            {
                return new DmSequenceHiLoValueGenerator<ulong>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(uint))
            {
                return new DmSequenceHiLoValueGenerator<uint>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(ushort))
            {
                return new DmSequenceHiLoValueGenerator<ushort>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            if (type == typeof(sbyte))
            {
                return new DmSequenceHiLoValueGenerator<sbyte>(rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
            }
            return null;
        }
    }
}
