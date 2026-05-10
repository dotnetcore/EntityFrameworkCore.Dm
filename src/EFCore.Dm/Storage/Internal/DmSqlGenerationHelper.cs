using System;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Dm.Storage.Internal
{
	public class DmSqlGenerationHelper : RelationalSqlGenerationHelper
	{
		public override string BatchTerminator => Environment.NewLine + "/";

		public DmSqlGenerationHelper([NotNull] RelationalSqlGenerationHelperDependencies dependencies)
			: base(dependencies)
		{
		}

		// DM identifiers must start with a letter or underscore, not a digit.
		// EF Core may generate parameter names like "8__locals1_lang" from C# compiler
		// closure display-class field names. Prefix with "p" to make them valid.
		private static string SanitizeParameterName(string name)
			=> name.Length > 0 && char.IsDigit(name[0]) ? "p" + name : name;

		public override void GenerateParameterName(StringBuilder builder, string name)
		{
			builder.Append(":").Append(SanitizeParameterName(name));
		}

		public override string GenerateParameterName(string name)
		{
			if (name.StartsWith(":", StringComparison.Ordinal))
			{
				return name;
			}
			return ":" + SanitizeParameterName(name);
		}

		public override string EscapeIdentifier(string identifier)
		{
			return Check.NotEmpty(identifier, "identifier").Replace("\"", "\"\"");
		}

		public override void EscapeIdentifier(StringBuilder builder, string identifier)
		{
			Check.NotEmpty(identifier, "identifier");
			int length = builder.Length;
			builder.Append(identifier);
			builder.Replace("\"", "\"\"", length, identifier.Length);
		}

		public override string DelimitIdentifier(string identifier)
		{
			return "\"" + EscapeIdentifier(Check.NotEmpty(identifier, "identifier")) + "\"";
		}

		public override void DelimitIdentifier(StringBuilder builder, string identifier)
		{
			Check.NotEmpty(identifier, "identifier");
			builder.Append('"');
			EscapeIdentifier(builder, identifier);
			builder.Append('"');
		}
	}
}
