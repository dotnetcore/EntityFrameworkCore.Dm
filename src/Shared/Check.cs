using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Utilities
{
	[DebuggerStepThrough]
	internal static class Check
	{
		[ContractAnnotation("value:null => halt")]
		public static T NotNull<T>([NoEnumeration] T value, [InvokerParameterName][NotNull] string parameterName)
		{
			if (value == null)
			{
				NotEmpty(parameterName, "parameterName");
				throw new ArgumentNullException(parameterName);
			}
			return value;
		}

		[ContractAnnotation("value:null => halt")]
		public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T> value, [InvokerParameterName][NotNull] string parameterName)
		{
			NotNull(value, parameterName);
			if (value.Count == 0)
			{
				NotEmpty(parameterName, "parameterName");
				throw new ArgumentException($"The collection argument '{parameterName}' must not be empty.");
			}
			return value;
		}

		[ContractAnnotation("value:null => halt")]
		public static string NotEmpty(string value, [InvokerParameterName][NotNull] string parameterName)
		{
			Exception ex = null;
			if (value == null)
			{
				ex = new ArgumentNullException(parameterName);
			}
			else if (value.Trim().Length == 0)
			{
				ex = new ArgumentException($"The string argument '{parameterName}' must not be empty or whitespace.");
			}
			if (ex != null)
			{
				NotEmpty(parameterName, "parameterName");
				throw ex;
			}
			return value;
		}

		public static string NullButNotEmpty(string value, [InvokerParameterName][NotNull] string parameterName)
		{
			if (value != null && value.Length == 0)
			{
				NotEmpty(parameterName, "parameterName");
				throw new ArgumentException($"The string argument '{parameterName}' must not be empty or whitespace.");
			}
			return value;
		}

		public static IReadOnlyList<T> HasNoNulls<T>(IReadOnlyList<T> value, [InvokerParameterName][NotNull] string parameterName) where T : class
		{
			NotNull(value, parameterName);
			if (value.Any((T e) => e == null))
			{
				NotEmpty(parameterName, "parameterName");
				throw new ArgumentException(parameterName);
			}
			return value;
		}
	}
}
