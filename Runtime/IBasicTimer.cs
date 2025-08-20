using System;

namespace Nonatomic.TimerKit
{
	/// <summary>
	/// Defines a contract for basic timer functionality without milestone support.
	/// </summary>
	public interface IBasicTimer : IReadOnlyTimer
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
	}
}