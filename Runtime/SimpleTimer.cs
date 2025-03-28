using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.Timers
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
		private bool _processingMilestones;
		private Dictionary<Guid, TimerMilestone> _milestonesById = new Dictionary<Guid, TimerMilestone>();
		private SortedList<float, List<Guid>> _milestonesByTriggerValue = new SortedList<float, List<Guid>>();
		
		/// <summary>
		/// Initializes a new instance of the SimpleTimer class with a specified duration.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		public SimpleTimer(float duration)
		{
			Duration = duration;
			ResetTimer();
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

			// Store the original time remaining to check if we've reached zero
			var originalTimeRemaining = TimeRemaining;
			
			// Update the time remaining
			TimeRemaining -= deltaTime;
			
			CheckAndTriggerMilestones();
			OnTick?.Invoke(this);
			
			// Handle completion if we've reached zero
			if (!(originalTimeRemaining > 0) || !(TimeRemaining <= 0)) return;
			
			_isRunning = false;
			TimeRemaining = 0;
			OnComplete?.Invoke();
		}

		protected virtual void CheckAndTriggerMilestones()
		{
			if (_processingMilestones) return;
			_processingMilestones = true;

			try
			{
				// Process milestones in order of their trigger values
				var processedTriggerValues = new HashSet<float>();
				
				while (true)
				{
					float? nextTriggerValue = null;
					
					foreach (var key in _milestonesByTriggerValue.Keys)
					{
						if (processedTriggerValues.Contains(key)) continue;
						
						// Get a sample milestone to check if we should process this trigger value
						var sampleId = _milestonesByTriggerValue[key][0];
						var sampleMilestone = _milestonesById[sampleId];
						
						if (!ShouldTrigger(sampleMilestone)) continue;
						
						if (nextTriggerValue == null || key < nextTriggerValue.Value)
						{
							nextTriggerValue = key;
						}
					}
					
					if (nextTriggerValue == null) break;
					
					// Mark this trigger value as processed
					processedTriggerValues.Add(nextTriggerValue.Value);
					ProcessMilestonesAtTriggerValue(nextTriggerValue.Value);
				}
			}
			finally
			{
				_processingMilestones = false;
			}
		}

		private void ProcessMilestonesAtTriggerValue(float triggerValue)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var triggerIds) || triggerIds.Count == 0) return;
			
			var milestonesToTrigger = new List<TimerMilestone>(triggerIds.Count);
			var idsToRemove = new List<Guid>(triggerIds);
			
			// Collect valid milestones
			foreach (var id in idsToRemove)
			{
				if (_milestonesById.TryGetValue(id, out var milestone))
				{
					milestonesToTrigger.Add(milestone);
				}
			}
			
			// Remove all milestones for this trigger value from collections
			_milestonesByTriggerValue.Remove(triggerValue);
			
			foreach (var id in idsToRemove)
			{
				_milestonesById.Remove(id);
			}
			
			foreach (var milestone in milestonesToTrigger)
			{
				milestone.Callback?.Invoke();
			}
		}

		private bool ShouldTrigger(TimerMilestone milestone)
		{
			return milestone.Type switch
			{
				TimeType.TimeRemaining => TimeRemaining <= milestone.TriggerValue,
				TimeType.TimeElapsed => TimeElapsed >= milestone.TriggerValue,
				TimeType.ProgressElapsed => ProgressElapsed >= milestone.TriggerValue,
				TimeType.ProgressRemaining => ProgressRemaining <= milestone.TriggerValue,
				_ => throw new ArgumentOutOfRangeException(nameof(milestone.Type), "Unknown TimeType")
			};
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
			if (seconds < 0) return; // Ignore negative values
			
			// Store original time remaining to check for completion
			float originalTimeRemaining = TimeRemaining;
			
			// Update time remaining
			TimeRemaining -= seconds;
			
			// Check and trigger milestones based on the new time
			CheckAndTriggerMilestones();
			
			// Handle completion if we've reached zero
			if (originalTimeRemaining > 0 && TimeRemaining <= 0)
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
			if (seconds < 0) return; // Ignore negative values
			
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
			if (milestone == null) return;
			
			// Use a combination of trigger value and a unique part to ensure key uniqueness
			// This prevents issues in the original code where milestones with the same trigger value
			// would overwrite each other in the SortedList
			var milestoneId = Guid.NewGuid();
			_milestonesById[milestoneId] = milestone;
			
			if (!_milestonesByTriggerValue.TryGetValue(milestone.TriggerValue, out var milestoneList))
			{
				milestoneList = new List<Guid>();
				_milestonesByTriggerValue[milestone.TriggerValue] = milestoneList;
			}
			
			milestoneList.Add(milestoneId);
		}
		
		public virtual void RemoveMilestone(TimerMilestone milestone)
		{
			if (milestone == null) return;
			
			// Find the milestone ID
			Guid? milestoneIdToRemove = null;
			foreach (var pair in _milestonesById)
			{
				if (pair.Value == milestone)
				{
					milestoneIdToRemove = pair.Key;
					break;
				}
			}
			
			if (!milestoneIdToRemove.HasValue) return;
			
			// Remove from both collections
			_milestonesById.Remove(milestoneIdToRemove.Value);
			
			// Find and remove from trigger list
			if (_milestonesByTriggerValue.TryGetValue(milestone.TriggerValue, out var list))
			{
				list.Remove(milestoneIdToRemove.Value);
				if (list.Count == 0)
				{
					_milestonesByTriggerValue.Remove(milestone.TriggerValue);
				}
			}
		}

		public virtual void RemoveMilestones(TimerMilestone[] milestones)
		{
			foreach (var milestone in milestones)
			{
				RemoveMilestone(milestone);
			}
		}
		
		public virtual void RemoveAllMilestones()
		{
			_milestonesById.Clear();
			_milestonesByTriggerValue.Clear();
		}
		
		public virtual void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition)
		{
			if (condition == null) return;
			
			var milestonesToRemove = new List<Guid>();
			
			// Find all milestones matching the condition
			foreach (var pair in _milestonesById)
			{
				if (condition(pair.Value))
				{
					milestonesToRemove.Add(pair.Key);
				}
			}
			
			// Remove each milestone
			foreach (var id in milestonesToRemove)
			{
				if (_milestonesById.TryGetValue(id, out var milestone))
				{
					// First remove from the trigger list
					if (_milestonesByTriggerValue.TryGetValue(milestone.TriggerValue, out var list))
					{
						list.Remove(id);
						if (list.Count == 0)
						{
							_milestonesByTriggerValue.Remove(milestone.TriggerValue);
						}
					}
					
					// Then remove from the ID dictionary
					_milestonesById.Remove(id);
				}
			}
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