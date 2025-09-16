using UnityEngine;

namespace Nonatomic.TimerKit
{
	/// <summary>
	/// Abstract base class for components that provide a time source to timers.
	/// Attach this to a GameObject with a Timer component to automatically set the time source.
	/// </summary>
	[RequireComponent(typeof(Timer))]
	public abstract class TimeSourceProvider : MonoBehaviour, ITimeSource
	{
		private Timer _timer;
		
		/// <summary>
		/// Gets whether this time source supports setting time.
		/// Override in derived classes to specify capability.
		/// </summary>
		public abstract bool CanSetTime { get; }
		
		/// <summary>
		/// Gets the current time remaining from the time source.
		/// Override in derived classes to provide time value.
		/// </summary>
		public abstract float GetTimeRemaining();
		
		/// <summary>
		/// Sets the time remaining in the time source.
		/// Override in derived classes to handle time setting.
		/// </summary>
		public abstract void SetTimeRemaining(float timeRemaining);
		
		protected virtual void Awake()
		{
			// Get the Timer component on the same GameObject
			_timer = GetComponent<Timer>();
			if (_timer == null)
			{
				Debug.LogError("TimeSourceProvider requires a Timer component on the same GameObject", this);
				return;
			}
			
			// Set ourselves as the time source
			_timer.SetTimeSource(this);
		}
		
		protected virtual void OnDestroy()
		{
			// Clear the time source when destroyed
			if (_timer != null)
			{
				_timer.SetTimeSource(null);
			}
		}
	}
}