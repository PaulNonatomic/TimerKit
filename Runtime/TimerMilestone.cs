using System;

namespace Nonatomic.TimerKit
{
	public class TimerMilestone
	{
		public TimeType Type { get; set; }
		public float TriggerValue { get; set; }
		public Action Callback { get; set; }

		/// <summary>
		/// Gets whether this milestone should re-trigger every time the timer restarts.
		/// When true, the milestone will not be removed after triggering and will be reset when the timer resets.
		/// </summary>
		public bool IsRecurring { get; set; }

		public TimerMilestone(TimeType type, float triggerValue, Action callback, bool isRecurring = false)
		{
			Type = type;
			TriggerValue = triggerValue;
			Callback = callback;
			IsRecurring = isRecurring;
		}
	}
}