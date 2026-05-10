using System;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore
{
	public static class DmEntityTypeExtensions
	{
		public static bool IsTemporal(this IReadOnlyEntityType entityType)
		{
			return (((IReadOnlyAnnotatable)entityType)["Dm:IsTemporal"] as bool?).GetValueOrDefault();
		}

		public static string? GetPeriodStartPropertyName(this IReadOnlyEntityType entityType)
		{
			if (entityType is not RuntimeEntityType)
			{
				return ((IReadOnlyAnnotatable)entityType)["Dm:TemporalPeriodStartPropertyName"] as string;
			}
			throw new InvalidOperationException(CoreStrings.RuntimeModelMissingData);
		}

		public static string? GetPeriodEndPropertyName(this IReadOnlyEntityType entityType)
		{
			if (entityType is not RuntimeEntityType)
			{
				return ((IReadOnlyAnnotatable)entityType)["Dm:TemporalPeriodEndPropertyName"] as string;
			}
			throw new InvalidOperationException(CoreStrings.RuntimeModelMissingData);
		}
	}
}
