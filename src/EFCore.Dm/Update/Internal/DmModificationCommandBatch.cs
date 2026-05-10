using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.EntityFrameworkCore.Dm.Update.Internal
{
    public class DmModificationCommandBatch : AffectedCountModificationCommandBatch
    {
        private const int DefaultNetworkPacketSizeBytes = 4096;

        private const int MaxScriptLength = 134217728;

        private const int MaxParameterCount = 2100;

        private const int MaxRowCount = 1000;

        private int _parameterCount = 1;

        private readonly int _maxBatchSize;

        private int _commandsLeftToLengthCheck = 50;

        protected virtual new IDmUpdateSqlGenerator UpdateSqlGenerator => (IDmUpdateSqlGenerator)base.UpdateSqlGenerator;

        public DmModificationCommandBatch([NotNull] ModificationCommandBatchFactoryDependencies dependencies, int? maxBatchSize)
            : base(dependencies, null)
        {
            if (maxBatchSize.HasValue && maxBatchSize.Value <= 0)
            {
                throw new ArgumentOutOfRangeException("maxBatchSize", RelationalStrings.InvalidMaxBatchSize(maxBatchSize.Value));
            }
            _maxBatchSize = Math.Min(maxBatchSize.GetValueOrDefault(int.MaxValue), MaxRowCount);
        }

        protected bool CanAddCommand(IReadOnlyModificationCommand modificationCommand)
        {
            if (ModificationCommands.Count >= _maxBatchSize)
            {
                return false;
            }
            int num = CountParameters(modificationCommand);
            if (_parameterCount + num >= MaxParameterCount)
            {
                return false;
            }
            _parameterCount += num;
            return true;
        }

        protected override bool IsValid()
        {
            if (--_commandsLeftToLengthCheck < 0)
            {
                int length = GetCommandText().Length;
                if (length >= MaxScriptLength)
                {
                    return false;
                }
                int num = length / ModificationCommands.Count;
                int num2 = (MaxScriptLength - length) / num;
                _commandsLeftToLengthCheck = Math.Max(1, num2 / 4);
            }
            return true;
        }

        protected int GetParameterCount()
        {
            return _parameterCount;
        }

        private static int CountParameters(IReadOnlyModificationCommand modificationCommand)
        {
            int num = 0;
            for (int i = 0; i < modificationCommand.ColumnModifications.Count; i++)
            {
                IColumnModification columnModification = modificationCommand.ColumnModifications[i];
                if (columnModification.UseCurrentValueParameter)
                {
                    num++;
                }
                if (columnModification.UseOriginalValueParameter)
                {
                    num++;
                }
            }
            return num;
        }

        protected string GetCommandText()
        {
            if (ModificationCommands.Count > 1)
            {
                return "BEGIN " + SqlBuilder.ToString() + " END; ";
            }
            return SqlBuilder.ToString();
        }
    }
}
