using System;
using UnityEngine;

namespace Nonatomic.Timers
{
	/// <summary>
	/// A basic countdown timer implementation without milestone support.
	/// Provides core timer functionality: start, stop, reset, fast forward, rewind, and time tracking.
	/// </summary>
	public class BasicTimer : IBasicTimer
	{
		public event Action OnStart;
		public event Action OnResume;
		public event Action OnStop;
		public event Action OnComplete;
		public event Action<IReadOnlyTimer> OnTick;

		public float Duration
		{
			get => _duration;
			set
			{
				_duration = value;
				if (IsRunning)
				{
					TimeRemaining = Math.Min(TimeRemaining, _duration);
				}
			}
		}
		
		public float TimeRemaining { get; private set; }
		public float TimeElapsed => Duration - TimeRemaining;
		public float ProgressElapsed => TimeElapsed / Duration;
		public float ProgressRemaining => 1 - ProgressElapsed;
		public bool IsRunning => _isRunning;
		
		private float _duration;
		private bool _isRunning;

		/// <summary>
		/// Initializes a new instance of the BasicTimer class with a specified duration.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		public BasicTimer(float duration)
		{
			Duration = duration;
			ResetTimer();
		}
		
		/// <summary>
		/// Gets the time as either TimeRemaining, TimeElapsed, ProgressElapsed, ProgressRemaining
		/// </summary>
		public virtual float TimeByType(TimeType type)
		{
			return type switch
			{
				TimeType.TimeElapsed => TimeElapsed,
				TimeType.ProgressElapsed => ProgressElapsed,
				TimeType.ProgressRemaining => ProgressRemaining,
				TimeType.TimeRemaining => TimeRemaining,
				_ => TimeRemaining
			};
		}

		/// <summary>
		/// Starts the timer, resetting the remaining time to the full duration.
		/// </summary>
		public virtual void StartTimer()
		{
			TimeRemaining = Duration;
			_isRunning = true;
			OnStart?.Invoke();
		}
		
		/// <summary>
		/// Resumes the timer if possible, without resetting the remaining time.
		/// </summary>
		public virtual void ResumeTimer()
		{
			if (TimeRemaining <= 0) return;

			_isRunning = true;
			OnResume?.Invoke();
		}

		/// <summary>
		/// Updates the timer's countdown based on the delta time.
		/// </summary>
		/// <param name="deltaTime">The time in seconds to update the timer.</param>
		public virtual void Update(float deltaTime)
		{
			if (!_isRunning) return;

			var originalTimeRemaining = TimeRemaining;
			TimeRemaining -= deltaTime;
			
			OnTimerUpdated();
			OnTick?.Invoke(this);
			
			if (HasCompleted(originalTimeRemaining))
			{
				HandleCompletion();
			}
		}
		
		/// <summary>
		/// Called after the timer time is updated but before completion check.
		/// Override this in derived classes to add functionality like milestone processing.
		/// </summary>
		protected virtual void OnTimerUpdated()
		{
			// Base implementation does nothing - override in derived classes
		}
		
		private bool HasCompleted(float originalTimeRemaining)
		{
			return originalTimeRemaining > 0 && TimeRemaining <= 0;
		}
		
		private void HandleCompletion()
		{
			_isRunning = false;
			TimeRemaining = 0;
			OnComplete?.Invoke();
		}

		/// <summary>
		/// Stops the timer.
		/// </summary>
		public virtual void StopTimer()
		{
			_isRunning = false;
			OnStop?.Invoke();
		}

		/// <summary>
		/// Resets the timer to the full duration.
		/// </summary>
		public virtual void ResetTimer()
		{
			TimeRemaining = Duration;
			_isRunning = false;
			OnTimerReset();
		}
		
		/// <summary>
		/// Called when the timer is reset.
		/// Override this in derived classes to add functionality like resetting milestones.
		/// </summary>
		protected virtual void OnTimerReset()
		{
			// Base implementation does nothing - override in derived classes
		}
		
		/// <summary>
		/// Advances the timer forward by a specified number of seconds.
		/// </summary>
		/// <param name="seconds">The number of seconds to fast forward.</param>
		public virtual void FastForward(float seconds)
		{
			if (seconds < 0) return;
			
			float originalTimeRemaining = TimeRemaining;
			TimeRemaining -= seconds;
			
			OnTimerUpdated();
			
			if (HasCompleted(originalTimeRemaining))
			{
				HandleCompletion();
			}
			else
			{
				OnTick?.Invoke(this);
			}
		}

		/// <summary>
		/// Rewinds the timer backward by a specified number of seconds.
		/// </summary>
		/// <param name="seconds">The number of seconds to rewind.</param>
		public virtual void Rewind(float seconds)
		{
			if (seconds < 0) return;
			
			TimeRemaining = Math.Min(TimeRemaining + seconds, Duration);
			OnTick?.Invoke(this);
		}
		
		/// <summary>
		/// Serializes the current state of the timer into a JSON string.
		/// </summary>
		/// <returns>A JSON string representing the timer's state.</returns>
		public virtual string Serialize()
		{
			var state = new TimerState
			{
				Duration = Duration,
				TimeRemaining = TimeRemaining,
				IsRunning = IsRunning
			};
			
			return JsonUtility.ToJson(state);
		}

		/// <summary>
		/// Deserializes the given JSON string to restore the state of the timer.
		/// </summary>
		/// <param name="json">A JSON string representing the timer's state.</param>
		public virtual void Deserialize(string json)
		{
			var state = JsonUtility.FromJson<TimerState>(json);
			if (state == null) throw new InvalidOperationException("Failed to deserialize timer state.");

			Duration = state.Duration;
			TimeRemaining = state.TimeRemaining;
			_isRunning = state.IsRunning;

			if (_isRunning && TimeRemaining <= 0)
			{
				StopTimer();
			}
		}

		[Serializable]
		private class TimerState
		{
			public float Duration;
			public float TimeRemaining;
			public bool IsRunning;
		}
	}
}