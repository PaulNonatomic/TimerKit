namespace Nonatomic.TimerKit
{
	/// <summary>
	/// Default time source implementation that manages time internally.
	/// This is the standard time source used when no custom source is provided.
	/// </summary>
	public class DefaultTimeSource : ITimeSource
	{
		private float _timeRemaining;
		
		/// <summary>
		/// Gets whether this time source supports setting time.
		/// DefaultTimeSource always allows setting time.
		/// </summary>
		public bool CanSetTime => true;
		
		/// <summary>
		/// Initializes a new instance of the DefaultTimeSource class.
		/// </summary>
		/// <param name="initialTime">The initial time value.</param>
		public DefaultTimeSource(float initialTime = 0f)
		{
			_timeRemaining = initialTime;
		}
		
		/// <summary>
		/// Gets the current time remaining.
		/// </summary>
		public float GetTimeRemaining()
		{
			return _timeRemaining;
		}
		
		/// <summary>
		/// Sets the time remaining.
		/// </summary>
		public void SetTimeRemaining(float timeRemaining)
		{
			_timeRemaining = timeRemaining;
		}
		
		/// <summary>
		/// Updates the time source by subtracting delta time.
		/// </summary>
		/// <param name="deltaTime">The time to subtract.</param>
		public void UpdateTime(float deltaTime)
		{
			_timeRemaining -= deltaTime;
		}
	}
}