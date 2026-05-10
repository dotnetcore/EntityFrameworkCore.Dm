using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Migrations.Operations
{
	public class DmCreateSchemaOperation : MigrationOperation
	{
		public virtual string UserName
		{
			get; [param: NotNull]
			set;
		}

		public virtual string Schema { get; set; }

		public DmCreateSchemaOperation()
			: base()
		{
		}
	}
}
