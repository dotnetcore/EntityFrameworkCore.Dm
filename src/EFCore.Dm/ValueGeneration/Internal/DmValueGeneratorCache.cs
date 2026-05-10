using System.Collections.Concurrent;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Microsoft.EntityFrameworkCore.Dm.ValueGeneration.Internal
{
	public class DmValueGeneratorCache : ValueGeneratorCache, IDmValueGeneratorCache, IValueGeneratorCache
	{
		private readonly ConcurrentDictionary<string, DmSequenceValueGeneratorState> _sequenceGeneratorCache = new ConcurrentDictionary<string, DmSequenceValueGeneratorState>();

		public DmValueGeneratorCache([NotNull] ValueGeneratorCacheDependencies dependencies)
			: base(dependencies)
		{
		}

		public virtual DmSequenceValueGeneratorState GetOrAddSequenceState(IProperty property, IRelationalConnection connection)
		{
			Check.NotNull(property, "property");
			Check.NotNull(connection, "connection");
			IReadOnlySequence sequence = property.FindHiLoSequence();
			return _sequenceGeneratorCache.GetOrAdd(GetSequenceName(sequence, connection), (string sequenceName) => new DmSequenceValueGeneratorState(sequence));
		}

		private static string GetSequenceName(IReadOnlySequence sequence, IRelationalConnection connection)
		{
			var dbConnection = connection.DbConnection;
			return dbConnection.Database.ToUpperInvariant()
				+ "::"
				+ dbConnection.DataSource.ToUpperInvariant()
				+ "::"
				+ ((sequence.Schema == null) ? "" : (sequence.Schema + "."))
				+ sequence.Name;
		}
	}
}
