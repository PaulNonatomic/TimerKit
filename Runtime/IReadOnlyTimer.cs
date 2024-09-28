namespace Timers.Runtime
{
	public interface IReadOnlyTimer
	{
		/// <summary>
		/// Gets the running state of the timer.
		/// </summary>
		bool IsRunning { get; }
		
		/// <summary>
		/// Gets the total duration of the timer in seconds.
		/// </summary>
		float Duration { get; }
		
		/// <summary>
		/// Gets the remaining time in seconds.
		/// </summary>
		float TimeRemaining { get; }

		/// <summary>
		/// Gets the elapsed time in seconds since the timer was started.
		/// </summary>
		float TimeElapsed { get; }

		/// <summary>
		/// Gets the progress of the timer as a fraction of the elapsed time over the total duration.
		/// </summary>
		float ProgressElapsed { get; }

		/// <summary>
		/// Gets the remaining progress of the timer as a fraction.
		/// </summary>
		float ProgressRemaining { get; }
		
		/// <summary>
		/// Gets the time as either TimeRemaining, TimeElapsed, ProgressElapsed, ProgressRemaining
		/// </summary>
		float TimeByType(TimeType type);
	}
}