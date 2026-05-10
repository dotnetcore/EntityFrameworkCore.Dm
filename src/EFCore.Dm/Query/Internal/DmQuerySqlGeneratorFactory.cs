using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	public class DmQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
	{
		private readonly QuerySqlGeneratorDependencies _dependencies;

		private readonly IRelationalTypeMappingSource _typeMappingSource;

		public DmQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies, IRelationalTypeMappingSource typeMappingSource)
		{
			_dependencies = dependencies;
			_typeMappingSource = typeMappingSource;
		}

		public virtual QuerySqlGenerator Create()
		{
			return new DmQuerySqlGenerator(_dependencies, _typeMappingSource);
		}
	}
}
