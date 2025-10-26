using System;
using System.Collections.Generic;
using System.Linq;

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

		private void CheckAndTriggerMilestones()
		{
			if (_processingMilestones) return;
			// Only trigger milestones when the timer is actively running
			if (!IsRunning) return;

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
			var alreadyTriggeredMilestones = new HashSet<(float triggerValue, Guid id)>();
			var rangeMilestonesToReAdd = new List<(Guid id, TimerRangeMilestone milestone)>();

			while (TryProcessNextMilestone(alreadyTriggeredMilestones, rangeMilestonesToReAdd))
			{
				// Continue processing until no more milestones to trigger
			}

			ReAddRangeMilestones(rangeMilestonesToReAdd);
		}

		private bool TryProcessNextMilestone(HashSet<(float triggerValue, Guid id)> alreadyTriggeredMilestones, List<(Guid id, TimerRangeMilestone milestone)> rangeMilestonesToReAdd)
		{
			var nextMilestone = FindNextMilestoneToProcess(alreadyTriggeredMilestones);
			if (!nextMilestone.HasValue) return false;

			MarkMilestonesAsProcessed(nextMilestone.Value.triggerValue, alreadyTriggeredMilestones);
			ProcessMilestonesAtTriggerValue(nextMilestone.Value.triggerValue, rangeMilestonesToReAdd);
			return true;
		}

		private (float triggerValue, Guid id)? FindNextMilestoneToProcess(HashSet<(float, Guid)> alreadyTriggeredMilestones)
		{
			float? lowestUnprocessedTriggerValue = null;
			Guid? correspondingMilestoneId = null;

			foreach (var kvp in _milestonesByTriggerValue)
			{
				var milestoneId = FindTriggeredMilestoneInList(kvp.Key, kvp.Value, alreadyTriggeredMilestones);
				
				if (!milestoneId.HasValue) continue;
				if (!ShouldUpdateLowestTriggerValue(kvp.Key, lowestUnprocessedTriggerValue)) continue;
				
				lowestUnprocessedTriggerValue = kvp.Key;
				correspondingMilestoneId = milestoneId.Value;
			}

			return CreateMilestoneResult(lowestUnprocessedTriggerValue, correspondingMilestoneId);
		}

		private bool ShouldUpdateLowestTriggerValue(float currentValue, float? lowestValue)
		{
			return lowestValue == null || currentValue < lowestValue.Value;
		}

		private (float triggerValue, Guid id)? CreateMilestoneResult(float? triggerValue, Guid? milestoneId)
		{
			if (triggerValue == null || milestoneId == null) return null;
			return (triggerValue.Value, milestoneId.Value);
		}

		private Guid? FindTriggeredMilestoneInList(float triggerValue, List<Guid> milestoneIds, HashSet<(float, Guid)> alreadyTriggeredMilestones)
		{
			foreach (var id in milestoneIds)
			{
				if (alreadyTriggeredMilestones.Contains((triggerValue, id))) continue;
				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;
				if (!ShouldTrigger(milestone)) continue;

				return id;
			}

			return null;
		}

		private void MarkMilestonesAsProcessed(float triggerValue, HashSet<(float, Guid)> alreadyTriggeredMilestones)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var idsToProcess)) return;

			foreach (var id in idsToProcess.ToList())
			{
				alreadyTriggeredMilestones.Add((triggerValue, id));
			}
		}

		private void ProcessMilestonesAtTriggerValue(float triggerValue, List<(Guid id, TimerRangeMilestone milestone)> rangeMilestonesToReAdd)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var triggerIds) || triggerIds.Count == 0) return;

			var collectedMilestones = CollectMilestonesForProcessing(triggerIds, triggerValue);

			RemoveMilestonesFromTriggerValue(triggerValue);
			RemoveExhaustedMilestones(collectedMilestones.exhaustedMilestoneIds);
			rangeMilestonesToReAdd.AddRange(collectedMilestones.recurringMilestones);
			
			InvokeMilestoneCallbacks(collectedMilestones.milestonesToTrigger);
		}

		private (List<(TimerMilestone milestone, float? intervalValue)> milestonesToTrigger, List<Guid> exhaustedMilestoneIds, List<(Guid id, TimerRangeMilestone milestone)> recurringMilestones, List<Guid> recurringRegularMilestoneIds)
			CollectMilestonesForProcessing(List<Guid> triggerIds, float triggerValue)
		{
			var milestonesToTrigger = new List<(TimerMilestone milestone, float? intervalValue)>();
			var exhaustedMilestoneIds = new List<Guid>();
			var recurringMilestones = new List<(Guid id, TimerRangeMilestone milestone)>();
			var recurringRegularMilestoneIds = new List<Guid>();
			var processedIds = new HashSet<Guid>();

			foreach (var id in triggerIds)
			{
				// Skip duplicates within the same trigger value
				if (!processedIds.Add(id)) continue;

				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;

				if (milestone is not TimerRangeMilestone rangeMilestone)
				{
					milestonesToTrigger.Add((milestone, null));
					// Only mark as exhausted if not recurring
					if (!milestone.IsRecurring)
					{
						exhaustedMilestoneIds.Add(id);
					}
					else
					{
						// Recurring regular milestones need to be re-added
						recurringRegularMilestoneIds.Add(id);
					}
					continue;
				}

				// Calculate all crossed intervals for range milestones
				var crossedIntervals = CalculateCrossedIntervals(rangeMilestone);

				// Add a callback invocation for each crossed interval
				foreach (var intervalValue in crossedIntervals)
				{
					milestonesToTrigger.Add((rangeMilestone, intervalValue));
				}

				// Update to the last crossed value
				if (crossedIntervals.Count > 0)
				{
					rangeMilestone.LastTriggeredValue = crossedIntervals[crossedIntervals.Count - 1];
				}

				// Check if more intervals remain
				if (rangeMilestone.HasMoreIntervals())
				{
					recurringMilestones.Add((id, rangeMilestone));
				}
				else if (!rangeMilestone.IsRecurring)
				{
					// Only remove if not recurring
					exhaustedMilestoneIds.Add(id);
				}
			}

			return (milestonesToTrigger, exhaustedMilestoneIds, recurringMilestones, recurringRegularMilestoneIds);
		}

		private List<float> CalculateCrossedIntervals(TimerRangeMilestone rangeMilestone)
		{
			var crossedIntervals = new List<float>();
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
					crossedIntervals.Add(nextTrigger);
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
					crossedIntervals.Add(nextTrigger);
					nextTrigger += interval;
				}
			}

			return crossedIntervals;
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
			foreach (var id in exhaustedMilestoneIds)
			{
				_milestonesById.Remove(id);
			}
		}

		private void ReAddRangeMilestones(List<(Guid id, TimerRangeMilestone milestone)> recurringMilestones)
		{
			foreach (var (id, rangeMilestone) in recurringMilestones)
			{
				rangeMilestone.UpdateTriggerValue();
				AddMilestoneToTriggerValue(id, rangeMilestone.TriggerValue);
			}
		}

		private void ReAddRecurringRegularMilestones(float triggerValue, List<Guid> recurringRegularMilestoneIds)
		{
			foreach (var id in recurringRegularMilestoneIds)
			{
				AddMilestoneToTriggerValue(id, triggerValue);
			}
		}

		private void InvokeMilestoneCallbacks(List<(TimerMilestone milestone, float? intervalValue)> milestonesToTrigger)
		{
			foreach (var (milestone, intervalValue) in milestonesToTrigger)
			{
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
			foreach (var id in milestoneIds)
			{
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
			foreach (var (id, rangeMilestone, oldTriggerValue) in rangeMilestonesToUpdate)
			{
				RemoveMilestoneFromTriggerValue(id, oldTriggerValue);
				AddMilestoneToTriggerValue(id, rangeMilestone.TriggerValue);
			}
		}
	}
}
