namespace Nonatomic.TimerKit
{
	/// <summary>
	/// Interface for providing time values to timers.
	/// Allows for custom time sources such as session timers, network-synchronized time, or testing scenarios.
	/// </summary>
	public interface ITimeSource
	{
		/// <summary>
		/// Gets the current time remaining from the time source.
		/// </summary>
		float GetTimeRemaining();
		
		/// <summary>
		/// Sets the time remaining in the time source.
		/// </summary>
		void SetTimeRemaining(float timeRemaining);
		
		/// <summary>
		/// Gets whether the time source supports setting time.
		/// Some time sources (like external session timers) may be read-only.
		/// </summary>
		bool CanSetTime { get; }
	}
}