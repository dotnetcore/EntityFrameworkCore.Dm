using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Dm.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Internal
{
    public class DmModelValidator : RelationalModelValidator
    {
        public DmModelValidator([NotNull] ModelValidatorDependencies dependencies, [NotNull] RelationalModelValidatorDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
        }

        public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.Validate(model, logger);
            ValidateDefaultDecimalMapping(model, logger);
            ValidateByteIdentityMapping(model, logger);
            ValidateNonKeyValueGeneration(model, logger);
        }

        protected virtual void ValidateDefaultDecimalMapping([NotNull] IModel model, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (IProperty item in from p in model.GetEntityTypes().SelectMany(t => t.GetDeclaredProperties())
                                       where (p.ClrType.UnwrapNullableType() == typeof(decimal) && !p.IsForeignKey())

                                       select p)
            {
                var conventionProp = item as IConventionProperty;
                ConfigurationSource? columnTypeSource = (conventionProp != null) ? RelationalPropertyExtensions.GetColumnTypeConfigurationSource(conventionProp) : null;
                ConfigurationSource? typeMappingSource = (conventionProp != null) ? conventionProp.GetTypeMappingConfigurationSource() : null;
                if ((!columnTypeSource.HasValue && ConfigurationSourceExtensions.Overrides((ConfigurationSource)2, typeMappingSource)) || (columnTypeSource.HasValue && ConfigurationSourceExtensions.Overrides((ConfigurationSource)2, columnTypeSource)))
                {
                    logger.DecimalTypeDefaultWarning(item);
                }
            }
        }

        protected virtual void ValidateByteIdentityMapping([NotNull] IModel model, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (IProperty item in from p in model.GetEntityTypes().SelectMany(t => t.GetDeclaredProperties())
                                       where p.ClrType.UnwrapNullableType() == typeof(byte) && (p.GetValueGenerationStrategy() == DmValueGenerationStrategy.IdentityColumn)
                                       select p)
            {
                logger.ByteIdentityColumnWarning(item);
            }
        }

        protected virtual void ValidateNonKeyValueGeneration([NotNull] IModel model, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            using IEnumerator<IProperty> enumerator = model.GetEntityTypes().SelectMany(t => t.GetDeclaredProperties()).Where(p =>
            {
                if (p.GetValueGenerationStrategy() == DmValueGenerationStrategy.SequenceHiLo && DmPropertyExtensions.GetValueGenerationStrategyConfigurationSource((IConventionProperty)p).HasValue && !p.IsKey() && p.ValueGenerated != 0)
                {
                    IAnnotation annotation = p.FindAnnotation("Dm:ValueGenerationStrategy");
                    if (annotation is ConventionAnnotation conventionAnnotation)
                    {
                        return !ConfigurationSourceExtensions.Overrides((ConfigurationSource)2, (ConfigurationSource?)conventionAnnotation.GetConfigurationSource());
                    }
                    return true;
                }
                return false;
            })
                .GetEnumerator();
            if (enumerator.MoveNext())
            {
                IProperty current = enumerator.Current;
                throw new InvalidOperationException(DmStrings.NonKeyValueGeneration(current.Name, current.DeclaringType.DisplayName()));
            }
        }

        protected override void ValidateSharedColumnsCompatibility(IReadOnlyList<IEntityType> mappedTypes, in StoreObjectIdentifier storeObject, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.ValidateSharedColumnsCompatibility(mappedTypes, in storeObject, logger);
            Dictionary<string, IProperty> dictionary = new Dictionary<string, IProperty>();
            foreach (IProperty item in mappedTypes.SelectMany(et => et.GetDeclaredProperties()))
            {
                string columnName = RelationalPropertyExtensions.GetColumnName(item, in storeObject);
                if (columnName != null && item.GetValueGenerationStrategy(in storeObject) == DmValueGenerationStrategy.IdentityColumn)
                {
                    dictionary[columnName] = item;
                }
            }
            if (dictionary.Count > 1)
            {
                throw new InvalidOperationException(DmStrings.MultipleIdentityColumns(new StringBuilder().AppendJoin(dictionary.Values.Select(p => "'" + p.DeclaringType.DisplayName() + "." + p.Name + "'")), storeObject.DisplayName()));
            }
        }
    }
}
