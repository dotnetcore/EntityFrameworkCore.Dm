using System;
using Dm;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmTransientExceptionDetector
	{
		public static bool ShouldRetryOn([NotNull] Exception ex)
		{
			if (ex is DmException)
			{
				return false;
			}
			if (ex is TimeoutException)
			{
				return true;
			}
			return false;
		}
	}
}
