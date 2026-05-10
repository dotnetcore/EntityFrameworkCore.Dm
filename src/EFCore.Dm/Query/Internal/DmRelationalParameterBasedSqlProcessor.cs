using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Dm.Query.Internal
{
	internal class DmRelationalParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
	{
		public DmRelationalParameterBasedSqlProcessor(RelationalParameterBasedSqlProcessorDependencies dependencies, RelationalParameterBasedSqlProcessorParameters parameters)
			: base(dependencies, parameters)
		{
		}

		public override Expression Process(Expression queryExpression, ParametersCacheDecorator parametersDecorator)
		{
			var result = base.Process(queryExpression, parametersDecorator);

			// After parameter expansion, bool constants (e.g. WHERE 0 for empty collections)
			// may appear in filter positions. DM does not accept bare value expressions
			// as filter conditions. Re-run the visitor to convert them to search conditions.
			return new SearchConditionConvertingExpressionVisitor(Dependencies.SqlExpressionFactory).Visit(result);
		}

		protected override Expression ProcessSqlNullability(Expression selectExpression, ParametersCacheDecorator parametersDecorator)
			=> new DmSqlNullabilityProcessor(Dependencies, Parameters).Process(selectExpression, parametersDecorator);
    }
}
