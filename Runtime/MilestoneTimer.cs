using System;
using System.Collections.Generic;
using System.Linq;

namespace Nonatomic.Timers
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
		public MilestoneTimer(float duration) : base(duration)
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
			var processedMilestones = new HashSet<(float triggerValue, Guid id)>();
			
			while (true)
			{
				var nextMilestone = FindNextMilestoneToProcess(processedMilestones);
				if (!nextMilestone.HasValue) break;
				
				MarkMilestonesAsProcessed(nextMilestone.Value.triggerValue, processedMilestones);
				ProcessMilestonesAtTriggerValue(nextMilestone.Value.triggerValue);
			}
		}
		
		private (float triggerValue, Guid id)? FindNextMilestoneToProcess(HashSet<(float, Guid)> processedMilestones)
		{
			float? nextTriggerValue = null;
			Guid? nextMilestoneId = null;
			
			foreach (var kvp in _milestonesByTriggerValue)
			{
				var result = FindTriggeredMilestoneInList(kvp.Key, kvp.Value, processedMilestones);
				if (!result.HasValue) continue;
				
				if (nextTriggerValue == null || kvp.Key < nextTriggerValue.Value)
				{
					nextTriggerValue = kvp.Key;
					nextMilestoneId = result.Value;
				}
			}
			
			if (nextTriggerValue == null || nextMilestoneId == null) return null;
			
			return (nextTriggerValue.Value, nextMilestoneId.Value);
		}
		
		private Guid? FindTriggeredMilestoneInList(float triggerValue, List<Guid> milestoneIds, HashSet<(float, Guid)> processedMilestones)
		{
			foreach (var id in milestoneIds)
			{
				if (processedMilestones.Contains((triggerValue, id))) continue;
				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;
				if (!ShouldTrigger(milestone)) continue;
				
				return id;
			}
			
			return null;
		}
		
		private void MarkMilestonesAsProcessed(float triggerValue, HashSet<(float, Guid)> processedMilestones)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var idsToProcess)) return;
			
			foreach (var id in idsToProcess.ToList())
			{
				processedMilestones.Add((triggerValue, id));
			}
		}

		private void ProcessMilestonesAtTriggerValue(float triggerValue)
		{
			if (!_milestonesByTriggerValue.TryGetValue(triggerValue, out var triggerIds) || triggerIds.Count == 0) return;
			
			var collectedMilestones = CollectMilestonesForProcessing(triggerIds, triggerValue);
			
			RemoveMilestonesFromTriggerValue(triggerValue);
			RemoveExhaustedMilestones(collectedMilestones.idsToRemove);
			ReAddRangeMilestones(collectedMilestones.rangeMilestonesToReAdd);
			InvokeMilestoneCallbacks(collectedMilestones.milestonesToTrigger);
		}
		
		private (List<TimerMilestone> milestonesToTrigger, List<Guid> idsToRemove, List<(Guid id, TimerRangeMilestone milestone)> rangeMilestonesToReAdd) 
			CollectMilestonesForProcessing(List<Guid> triggerIds, float triggerValue)
		{
			var milestonesToTrigger = new List<TimerMilestone>(triggerIds.Count);
			var idsToRemove = new List<Guid>();
			var rangeMilestonesToReAdd = new List<(Guid id, TimerRangeMilestone milestone)>();
			
			foreach (var id in triggerIds)
			{
				if (!_milestonesById.TryGetValue(id, out var milestone)) continue;
				
				milestonesToTrigger.Add(milestone);
				ProcessMilestoneForReAddOrRemoval(id, milestone, triggerValue, idsToRemove, rangeMilestonesToReAdd);
			}
			
			return (milestonesToTrigger, idsToRemove, rangeMilestonesToReAdd);
		}
		
		private void ProcessMilestoneForReAddOrRemoval(
			Guid id, 
			TimerMilestone milestone, 
			float triggerValue,
			List<Guid> idsToRemove, 
			List<(Guid id, TimerRangeMilestone milestone)> rangeMilestonesToReAdd)
		{
			if (milestone is not TimerRangeMilestone rangeMilestone)
			{
				idsToRemove.Add(id);
				return;
			}
			
			rangeMilestone.LastTriggeredValue = triggerValue;
			
			if (rangeMilestone.HasMoreIntervals())
			{
				rangeMilestonesToReAdd.Add((id, rangeMilestone));
			}
			else
			{
				idsToRemove.Add(id);
			}
		}
		
		private void RemoveMilestonesFromTriggerValue(float triggerValue)
		{
			_milestonesByTriggerValue.Remove(triggerValue);
		}
		
		private void RemoveExhaustedMilestones(List<Guid> idsToRemove)
		{
			foreach (var id in idsToRemove)
			{
				_milestonesById.Remove(id);
			}
		}
		
		private void ReAddRangeMilestones(List<(Guid id, TimerRangeMilestone milestone)> rangeMilestonesToReAdd)
		{
			foreach (var (id, rangeMilestone) in rangeMilestonesToReAdd)
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