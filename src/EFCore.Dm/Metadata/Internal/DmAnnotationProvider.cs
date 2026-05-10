using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Dm.Metadata.Internal
{
    public class DmAnnotationProvider : RelationalAnnotationProvider
    {
        public DmAnnotationProvider([NotNull] RelationalAnnotationProviderDependencies dependencies)
            : base(dependencies)
        {
        }

        public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
        {
            StoreObjectIdentifier table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
            IProperty val = (from m in column.PropertyMappings
                             select m.Property).FirstOrDefault(p => p.GetValueGenerationStrategy(in table) == DmValueGenerationStrategy.IdentityColumn);
            if (val != null)
            {
                int? identitySeed = val.GetIdentitySeed();
                yield return new Annotation("Dm:Identity", string.Format(arg1: val.GetIdentityIncrement().GetValueOrDefault(1), provider: CultureInfo.InvariantCulture, format: "{0}, {1}", arg0: identitySeed.GetValueOrDefault(1)));
            }
        }
    }
}
