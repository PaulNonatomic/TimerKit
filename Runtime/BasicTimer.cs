using System;
using UnityEngine;

namespace Nonatomic.TimerKit
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
		public event Action<float> OnDurationChanged;

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
				OnDurationChanged?.Invoke(_duration);
			}
		}
		
		public virtual float TimeRemaining 
		{ 
			get => _timeSource?.GetTimeRemaining() ?? _internalTimeRemaining;
			
			private set
			{
				if (_timeSource is { CanSetTime: true })
				{
					_timeSource.SetTimeRemaining(value);
				}
				else if (_timeSource == null)
				{
					_internalTimeRemaining = value;
				}
			}
		}
		public virtual float TimeElapsed => Duration - TimeRemaining;
		public virtual float ProgressElapsed => TimeElapsed / Duration;
		public virtual float ProgressRemaining => 1 - ProgressElapsed;
		public bool IsRunning { get; private set; }

		private float _duration;
		private float _internalTimeRemaining;
		private ITimeSource _timeSource;

		/// <summary>
		/// Initializes a new instance of the BasicTimer class with a specified duration.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		/// <param name="timeSource">Optional custom time source. If null, uses internal time management.</param>
		/// <param name="preserveTimeSourceValue">If true and timeSource is provided, preserves the time source's current value instead of resetting to duration.</param>
		public BasicTimer(float duration, ITimeSource timeSource = null, bool preserveTimeSourceValue = false)
		{
			_timeSource = timeSource;
			Duration = duration;
			
			if (preserveTimeSourceValue && timeSource != null)
			{
				IsRunning = false;
			}
			else
			{
				ResetTimer();
			}
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
			if (_timeSource == null || _timeSource.CanSetTime)
			{
				TimeRemaining = Duration;
			}
			
			IsRunning = true;
			OnStart?.Invoke();
		}
		
		/// <summary>
		/// Resumes the timer if possible, without resetting the remaining time.
		/// </summary>
		public virtual void ResumeTimer()
		{
			if (TimeRemaining <= 0) return;

			IsRunning = true;
			OnResume?.Invoke();
		}

		/// <summary>
		/// Updates the timer's countdown based on the delta time.
		/// </summary>
		/// <param name="deltaTime">The time in seconds to update the timer.</param>
		public virtual void Update(float deltaTime)
		{
			if (!IsRunning) return;

			var originalTimeRemaining = TimeRemaining;
			
			if (_timeSource != null && _timeSource.CanSetTime)
			{
				TimeRemaining -= deltaTime;
			}
			else if (_timeSource == null)
			{
				_internalTimeRemaining -= deltaTime;
			}
			
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
			//...
		}
		
		private bool HasCompleted(float originalTimeRemaining)
		{
			return originalTimeRemaining > 0 && TimeRemaining <= 0;
		}
		
		private void HandleCompletion()
		{
			IsRunning = false;
			TimeRemaining = 0;
			OnComplete?.Invoke();
		}

		/// <summary>
		/// Stops the timer.
		/// </summary>
		public virtual void StopTimer()
		{
			IsRunning = false;
			OnStop?.Invoke();
		}

		/// <summary>
		/// Resets the timer to the full duration.
		/// </summary>
		public virtual void ResetTimer()
		{
			if (_timeSource == null || _timeSource.CanSetTime)
			{
				TimeRemaining = Duration;
			}
			
			IsRunning = false;
			OnTimerReset();
		}
		
		/// <summary>
		/// Called when the timer is reset.
		/// Override this in derived classes to add functionality like resetting milestones.
		/// </summary>
		protected virtual void OnTimerReset()
		{
			//...
		}
		
		/// <summary>
		/// Advances the timer forward by a specified number of seconds.
		/// </summary>
		/// <param name="seconds">The number of seconds to fast forward.</param>
		public virtual void FastForward(float seconds)
		{
			if (seconds < 0) return;
			if (_timeSource != null && !_timeSource.CanSetTime) return; // Can't fast forward read-only time source
			
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
			if (_timeSource != null && !_timeSource.CanSetTime) return; // Can't rewind read-only time source
			
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
				TimeRemaining = _timeSource != null ? _timeSource.GetTimeRemaining() : _internalTimeRemaining,
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
			if (_timeSource != null && _timeSource.CanSetTime)
			{
				_timeSource.SetTimeRemaining(state.TimeRemaining);
			}
			else if (_timeSource == null)
			{
				_internalTimeRemaining = state.TimeRemaining;
			}
			IsRunning = state.IsRunning;

			if (IsRunning && TimeRemaining <= 0)
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