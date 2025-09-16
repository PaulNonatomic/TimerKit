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
		private bool _processingMilestones;
		private Dictionary<Guid, TimerMilestone> _milestonesById = new Dictionary<Guid, TimerMilestone>();
		private SortedList<float, List<Guid>> _milestonesByTriggerValue = new SortedList<float, List<Guid>>();

		/// <summary>
		/// Initializes a new instance of the MilestoneTimer class with a specified duration.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		/// <param name="timeSource">Optional custom time source. If null, uses internal time management.</param>
		/// <param name="preserveTimeSourceValue">If true and timeSource is provided, preserves the time source's current value.</param>
		public MilestoneTimer(float duration, ITimeSource timeSource = null, bool preserveTimeSourceValue = false) 
			: base(duration, timeSource, preserveTimeSourceValue)
		{
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
		/// Resets all range milestones to their initial state.
		/// </summary>
		protected override void OnTimerReset()
		{
			ResetRangeMilestones();
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
		/// Adds a range milestone that triggers at regular intervals within a specified range.
		/// </summary>
		/// <param name="type">The time type (TimeRemaining, TimeElapsed, etc.)</param>
		/// <param name="rangeStart">The start of the range (higher value for TimeRemaining)</param>
		/// <param name="rangeEnd">The end of the range (lower value for TimeRemaining)</param>
		/// <param name="interval">The interval at which to trigger callbacks</param>
		/// <param name="callback">The callback to execute at each interval</param>
		/// <returns>The created TimerRangeMilestone</returns>
		public virtual TimerRangeMilestone AddRangeMilestone(TimeType type, float rangeStart, float rangeEnd, float interval, Action callback)
		{
			var rangeMilestone = new TimerRangeMilestone(type, rangeStart, rangeEnd, interval, callback);
			AddMilestone(rangeMilestone);
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

		private void CheckAndTriggerMilestones()
		{
			if (_processingMilestones) return;
			
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
			
			while (TryProcessNextMilestone(alreadyTriggeredMilestones))
			{
				// Continue processing until no more milestones to trigger
			}
		}
		
		private bool TryProcessNextMilestone(HashSet<(float triggerValue, Guid id)> alreadyTriggeredMilestones)
		{
			var nextMilestone = FindNextMilestoneToProcess(alreadyTriggeredMilestones);
			if (!nextMilestone.HasValue) return false;
			
			MarkMilestonesAsProcessed(nextMilestone.Value.triggerValue, alreadyTriggeredMilestones);
			ProcessMilestonesAtTriggerValue(nextMilestone.Value.triggerValue);
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
				
				if (ShouldUpdateLowestTriggerValue(kvp.Key, lowestUnprocessedTriggerValue))
				{
					lowestUnprocessedTriggerValue = kvp.Key;
					correspondingMilestoneId = milestoneId.Value;
				}
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

		private void ProcessMilestonesAtTriggerValue(float triggerValue)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var triggerIds) || triggerIds.Count == 0) return;
			
			var collectedMilestones = CollectMilestonesForProcessing(triggerIds, triggerValue);
			
			RemoveMilestonesFromTriggerValue(triggerValue);
			RemoveExhaustedMilestones(collectedMilestones.exhaustedMilestoneIds);
			ReAddRangeMilestones(collectedMilestones.recurringMilestones);
			InvokeMilestoneCallbacks(collectedMilestones.milestonesToTrigger);
		}
		
		private (List<TimerMilestone> milestonesToTrigger, List<Guid> exhaustedMilestoneIds, List<(Guid id, TimerRangeMilestone milestone)> recurringMilestones) 
			CollectMilestonesForProcessing(List<Guid> triggerIds, float triggerValue)
		{
			var milestonesToTrigger = new List<TimerMilestone>(triggerIds.Count);
			var exhaustedMilestoneIds = new List<Guid>();
			var recurringMilestones = new List<(Guid id, TimerRangeMilestone milestone)>();
			
			foreach (var id in triggerIds)
			{
				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;
				
				milestonesToTrigger.Add(milestone);
				ProcessMilestoneForReAddOrRemoval(id, milestone, triggerValue, exhaustedMilestoneIds, recurringMilestones);
			}
			
			return (milestonesToTrigger, exhaustedMilestoneIds, recurringMilestones);
		}
		
		private void ProcessMilestoneForReAddOrRemoval(
			Guid id, 
			TimerMilestone milestone, 
			float triggerValue,
			List<Guid> exhaustedMilestoneIds, 
			List<(Guid id, TimerRangeMilestone milestone)> recurringMilestones)
		{
			if (milestone is not TimerRangeMilestone rangeMilestone)
			{
				exhaustedMilestoneIds.Add(id);
				return;
			}
			
			ProcessRangeMilestone(id, rangeMilestone, triggerValue, exhaustedMilestoneIds, recurringMilestones);
		}
		
		private void ProcessRangeMilestone(
			Guid id, 
			TimerRangeMilestone rangeMilestone, 
			float triggerValue,
			List<Guid> exhaustedMilestoneIds, 
			List<(Guid id, TimerRangeMilestone milestone)> recurringMilestones)
		{
			rangeMilestone.LastTriggeredValue = triggerValue;
			
			if (rangeMilestone.HasMoreIntervals())
			{
				recurringMilestones.Add((id, rangeMilestone));
			}
			else
			{
				exhaustedMilestoneIds.Add(id);
			}
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
		
		private void InvokeMilestoneCallbacks(List<TimerMilestone> milestonesToTrigger)
		{
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

		private void AddMilestoneToTriggerValue(Guid id, float triggerValue)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var milestoneList))
			{
				milestoneList = new List<Guid>();
				_milestonesByTriggerValue[triggerValue] = milestoneList;
			}
			milestoneList.Add(id);
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
				if (condition(pair.Value))
				{
					milestonesToRemove.Add(pair.Key);
				}
			}
			
			return milestonesToRemove;
		}
		
		private void RemoveMilestonesByIds(List<Guid> milestoneIds)
		{
			foreach (var id in milestoneIds)
			{
				if (_milestonesById.TryGetValue(id, out var milestone))
				{
					RemoveMilestoneById(id, milestone.TriggerValue);
				}
			}
		}

		private void ResetRangeMilestones()
		{
			var rangeMilestonesToUpdate = CollectRangeMilestones();
			UpdateRangeMilestonePositions(rangeMilestonesToUpdate);
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