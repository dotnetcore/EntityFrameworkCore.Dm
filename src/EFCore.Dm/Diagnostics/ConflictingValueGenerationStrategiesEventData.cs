using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.Diagnostics
{
	public class ConflictingValueGenerationStrategiesEventData : EventData
	{
		public virtual DmValueGenerationStrategy DmValueGenerationStrategy { get; }

		public virtual string OtherValueGenerationStrategy { get; }

		public virtual IReadOnlyProperty Property { get; }

		public ConflictingValueGenerationStrategiesEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DmValueGenerationStrategy dmValueGenerationStrategy, string otherValueGenerationStrategy, IReadOnlyProperty property)
			: base(eventDefinition, messageGenerator)
		{
			DmValueGenerationStrategy = dmValueGenerationStrategy;
			OtherValueGenerationStrategy = otherValueGenerationStrategy;
			Property = property;
		}
	}
}
