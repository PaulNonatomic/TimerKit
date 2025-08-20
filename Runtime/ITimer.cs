using System;

namespace Nonatomic.Timers
{
	/// <summary>
	/// Defines a contract for timer functionality with milestone support.
	/// Extends IBasicTimer with milestone management capabilities.
	/// </summary>
	public interface ITimer : IBasicTimer
	{
		/// <summary>
		/// Adds a milestone to the timer, which will trigger a specified action when the timer reaches a specific point.
		/// </summary>
		void AddMilestone(TimerMilestone milestone);
		
		/// <summary>
		/// Adds a range milestone that triggers at regular intervals within a specified range.
		/// </summary>
		/// <param name="type">The time type (TimeRemaining, TimeElapsed, etc.)</param>
		/// <param name="rangeStart">The start of the range (higher value for TimeRemaining)</param>
		/// <param name="rangeEnd">The end of the range (lower value for TimeRemaining)</param>
		/// <param name="interval">The interval at which to trigger callbacks</param>
		/// <param name="callback">The callback to execute at each interval</param>
		/// <returns>The created TimerRangeMilestone</returns>
		TimerRangeMilestone AddRangeMilestone(TimeType type, float rangeStart, float rangeEnd, float interval, Action callback);

		/// <summary>
		/// Removes a specific milestone from the timer.
		/// </summary>
		void RemoveMilestone(TimerMilestone milestone);

		/// <summary>
		/// Removes all milestones currently associated with the timer.
		/// </summary>
		void RemoveAllMilestones();

		/// <summary>
		/// Removes milestones that meet a specific condition.
		/// </summary>
		void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition);
	}
}