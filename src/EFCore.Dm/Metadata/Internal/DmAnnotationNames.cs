namespace Microsoft.EntityFrameworkCore.Dm.Metadata.Internal
{
	public static class DmAnnotationNames
	{
		public const string Prefix = "Dm:";

		public const string ValueGenerationStrategy = "Dm:ValueGenerationStrategy";

		public const string HiLoSequenceName = "Dm:HiLoSequenceName";

		public const string HiLoSequenceSchema = "Dm:HiLoSequenceSchema";

		public const string SequenceNameSuffix = "Dm:SequenceNameSuffix";

		public const string SequenceName = "Dm:SequenceName";

		public const string SequenceSchema = "Dm:SequenceSchema";

		public const string Identity = "Dm:Identity";

		public const string IdentitySeed = "Dm:IdentitySeed";

		public const string IdentityIncrement = "Dm:IdentityIncrement";

		public const string MaxDatabaseSize = "Dm:DatabaseMaxSize";

		public const string ServiceTierSql = "Dm:ServiceTierSql";

		public const string PerformanceLevelSql = "Dm:PerformanceLevelSql";

		public const string IsTemporal = "Dm:IsTemporal";

		public const string TemporalPeriodStartPropertyName = "Dm:TemporalPeriodStartPropertyName";

		public const string TemporalPeriodEndPropertyName = "Dm:TemporalPeriodEndPropertyName";
	}
}
