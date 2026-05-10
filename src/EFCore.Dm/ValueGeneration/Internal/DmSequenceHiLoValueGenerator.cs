using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Storage.Internal;
using Microsoft.EntityFrameworkCore.Dm.Update.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Dm.ValueGeneration.Internal
{
	public class DmSequenceHiLoValueGenerator<TValue> : HiLoValueGenerator<TValue>
	{
		private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

		private readonly IDmUpdateSqlGenerator _sqlGenerator;

		private readonly IDmRelationalConnection _connection;

		private readonly IReadOnlySequence _sequence;

		private readonly IRelationalCommandDiagnosticsLogger _commandLogger;

		public override bool GeneratesTemporaryValues => false;

		public DmSequenceHiLoValueGenerator([NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder, [NotNull] IDmUpdateSqlGenerator sqlGenerator, [NotNull] DmSequenceValueGeneratorState generatorState, [NotNull] IDmRelationalConnection connection, [NotNull] IRelationalCommandDiagnosticsLogger commandLogger)
			: base((HiLoValueGeneratorState)(object)generatorState)
		{
			_sequence = generatorState.Sequence;
			_rawSqlCommandBuilder = rawSqlCommandBuilder;
			_sqlGenerator = sqlGenerator;
			_connection = connection;
			_commandLogger = commandLogger;
		}

		protected override long GetNewLowValue()
		{
			return (long)Convert.ChangeType(_rawSqlCommandBuilder.Build((_sqlGenerator).GenerateNextSequenceValueOperation(_sequence.Name, _sequence.Schema)).ExecuteScalar(new RelationalCommandParameterObject((IRelationalConnection)_connection, null, null, null, _commandLogger, (CommandSource)6)), typeof(long), CultureInfo.InvariantCulture);
		}

		protected override async Task<long> GetNewLowValueAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return (long)Convert.ChangeType(await _rawSqlCommandBuilder.Build(_sqlGenerator.GenerateNextSequenceValueOperation(_sequence.Name, _sequence.Schema)).ExecuteScalarAsync(new RelationalCommandParameterObject(_connection, null, null, null, _commandLogger), cancellationToken), typeof(long), CultureInfo.InvariantCulture);
		}
	}
}
