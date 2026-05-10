using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	public class DmCompiledQueryCacheKeyGenerator : RelationalCompiledQueryCacheKeyGenerator
	{
		private struct DmCompiledQueryCacheKey
		{
			private readonly RelationalCompiledQueryCacheKey _relationalCompiledQueryCacheKey;

			public DmCompiledQueryCacheKey(RelationalCompiledQueryCacheKey relationalCompiledQueryCacheKey)
			{
				_relationalCompiledQueryCacheKey = relationalCompiledQueryCacheKey;
			}

			public override bool Equals(object obj)
			{
				if (obj is not null and DmCompiledQueryCacheKey)
				{
					return Equals((DmCompiledQueryCacheKey)obj);
				}
				return false;
			}

			private bool Equals(DmCompiledQueryCacheKey other)
			{
				return (_relationalCompiledQueryCacheKey).Equals(other._relationalCompiledQueryCacheKey);
			}

			public override int GetHashCode()
			{
				return _relationalCompiledQueryCacheKey.GetHashCode();
			}
		}

		public DmCompiledQueryCacheKeyGenerator([NotNull] CompiledQueryCacheKeyGeneratorDependencies dependencies, [NotNull] RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies)
			: base(dependencies, relationalDependencies)
		{
		}

		public override object GenerateCacheKey(Expression query, bool async)
		{
			return new DmCompiledQueryCacheKey(GenerateCacheKeyCore(query, async));
		}
	}
}
