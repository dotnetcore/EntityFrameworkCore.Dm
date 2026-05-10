using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Storage.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Microsoft.EntityFrameworkCore.Dm.ValueGeneration.Internal
{
	public class DmValueGeneratorSelector : RelationalValueGeneratorSelector
	{
		private readonly IDmSequenceValueGeneratorFactory _sequenceFactory;

		private readonly IDmRelationalConnection _connection;

		private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

		private readonly IRelationalCommandDiagnosticsLogger _commandLogger;

		public new virtual IDmValueGeneratorCache Cache => (IDmValueGeneratorCache)base.Cache;

		public DmValueGeneratorSelector([NotNull] ValueGeneratorSelectorDependencies dependencies, [NotNull] IDmSequenceValueGeneratorFactory sequenceFactory, [NotNull] IDmRelationalConnection connection, [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder, [NotNull] IRelationalCommandDiagnosticsLogger commandLogger)
			: base(dependencies)
		{
			_sequenceFactory = sequenceFactory;
			_connection = connection;
			_rawSqlCommandBuilder = rawSqlCommandBuilder;
			_commandLogger = commandLogger;
		}

		public override bool TrySelect(IProperty property, ITypeBase typeBase, out ValueGenerator valueGenerator)
		{
			Check.NotNull(property, "property");
			Check.NotNull(typeBase, "typeBase");
			if (property.GetValueGeneratorFactory() != null || property.GetValueGenerationStrategy() != DmValueGenerationStrategy.SequenceHiLo)
			{
				return base.TrySelect(property, typeBase, out valueGenerator);
			}
			Type propertyType = property.ClrType.UnwrapNullableType().UnwrapEnumType();
			valueGenerator = _sequenceFactory.TryCreate(property, propertyType, Cache.GetOrAddSequenceState(property, _connection), _connection, _rawSqlCommandBuilder, _commandLogger);
			return valueGenerator != null;
		}

		protected override ValueGenerator FindForType(IProperty property, ITypeBase typeBase, Type clrType)
		{
			if (property.ClrType.UnwrapNullableType() == typeof(Guid))
			{
				if (property.ValueGenerated == ValueGenerated.Never || property.GetDefaultValueSql() != null)
				{
					return new TemporaryGuidValueGenerator();
				}
				return new GuidValueGenerator();
			}
			return base.FindForType(property, typeBase, clrType);
		}
	}
}
