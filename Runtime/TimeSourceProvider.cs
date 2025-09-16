using UnityEngine;

namespace Nonatomic.TimerKit
{
	/// <summary>
	/// Abstract base class for components that provide a time source to timers.
	/// Attach this to a GameObject with a component implementing ITimer to automatically set the time source.
	/// </summary>
	public abstract class TimeSourceProvider : MonoBehaviour, ITimeSource
	{
		private ITimer _timer;
		private Timer _timerComponent;
		
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
			// Try to get an ITimer implementation on the same GameObject
			_timer = GetComponent<ITimer>();
			if (_timer == null)
			{
				Debug.LogError("TimeSourceProvider requires a component implementing ITimer on the same GameObject", this);
				return;
			}
			
			// If it's a Timer MonoBehaviour, we can set the time source
			_timerComponent = _timer as Timer;
			if (_timerComponent != null)
			{
				_timerComponent.SetTimeSource(this);
			}
			else
			{
				Debug.LogWarning("ITimer implementation doesn't support SetTimeSource. Time source provider will not be connected.", this);
			}
		}
		
		protected virtual void OnDestroy()
		{
			// Clear the time source when destroyed
			if (_timerComponent != null)
			{
				_timerComponent.SetTimeSource(null);
			}
		}
	}
}