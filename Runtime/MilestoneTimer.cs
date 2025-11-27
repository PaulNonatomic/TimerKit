using System;
using System.Collections.Generic;

namespace Nonatomic.TimerKit
{
	/// <summary>
	/// A timer that extends BasicTimer with milestone support.
	/// Provides the ability to trigger callbacks at specific time points or intervals.
	/// </summary>
	public class MilestoneTimer : BasicTimer, ITimer
	{
		/// <summary>
		/// Gets the time remaining. During callback execution, returns the interval value that triggered the callback.
		/// Otherwise, returns the current timer's time remaining.
		/// </summary>
		public override float TimeRemaining => _callbackTimeOverride ?? base.TimeRemaining;

		/// <summary>
		/// Gets the time elapsed. During callback execution, calculated from the overridden TimeRemaining.
		/// </summary>
		public override float TimeElapsed => Duration - TimeRemaining;

		/// <summary>
		/// Gets the progress elapsed as a value between 0 and 1.
		/// </summary>
		public override float ProgressElapsed => TimeElapsed / Duration;

		/// <summary>
		/// Gets the progress remaining as a value between 0 and 1.
		/// </summary>
		public override float ProgressRemaining => 1 - ProgressElapsed;

		/// <summary>
		/// Initializes a new instance of the MilestoneTimer class with a specified duration.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		/// <param name="timeSource">Optional custom time source. If null, uses internal time management.</param>
		/// <param name="preserveTimeSourceValue">If true and timeSource is provided, preserves the time source's current value.</param>
		public MilestoneTimer(float duration, ITimeSource timeSource = null, bool preserveTimeSourceValue = false)
			: base(duration, timeSource, preserveTimeSourceValue)
		{
			//...
		}

		/// <summary>
		/// Adds a milestone to the timer, which will trigger a specified action when the timer reaches a specific point.
		/// </summary>
		/// <param name="milestone">The milestone to add to the timer.</param>
		public virtual void AddMilestone(TimerMilestone milestone)
		{
			if (milestone == null) return;

			var milestoneId = Guid.NewGuid();
			_milestonesById[milestoneId] = milestone;
			AddMilestoneToTriggerValue(milestoneId, milestone.TriggerValue);
		}

		/// <summary>
		/// Adds a milestone to the timer by specifying its components.
		/// </summary>
		/// <param name="type">The time type (TimeRemaining, TimeElapsed, etc.)</param>
		/// <param name="triggerValue">The value at which to trigger the milestone</param>
		/// <param name="callback">The callback to execute when the milestone is reached</param>
		/// <param name="isRecurring">Whether this milestone should re-trigger every time the timer restarts</param>
		/// <returns>The created TimerMilestone</returns>
		public virtual TimerMilestone AddMilestone(TimeType type, float triggerValue, Action callback, bool isRecurring = false)
		{
			var milestone = new TimerMilestone(type, triggerValue, callback, isRecurring);
			AddMilestone(milestone);
			return milestone;
		}

		/// <summary>
		/// Adds a range milestone to the timer.
		/// </summary>
		/// <param name="rangeMilestone">The range milestone to add.</param>
		public virtual void AddRangeMilestone(TimerRangeMilestone rangeMilestone)
		{
			AddMilestone(rangeMilestone);
		}

		/// <summary>
		/// Adds a range milestone that triggers at regular intervals within a specified range.
		/// </summary>
		/// <param name="type">The time type (TimeRemaining, TimeElapsed, etc.)</param>
		/// <param name="rangeStart">The start of the range (higher value for TimeRemaining)</param>
		/// <param name="rangeEnd">The end of the range (lower value for TimeRemaining)</param>
		/// <param name="interval">The interval at which to trigger callbacks</param>
		/// <param name="callback">The callback to execute at each interval</param>
		/// <returns">The created TimerRangeMilestone</returns>
		public virtual TimerRangeMilestone AddRangeMilestone(TimeType type, float rangeStart, float rangeEnd, float interval, Action callback, bool isRecurring = false)
		{
			var rangeMilestone = new TimerRangeMilestone(type, rangeStart, rangeEnd, interval, callback, isRecurring);
			AddRangeMilestone(rangeMilestone);
			return rangeMilestone;
		}

		/// <summary>
		/// Removes a specific milestone from the timer.
		/// </summary>
		/// <param name="milestone">The milestone to remove.</param>
		public virtual void RemoveMilestone(TimerMilestone milestone)
		{
			if (milestone == null) return;

			var milestoneId = FindMilestoneId(milestone);
			if (!milestoneId.HasValue) return;

			RemoveMilestoneById(milestoneId.Value, milestone.TriggerValue);
		}

		/// <summary>
		/// Removes all milestones from the timer.
		/// </summary>
		public virtual void RemoveAllMilestones()
		{
			_milestonesById.Clear();
			_milestonesByTriggerValue.Clear();
		}

		/// <summary>
		/// Removes milestones that meet a specific condition.
		/// </summary>
		/// <param name="condition">The condition to evaluate for each milestone.</param>
		public virtual void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition)
		{
			if (condition == null) return;

			var milestonesToRemove = FindMilestonesMatchingCondition(condition);
			RemoveMilestonesByIds(milestonesToRemove);
		}

		/// <summary>
		/// Called after the timer time is updated but before completion check.
		/// Processes milestone triggers.
		/// </summary>
		protected override void OnTimerUpdated()
		{
			CheckAndTriggerMilestones();
		}

		/// <summary>
		/// Called when the timer is reset.
		/// Resets all range milestones and re-adds recurring regular milestones to their initial state.
		/// </summary>
		protected override void OnTimerReset()
		{
			ResetRangeMilestones();
			ResetRecurringRegularMilestones();
		}

		private bool _processingMilestones;
		private Dictionary<Guid, TimerMilestone> _milestonesById = new();
		private SortedList<float, List<Guid>> _milestonesByTriggerValue = new();
		private float? _callbackTimeOverride;

		// Pooled collections to avoid allocations during milestone processing
		private HashSet<(float triggerValue, Guid id)> _alreadyTriggeredMilestones = new();
		private List<(Guid id, TimerRangeMilestone milestone)> _rangeMilestonesToReAdd = new();

		// Pooled collections for CollectMilestonesForProcessing
		private List<(TimerMilestone milestone, float? intervalValue)> _milestonesToTrigger = new();
		private List<Guid> _exhaustedMilestoneIds = new();
		private List<(Guid id, TimerRangeMilestone milestone)> _recurringMilestones = new();
		private List<Guid> _recurringRegularMilestoneIds = new();
		private HashSet<Guid> _processedIds = new();

		// Pooled collection for CalculateCrossedIntervals
		private List<float> _crossedIntervals = new();

		private void CheckAndTriggerMilestones()
		{
			if (_processingMilestones) return;
			// Only trigger milestones when the timer is actively running
			if (!IsRunning) return;
			// Early exit if no milestones exist
			if (_milestonesByTriggerValue.Count == 0) return;

			_processingMilestones = true;

			try
			{
				ProcessAllTriggeredMilestones();
			}
			finally
			{
				_processingMilestones = false;
			}
		}

		private void ProcessAllTriggeredMilestones()
		{
			// Clear pooled collections instead of allocating new ones
			_alreadyTriggeredMilestones.Clear();
			_rangeMilestonesToReAdd.Clear();

			while (TryProcessNextMilestone(_alreadyTriggeredMilestones, _rangeMilestonesToReAdd))
			{
				// Continue processing until no more milestones to trigger
			}

			ReAddRangeMilestones(_rangeMilestonesToReAdd);
		}

		private bool TryProcessNextMilestone(HashSet<(float triggerValue, Guid id)> alreadyTriggeredMilestones, List<(Guid id, TimerRangeMilestone milestone)> rangeMilestonesToReAdd)
		{
			if (!TryFindNextMilestoneToProcess(alreadyTriggeredMilestones, out var triggerValue, out var milestoneId))
				return false;

			MarkMilestonesAsProcessed(triggerValue, alreadyTriggeredMilestones);
			ProcessMilestonesAtTriggerValue(triggerValue, rangeMilestonesToReAdd);
			return true;
		}

		private bool TryFindNextMilestoneToProcess(HashSet<(float, Guid)> alreadyTriggeredMilestones, out float triggerValue, out Guid milestoneId)
		{
			triggerValue = default;
			milestoneId = default;
			bool found = false;
			float lowestUnprocessedTriggerValue = float.MaxValue;

			// Use index-based iteration to avoid SortedList.GetEnumerator() allocation
			var keys = _milestonesByTriggerValue.Keys;
			var values = _milestonesByTriggerValue.Values;
			var count = _milestonesByTriggerValue.Count;

			for (int i = 0; i < count; i++)
			{
				var currentTriggerValue = keys[i];
				var milestoneIds = values[i];

				if (!TryFindTriggeredMilestoneInList(currentTriggerValue, milestoneIds, alreadyTriggeredMilestones, out var foundMilestoneId))
					continue;

				if (currentTriggerValue >= lowestUnprocessedTriggerValue)
					continue;

				lowestUnprocessedTriggerValue = currentTriggerValue;
				triggerValue = currentTriggerValue;
				milestoneId = foundMilestoneId;
				found = true;
			}

			return found;
		}

		private bool TryFindTriggeredMilestoneInList(float triggerValue, List<Guid> milestoneIds, HashSet<(float, Guid)> alreadyTriggeredMilestones, out Guid milestoneId)
		{
			// Use index-based iteration to avoid List enumerator allocation
			var count = milestoneIds.Count;
			for (int i = 0; i < count; i++)
			{
				var id = milestoneIds[i];
				if (alreadyTriggeredMilestones.Contains((triggerValue, id))) continue;
				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;
				if (!ShouldTrigger(milestone)) continue;

				milestoneId = id;
				return true;
			}

			milestoneId = default;
			return false;
		}

		private void MarkMilestonesAsProcessed(float triggerValue, HashSet<(float, Guid)> alreadyTriggeredMilestones)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var idsToProcess)) return;

			// Use index-based iteration to avoid List enumerator allocation
			// No need for .ToList() since we're only reading, not modifying
			var count = idsToProcess.Count;
			for (int i = 0; i < count; i++)
			{
				alreadyTriggeredMilestones.Add((triggerValue, idsToProcess[i]));
			}
		}

		private void ProcessMilestonesAtTriggerValue(float triggerValue, List<(Guid id, TimerRangeMilestone milestone)> rangeMilestonesToReAdd)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var triggerIds) || triggerIds.Count == 0) return;

			// CollectMilestonesForProcessing now populates pooled class-level collections
			CollectMilestonesForProcessing(triggerIds, triggerValue);

			RemoveMilestonesFromTriggerValue(triggerValue);
			RemoveExhaustedMilestones(_exhaustedMilestoneIds);

			// Use index-based iteration to avoid List enumerator allocation
			var recurringCount = _recurringMilestones.Count;
			for (int i = 0; i < recurringCount; i++)
			{
				rangeMilestonesToReAdd.Add(_recurringMilestones[i]);
			}

			InvokeMilestoneCallbacks(_milestonesToTrigger);
		}

		private void CollectMilestonesForProcessing(List<Guid> triggerIds, float triggerValue)
		{
			// Clear pooled collections instead of allocating new ones
			_milestonesToTrigger.Clear();
			_exhaustedMilestoneIds.Clear();
			_recurringMilestones.Clear();
			_recurringRegularMilestoneIds.Clear();
			_processedIds.Clear();

			// Use index-based iteration to avoid List enumerator allocation
			var count = triggerIds.Count;
			for (int i = 0; i < count; i++)
			{
				var id = triggerIds[i];

				// Skip duplicates within the same trigger value
				if (!_processedIds.Add(id)) continue;

				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;

				if (milestone is not TimerRangeMilestone rangeMilestone)
				{
					_milestonesToTrigger.Add((milestone, null));
					// Only mark as exhausted if not recurring
					if (!milestone.IsRecurring)
					{
						_exhaustedMilestoneIds.Add(id);
					}
					else
					{
						// Recurring regular milestones need to be re-added
						_recurringRegularMilestoneIds.Add(id);
					}
					continue;
				}

				// Calculate all crossed intervals for range milestones
				CalculateCrossedIntervals(rangeMilestone);

				// Add a callback invocation for each crossed interval
				var crossedCount = _crossedIntervals.Count;
				for (int j = 0; j < crossedCount; j++)
				{
					_milestonesToTrigger.Add((rangeMilestone, _crossedIntervals[j]));
				}

				// Update to the last crossed value
				if (crossedCount > 0)
				{
					rangeMilestone.LastTriggeredValue = _crossedIntervals[crossedCount - 1];
				}

				// Check if more intervals remain
				if (rangeMilestone.HasMoreIntervals())
				{
					_recurringMilestones.Add((id, rangeMilestone));
				}
				else if (!rangeMilestone.IsRecurring)
				{
					// Only remove if not recurring
					_exhaustedMilestoneIds.Add(id);
				}
			}
		}

		private void CalculateCrossedIntervals(TimerRangeMilestone rangeMilestone)
		{
			// Clear pooled collection instead of allocating a new one
			_crossedIntervals.Clear();

			var currentValue = GetCurrentValueForTimeType(rangeMilestone.Type);
			var lastValue = rangeMilestone.LastTriggeredValue ?? rangeMilestone.RangeStart;
			var interval = rangeMilestone.Interval;
			var rangeStart = rangeMilestone.RangeStart;
			var rangeEnd = rangeMilestone.RangeEnd;

			if (rangeMilestone.Type == TimeType.TimeRemaining || rangeMilestone.Type == TimeType.ProgressRemaining)
			{
				// For decreasing types, we move from higher to lower values
				var nextTrigger = lastValue;
				if (rangeMilestone.LastTriggeredValue != null)
				{
					nextTrigger = lastValue - interval;
				}

				while (nextTrigger >= rangeEnd && nextTrigger >= currentValue && nextTrigger <= Duration)
				{
					_crossedIntervals.Add(nextTrigger);
					nextTrigger -= interval;
				}
			}
			else
			{
				// For increasing types, we move from lower to higher values
				var nextTrigger = lastValue;
				if (rangeMilestone.LastTriggeredValue != null)
				{
					nextTrigger = lastValue + interval;
				}

				while (nextTrigger <= rangeEnd && nextTrigger <= currentValue && nextTrigger <= Duration)
				{
					_crossedIntervals.Add(nextTrigger);
					nextTrigger += interval;
				}
			}
		}

		private float GetCurrentValueForTimeType(TimeType type)
		{
			return type switch
			{
				TimeType.TimeRemaining => TimeRemaining,
				TimeType.TimeElapsed => TimeElapsed,
				TimeType.ProgressElapsed => ProgressElapsed,
				TimeType.ProgressRemaining => ProgressRemaining,
				_ => throw new ArgumentOutOfRangeException(nameof(type), "Unknown TimeType")
			};
		}

		private void RemoveMilestonesFromTriggerValue(float triggerValue)
		{
			_milestonesByTriggerValue.Remove(triggerValue);
		}

		private void RemoveExhaustedMilestones(List<Guid> exhaustedMilestoneIds)
		{
			// Use index-based iteration to avoid List enumerator allocation
			var count = exhaustedMilestoneIds.Count;
			for (int i = 0; i < count; i++)
			{
				_milestonesById.Remove(exhaustedMilestoneIds[i]);
			}
		}

		private void ReAddRangeMilestones(List<(Guid id, TimerRangeMilestone milestone)> recurringMilestones)
		{
			// Use index-based iteration to avoid List enumerator allocation
			var count = recurringMilestones.Count;
			for (int i = 0; i < count; i++)
			{
				var (id, rangeMilestone) = recurringMilestones[i];
				rangeMilestone.UpdateTriggerValue();
				AddMilestoneToTriggerValue(id, rangeMilestone.TriggerValue);
			}
		}

		private void ReAddRecurringRegularMilestones(float triggerValue, List<Guid> recurringRegularMilestoneIds)
		{
			// Use index-based iteration to avoid List enumerator allocation
			var count = recurringRegularMilestoneIds.Count;
			for (int i = 0; i < count; i++)
			{
				AddMilestoneToTriggerValue(recurringRegularMilestoneIds[i], triggerValue);
			}
		}

		private void InvokeMilestoneCallbacks(List<(TimerMilestone milestone, float? intervalValue)> milestonesToTrigger)
		{
			// Use index-based iteration to avoid List enumerator allocation
			var count = milestonesToTrigger.Count;
			for (int i = 0; i < count; i++)
			{
				var (milestone, intervalValue) = milestonesToTrigger[i];
				try
				{
					// Temporarily override timer values so callbacks see the exact interval that triggered them
					_callbackTimeOverride = intervalValue;
					milestone.Callback?.Invoke();
				}
				finally
				{
					// Always clear the override to restore normal timer state
					_callbackTimeOverride = null;
				}
			}
		}

		private bool ShouldTrigger(TimerMilestone milestone)
		{
			return milestone.Type switch
			{
				TimeType.TimeRemaining => TimeRemaining <= milestone.TriggerValue && milestone.TriggerValue <= Duration,
				TimeType.TimeElapsed => TimeElapsed >= milestone.TriggerValue && milestone.TriggerValue <= Duration,
				TimeType.ProgressElapsed => ProgressElapsed >= milestone.TriggerValue,
				TimeType.ProgressRemaining => ProgressRemaining <= milestone.TriggerValue,
				_ => throw new ArgumentOutOfRangeException(nameof(milestone.Type), "Unknown TimeType")
			};
		}

		private void AddMilestoneToTriggerValue(Guid id, float triggerValue)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var milestoneList))
			{
				milestoneList = new List<Guid>();
				_milestonesByTriggerValue[triggerValue] = milestoneList;
			}

			// Don't add duplicates - prevents recurring milestones from being added multiple times
			if (!milestoneList.Contains(id))
			{
				milestoneList.Add(id);
			}
		}

		private Guid? FindMilestoneId(TimerMilestone milestone)
		{
			foreach (var pair in _milestonesById)
			{
				if (pair.Value == milestone)
				{
					return pair.Key;
				}
			}

			return null;
		}

		private void RemoveMilestoneById(Guid milestoneId, float triggerValue)
		{
			_milestonesById.Remove(milestoneId);
			RemoveMilestoneFromTriggerValue(milestoneId, triggerValue);
		}

		private void RemoveMilestoneFromTriggerValue(Guid milestoneId, float triggerValue)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var list)) return;

			list.Remove(milestoneId);

			if (list.Count == 0)
			{
				_milestonesByTriggerValue.Remove(triggerValue);
			}
		}

		private List<Guid> FindMilestonesMatchingCondition(Predicate<TimerMilestone> condition)
		{
			var milestonesToRemove = new List<Guid>();

			foreach (var pair in _milestonesById)
			{
				if (!condition(pair.Value)) continue;
				milestonesToRemove.Add(pair.Key);
			}

			return milestonesToRemove;
		}

		private void RemoveMilestonesByIds(List<Guid> milestoneIds)
		{
			// Use index-based iteration to avoid List enumerator allocation
			var count = milestoneIds.Count;
			for (int i = 0; i < count; i++)
			{
				var id = milestoneIds[i];
				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;
				RemoveMilestoneById(id, milestone.TriggerValue);
			}
		}

		private void ResetRangeMilestones()
		{
			var rangeMilestonesToUpdate = CollectRangeMilestones();
			UpdateRangeMilestonePositions(rangeMilestonesToUpdate);
		}

		private void ResetRecurringRegularMilestones()
		{
			// Find all recurring regular (non-range) milestones
			foreach (var pair in _milestonesById)
			{
				if (pair.Value is TimerRangeMilestone) continue; // Skip range milestones
				if (!pair.Value.IsRecurring) continue; // Skip non-recurring milestones

				var milestoneId = pair.Key;
				var milestone = pair.Value;

				// Re-add to trigger value lookup if not already there
				AddMilestoneToTriggerValue(milestoneId, milestone.TriggerValue);
			}
		}

		private List<(Guid id, TimerRangeMilestone milestone, float oldTriggerValue)> CollectRangeMilestones()
		{
			var rangeMilestonesToUpdate = new List<(Guid id, TimerRangeMilestone milestone, float oldTriggerValue)>();

			foreach (var pair in _milestonesById)
			{
				if (pair.Value is not TimerRangeMilestone rangeMilestone) continue;

				var oldTriggerValue = rangeMilestone.TriggerValue;
				rangeMilestone.Reset();
				rangeMilestonesToUpdate.Add((pair.Key, rangeMilestone, oldTriggerValue));
			}

			return rangeMilestonesToUpdate;
		}

		private void UpdateRangeMilestonePositions(List<(Guid id, TimerRangeMilestone milestone, float oldTriggerValue)> rangeMilestonesToUpdate)
		{
			// Use index-based iteration to avoid List enumerator allocation
			var count = rangeMilestonesToUpdate.Count;
			for (int i = 0; i < count; i++)
			{
				var (id, rangeMilestone, oldTriggerValue) = rangeMilestonesToUpdate[i];
				RemoveMilestoneFromTriggerValue(id, oldTriggerValue);
				AddMilestoneToTriggerValue(id, rangeMilestone.TriggerValue);
			}
		}
	}
}
