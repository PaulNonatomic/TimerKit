using System;

namespace Nonatomic.Timers
{
	/// <summary>
	/// Represents a range-based milestone that triggers callbacks at regular intervals within a specified range.
	/// Useful for triggering events every X seconds/progress units within a time range.
	/// </summary>
	public class TimerRangeMilestone : TimerMilestone
	{
		/// <summary>
		/// Gets the start value of the range (inclusive).
		/// For TimeRemaining: higher value (e.g., 10 seconds)
		/// For TimeElapsed/Progress: lower value (e.g., 0 seconds)
		/// </summary>
		public float RangeStart { get; }
		
		/// <summary>
		/// Gets the end value of the range (inclusive).
		/// For TimeRemaining: lower value (e.g., 0 seconds)
		/// For TimeElapsed/Progress: higher value (e.g., 10 seconds)
		/// </summary>
		public float RangeEnd { get; }
		
		/// <summary>
		/// Gets the interval at which to trigger callbacks within the range.
		/// </summary>
		public float Interval { get; }
		
		/// <summary>
		/// Gets or sets the last triggered value to prevent duplicate triggers.
		/// </summary>
		internal float? LastTriggeredValue { get; set; }

		/// <summary>
		/// Creates a new range-based milestone.
		/// </summary>
		/// <param name="type">The time type (TimeRemaining, TimeElapsed, etc.)</param>
		/// <param name="rangeStart">The start of the range</param>
		/// <param name="rangeEnd">The end of the range</param>
		/// <param name="interval">The interval at which to trigger within the range</param>
		/// <param name="callback">The callback to execute at each interval</param>
		public TimerRangeMilestone(TimeType type, float rangeStart, float rangeEnd, float interval, Action callback)
			: base(type, CalculateInitialTriggerValue(type, rangeStart), callback)
		{
			RangeStart = rangeStart;
			RangeEnd = rangeEnd;
			Interval = interval;
		}
		
		private static float CalculateInitialTriggerValue(TimeType type, float rangeStart)
		{
			return rangeStart;
		}
		
		/// <summary>
		/// Updates the TriggerValue to the next interval point within the range.
		/// </summary>
		internal void UpdateTriggerValue()
		{
			if (LastTriggeredValue == null)
			{
				TriggerValue = RangeStart;
				return;
			}
			
			TriggerValue = CalculateNextTriggerValue();
		}
		
		private float CalculateNextTriggerValue()
		{
			if (IsCountdownType())
			{
				return CalculateCountdownTriggerValue();
			}
			
			return CalculateCountUpTriggerValue();
		}
		
		private float CalculateCountdownTriggerValue()
		{
			var nextValue = LastTriggeredValue.Value - Interval;
			return Math.Max(nextValue, RangeEnd);
		}
		
		private float CalculateCountUpTriggerValue()
		{
			var nextValue = LastTriggeredValue.Value + Interval;
			return Math.Min(nextValue, RangeEnd);
		}
		
		private bool IsCountdownType()
		{
			return Type == TimeType.TimeRemaining || Type == TimeType.ProgressRemaining;
		}
		
		/// <summary>
		/// Checks if there are more intervals to trigger within the range.
		/// </summary>
		internal bool HasMoreIntervals()
		{
			if (LastTriggeredValue == null) return true;
			
			if (IsCountdownType()) return HasMoreCountdownIntervals();
			
			return HasMoreCountUpIntervals();
		}
		
		private bool HasMoreCountdownIntervals()
		{
			return LastTriggeredValue.Value - Interval >= RangeEnd;
		}
		
		private bool HasMoreCountUpIntervals()
		{
			return LastTriggeredValue.Value + Interval <= RangeEnd;
		}
		
		/// <summary>
		/// Resets the range milestone to its initial state.
		/// </summary>
		public void Reset()
		{
			LastTriggeredValue = null;
			UpdateTriggerValue();
		}
	}
}