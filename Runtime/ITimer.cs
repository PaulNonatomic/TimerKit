using System;

namespace Nonatomic.Timers
{
	/// <summary>
	/// Defines a contract for timer functionality, allowing for starting, stopping, and resetting the timer,
	/// as well as tracking its progress and elapsed time.
	/// </summary>
	public interface ITimer : IReadOnlyTimer
	{
		/// <summary>
		/// Occurs when the timer starts.
		/// </summary>
		event Action OnStart;

		/// <summary>
		/// Occurs when the timer completes its countdown.
		/// </summary>
		event Action OnComplete;
		
		/// <summary>
		/// Occurs when the timer resumes its countdown.
		/// </summary>
		event Action OnResume;
		
		/// <summary>
		/// Occurs when the timer is stopped.
		/// </summary>
		event Action OnStop;

		/// <summary>
		/// Occurs each time the timer is updated, providing the remaining time.
		/// </summary>
		event Action<IReadOnlyTimer> OnTick;

		/// <summary>
		/// Gets or sets the total duration of the timer in seconds.
		/// </summary>
		new float Duration { get; set; }

		/// <summary>
		/// Starts or restarts the timer, resetting the remaining time to the duration.
		/// </summary>
		void StartTimer();

		/// <summary>
		/// Resume the timer without resetting the remaining time.
		/// </summary>
		void ResumeTimer();

		/// <summary>
		/// Stops the timer, pausing the countdown.
		/// </summary>
		void StopTimer();

		/// <summary>
		/// Resets the timer to its initial state with the full duration remaining.
		/// </summary>
		void ResetTimer();

		/// <summary>
		/// Advances the timer forward by the specified number of seconds, reducing the remaining time.
		/// </summary>
		/// <param name="seconds">The amount of time in seconds to advance the timer by.</param>
		void FastForward(float seconds);

		/// <summary>
		/// Rewinds the timer backward by a specified number of seconds.
		/// </summary>
		/// <param name="seconds">The number of seconds to rewind the timer.</param>
		void Rewind(float seconds);

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
		/// Removes a specific milestone from the timer.
		/// </summary>
		void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition);
	}
}