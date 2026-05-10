namespace Microsoft.EntityFrameworkCore.Migrations.Operations
{
	public class DmDropSchemaOperation : MigrationOperation
	{
		public virtual string Schema { get; set; }

		public DmDropSchemaOperation()
			: base()
		{
		}
	}
}
