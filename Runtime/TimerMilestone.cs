using System;

namespace Nonatomic.Timers
{
	public class TimerMilestone
	{
		public TimeType Type { get; set; }
		public float TriggerValue { get; set; }
		public Action Callback { get; set; }

		public TimerMilestone(TimeType type, float triggerValue, Action callback)
		{
			Type = type;
			TriggerValue = triggerValue;
			Callback = callback;
		}
	}
}