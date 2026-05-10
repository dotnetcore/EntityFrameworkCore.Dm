using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
    internal class DmRelationalParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
    {
        protected virtual RelationalParameterBasedSqlProcessorDependencies Dependencies { get; }

        public DmRelationalParameterBasedSqlProcessorFactory(RelationalParameterBasedSqlProcessorDependencies dependencies)
        {
            Dependencies = dependencies;
        }

        public RelationalParameterBasedSqlProcessor Create(RelationalParameterBasedSqlProcessorParameters parameters)
        {
            return new DmRelationalParameterBasedSqlProcessor(Dependencies, parameters);
        }
    }
}
