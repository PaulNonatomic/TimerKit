using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timers.Runtime
{
	/// <summary>
	/// Implements a basic countdown timer with start, stop, reset, fast forward, and rewind capabilities,
	/// as well as the ability to set milestones that trigger events at specific times or progress points.
	/// </summary>
	public class SimpleTimer : ITimer
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
					// Adjust TimeRemaining only if it exceeds the new duration
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
		private List<TimerMilestone> _milestones = new List<TimerMilestone>();
		
		/// <summary>
		/// Initializes a new instance of the SimpleTimer class with a specified duration.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		public SimpleTimer(float duration)
		{
			Duration = duration;
		}
		
		/// <summary>
		/// Gets the time as either TimeRemaining, TimeElapsed, ProgressElapsed, ProgressRemaining
		/// </summary>
		public virtual float TimeByType(TimeType type)
		{
			switch (type)
			{
				case TimeType.TimeElapsed: return TimeElapsed;
				case TimeType.ProgressElapsed: return ProgressElapsed;
				case TimeType.ProgressRemaining: return ProgressRemaining;
				case TimeType.TimeRemaining:
				default: return TimeRemaining;
			}
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
		public void ResumeTimer()
		{
			if (TimeRemaining <= 0) return;

			_isRunning = true;
			OnResume?.Invoke();
		}

		/// <summary>
		/// Updates the timer's countdown based on the delta time and checks for any milestone triggers.
		/// </summary>
		/// <param name="deltaTime">The time in seconds to update the timer.</param>
		public virtual void Update(float deltaTime)
		{
			if (!_isRunning) return;

			TimeRemaining -= deltaTime;
			CheckAndTriggerMilestones();
			OnTick?.Invoke(this);
			
			if (!(TimeRemaining <= 0)) return;
			
			_isRunning = false;
			TimeRemaining = 0;
			OnComplete?.Invoke();
		}

		protected virtual void CheckAndTriggerMilestones()
		{
			_milestones.RemoveAll(milestone =>
			{
				var shouldTrigger = milestone.Type switch
				{
					TimeType.TimeRemaining => TimeRemaining <= milestone.TriggerValue,
					TimeType.TimeElapsed => TimeElapsed >= milestone.TriggerValue,
					TimeType.ProgressElapsed => ProgressElapsed >= milestone.TriggerValue,
					TimeType.ProgressRemaining => ProgressRemaining <= milestone.TriggerValue,
					_ => throw new ArgumentOutOfRangeException()
				};

				if (!shouldTrigger) return false;
				
				milestone.Callback?.Invoke();
				return true;  // Mark for removal
			});
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
		}
		
		/// <summary>
		/// Advances the timer forward by a specified number of seconds.
		/// </summary>
		/// <param name="seconds">The number of seconds to fast forward.</param>
		public virtual void FastForward(float seconds)
		{
			TimeRemaining -= seconds;
			if (TimeRemaining <= 0)
			{
				TimeRemaining = 0;
				_isRunning = false;
				OnComplete?.Invoke();
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
			TimeRemaining += seconds;
			if (TimeRemaining > Duration)
			{
				TimeRemaining = Duration;
			}
			
			OnTick?.Invoke(this);
		}
		
		/// <summary>
		/// Adds a milestone to the timer, which will trigger a specified action when the timer reaches a specific point.
		/// </summary>
		/// <param name="milestone">The milestone to add to the timer.</param>
		public virtual void AddMilestone(TimerMilestone milestone)
		{
			_milestones.Add(milestone);
		}
		
		public virtual void RemoveMilestone(TimerMilestone milestone)
		{
			_milestones.Remove(milestone);
		}
		
		public virtual void ClearMilestones()
		{
			_milestones.Clear();
		}

		public virtual void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition)
		{
			_milestones.RemoveAll(condition);
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
		public void Deserialize(string json)
		{
			var state = JsonUtility.FromJson<TimerState>(json);
			if (state == null) throw new InvalidOperationException("Failed to deserialize timer state.");

			Duration = state.Duration;
			TimeRemaining = state.TimeRemaining;
			_isRunning = state.IsRunning;

			// Ensure the timer state is consistent after deserialization
			if (_isRunning && TimeRemaining <= 0)
			{
				StopTimer(); // Stop the timer if the remaining time is zero or less
			}
		}

		// Internal class to hold the state of the timer for serialization
		[Serializable]
		private class TimerState
		{
			public float Duration;
			public float TimeRemaining;
			public bool IsRunning;
		}
	}
}