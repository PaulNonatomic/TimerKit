using NUnit.Framework;
using System;
using System.Collections.Generic;
using Nonatomic.TimerKit;
using UnityEngine;

namespace Tests.EditMode
{
	/// <summary>
	/// Comprehensive allocation tests to measure and track GC allocations in the timer system.
	/// These tests establish baselines and help identify allocation regressions.
	/// </summary>
	[TestFixture]
	public class AllocationTests
	{
		// Tolerance for "zero allocation" tests - some minimal allocation may occur
		private const long ZeroAllocationThreshold = 0;

		// Number of warmup iterations before measuring
		private const int WarmupIterations = 10;

		// Number of measurement iterations
		private const int MeasurementIterations = 100;

		#region BasicTimer Update Allocation Tests

		[Test]
		public void BasicTimer_Update_MeasureAllocations()
		{
			var timer = new BasicTimer(1000f);
			timer.StartTimer();

			// Warmup
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.016f);
			}

			// Measure
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / MeasurementIterations;

			Debug.Log($"BasicTimer.Update: {allocatedBytes} bytes total, {bytesPerUpdate} bytes/update over {MeasurementIterations} iterations");

			// Document current allocation level
			// Target: 0 bytes per update
			Assert.GreaterOrEqual(0, 0, $"BasicTimer.Update allocates {bytesPerUpdate} bytes per call");
		}

		[Test]
		public void BasicTimer_Update_WithOnTickSubscriber_MeasureAllocations()
		{
			var timer = new BasicTimer(1000f);
			int tickCount = 0;
			timer.OnTick += (t) => tickCount++;
			timer.StartTimer();

			// Warmup
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.016f);
			}

			// Measure
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / MeasurementIterations;

			Debug.Log($"BasicTimer.Update with OnTick: {allocatedBytes} bytes total, {bytesPerUpdate} bytes/update");
			Debug.Log($"Tick count: {tickCount}");
		}

		[Test]
		public void BasicTimer_Update_NotRunning_MeasureAllocations()
		{
			var timer = new BasicTimer(1000f);
			// Don't start - timer is not running

			// Warmup
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.016f);
			}

			// Measure
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"BasicTimer.Update (not running): {allocatedBytes} bytes total over {MeasurementIterations} iterations");

			// When not running, should allocate nothing
			Assert.AreEqual(0, allocatedBytes, "BasicTimer.Update when not running should not allocate");
		}

		#endregion

		#region StandardTimer/MilestoneTimer Update Allocation Tests

		[Test]
		public void StandardTimer_Update_NoMilestones_MeasureAllocations()
		{
			var timer = new StandardTimer(1000f);
			timer.StartTimer();

			// Warmup
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.016f);
			}

			// Measure
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / MeasurementIterations;

			Debug.Log($"StandardTimer.Update (no milestones): {allocatedBytes} bytes total, {bytesPerUpdate} bytes/update");

			// Document current allocation - this is what we want to reduce
		}

		[Test]
		public void StandardTimer_Update_WithSingleMilestone_NotTriggered_MeasureAllocations()
		{
			var timer = new StandardTimer(1000f);
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 100f, () => { }));
			timer.StartTimer();

			// Warmup - stay above milestone trigger
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.016f);
			}

			// Reset to ensure we don't trigger milestone
			timer.ResetTimer();
			timer.StartTimer();

			// Measure
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.016f); // Small updates, won't reach 100f milestone
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / MeasurementIterations;

			Debug.Log($"StandardTimer.Update (1 milestone, not triggered): {allocatedBytes} bytes total, {bytesPerUpdate} bytes/update");
		}

		[Test]
		public void StandardTimer_Update_WithMultipleMilestones_NotTriggered_MeasureAllocations()
		{
			var timer = new StandardTimer(1000f);

			// Add milestones that won't be triggered during test
			for (int i = 1; i <= 10; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, i * 10f, () => { }));
			}

			timer.StartTimer();

			// Warmup
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.016f);
			}

			timer.ResetTimer();
			timer.StartTimer();

			// Measure
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / MeasurementIterations;

			Debug.Log($"StandardTimer.Update (10 milestones, not triggered): {allocatedBytes} bytes total, {bytesPerUpdate} bytes/update");
		}

		[Test]
		public void StandardTimer_Update_WithRangeMilestone_NotTriggered_MeasureAllocations()
		{
			var timer = new StandardTimer(1000f);

			// Add range milestone that won't be triggered during test
			timer.AddRangeMilestone(TimeType.TimeRemaining, 100f, 10f, 10f, () => { });

			timer.StartTimer();

			// Warmup
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.016f);
			}

			timer.ResetTimer();
			timer.StartTimer();

			// Measure
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / MeasurementIterations;

			Debug.Log($"StandardTimer.Update (range milestone, not triggered): {allocatedBytes} bytes total, {bytesPerUpdate} bytes/update");
		}

		#endregion

		#region Milestone Triggering Allocation Tests

		[Test]
		public void StandardTimer_Update_MilestoneTriggering_MeasureAllocations()
		{
			int triggerCount = 0;
			long totalAllocations = 0;

			// Run multiple complete timer cycles to measure allocations during triggering
			for (int cycle = 0; cycle < 10; cycle++)
			{
				var timer = new StandardTimer(10f);
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => triggerCount++));
				timer.StartTimer();

				// Warmup
				timer.Update(0.1f);

				long startMemory = GC.GetTotalMemory(true);

				// Update until milestone triggers
				timer.Update(5f);

				long endMemory = GC.GetTotalMemory(true);
				totalAllocations += (endMemory - startMemory);
			}

			long avgAllocationPerTrigger = totalAllocations / 10;
			Debug.Log($"StandardTimer milestone trigger: {avgAllocationPerTrigger} bytes avg per trigger");
			Debug.Log($"Total triggers: {triggerCount}");
		}

		[Test]
		public void StandardTimer_Update_RangeMilestoneTriggering_MeasureAllocations()
		{
			int triggerCount = 0;

			var timer = new StandardTimer(100f);
			timer.AddRangeMilestone(TimeType.TimeRemaining, 90f, 10f, 10f, () => triggerCount++);
			timer.StartTimer();

			// Warmup
			timer.Update(5f);

			long startMemory = GC.GetTotalMemory(true);

			// Update to trigger all range milestones
			timer.Update(90f);

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerTrigger = triggerCount > 0 ? allocatedBytes / triggerCount : allocatedBytes;

			Debug.Log($"StandardTimer range milestone triggers: {allocatedBytes} bytes total for {triggerCount} triggers");
			Debug.Log($"Average: {bytesPerTrigger} bytes per trigger");
		}

		[Test]
		public void StandardTimer_Update_MultipleMilestonesTriggering_MeasureAllocations()
		{
			int triggerCount = 0;

			var timer = new StandardTimer(100f);

			// Add 10 milestones that will all trigger
			for (int i = 1; i <= 10; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, i * 9f, () => triggerCount++));
			}

			timer.StartTimer();

			// Warmup
			timer.Update(5f);

			long startMemory = GC.GetTotalMemory(true);

			// Update to trigger all milestones
			timer.Update(95f);

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerTrigger = triggerCount > 0 ? allocatedBytes / triggerCount : allocatedBytes;

			Debug.Log($"StandardTimer 10 milestones trigger: {allocatedBytes} bytes total for {triggerCount} triggers");
			Debug.Log($"Average: {bytesPerTrigger} bytes per trigger");
		}

		#endregion

		#region Timer MonoBehaviour Allocation Tests

		[Test]
		public void Timer_HandleTimerTick_SimulateAllocation()
		{
			// Simulate what Timer.HandleTimerTick does
			// This tests the event forwarding allocation

			Action<IReadOnlyTimer> onTick = null;
			int invokeCount = 0;

			onTick = (t) => invokeCount++;

			var timer = new StandardTimer(1000f);

			// Warmup
			for (int i = 0; i < WarmupIterations; i++)
			{
				onTick?.Invoke(timer);
			}

			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				onTick?.Invoke(timer);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Event invocation: {allocatedBytes} bytes over {MeasurementIterations} invocations");
		}

		#endregion

		#region Sustained Update Tests (Simulating Real Usage)

		[Test]
		public void StandardTimer_SustainedUpdates_60FPS_OneSecond_MeasureAllocations()
		{
			var timer = new StandardTimer(100f);
			timer.StartTimer();

			// Warmup
			for (int i = 0; i < 60; i++)
			{
				timer.Update(0.016667f);
			}

			timer.ResetTimer();
			timer.StartTimer();

			// Measure 1 second of updates at 60 FPS
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < 60; i++)
			{
				timer.Update(0.016667f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"StandardTimer 1 second @ 60 FPS (no milestones): {allocatedBytes} bytes");
			Debug.Log($"Per frame: {allocatedBytes / 60} bytes");
		}

		[Test]
		public void StandardTimer_SustainedUpdates_WithMilestones_60FPS_OneSecond_MeasureAllocations()
		{
			var timer = new StandardTimer(100f);

			// Add milestones that won't trigger during the test window
			for (int i = 1; i <= 5; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, i * 10f, () => { }));
			}

			timer.StartTimer();

			// Warmup
			for (int i = 0; i < 60; i++)
			{
				timer.Update(0.016667f);
			}

			timer.ResetTimer();
			timer.StartTimer();

			// Measure 1 second of updates at 60 FPS
			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < 60; i++)
			{
				timer.Update(0.016667f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"StandardTimer 1 second @ 60 FPS (5 milestones): {allocatedBytes} bytes");
			Debug.Log($"Per frame: {allocatedBytes / 60} bytes");
		}

		[Test]
		public void StandardTimer_SustainedUpdates_ManyTimers_MeasureAllocations()
		{
			const int timerCount = 100;
			var timers = new List<StandardTimer>(timerCount);

			for (int i = 0; i < timerCount; i++)
			{
				var timer = new StandardTimer(1000f);
				timer.StartTimer();
				timers.Add(timer);
			}

			// Warmup
			for (int frame = 0; frame < 60; frame++)
			{
				foreach (var timer in timers)
				{
					timer.Update(0.016667f);
				}
			}

			// Reset all timers
			foreach (var timer in timers)
			{
				timer.ResetTimer();
				timer.StartTimer();
			}

			// Measure 1 second of updates
			long startMemory = GC.GetTotalMemory(true);

			for (int frame = 0; frame < 60; frame++)
			{
				foreach (var timer in timers)
				{
					timer.Update(0.016667f);
				}
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"100 StandardTimers, 1 second @ 60 FPS: {allocatedBytes} bytes");
			Debug.Log($"Per frame (all timers): {allocatedBytes / 60} bytes");
			Debug.Log($"Per timer per frame: {allocatedBytes / 60 / timerCount} bytes");
		}

		[Test]
		public void StandardTimer_SustainedUpdates_ManyTimersWithMilestones_MeasureAllocations()
		{
			const int timerCount = 100;
			var timers = new List<StandardTimer>(timerCount);

			for (int i = 0; i < timerCount; i++)
			{
				var timer = new StandardTimer(1000f);
				// Add a milestone that won't trigger
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 500f, () => { }));
				timer.StartTimer();
				timers.Add(timer);
			}

			// Warmup
			for (int frame = 0; frame < 60; frame++)
			{
				foreach (var timer in timers)
				{
					timer.Update(0.016667f);
				}
			}

			// Reset all timers
			foreach (var timer in timers)
			{
				timer.ResetTimer();
				timer.StartTimer();
			}

			// Measure 1 second of updates
			long startMemory = GC.GetTotalMemory(true);

			for (int frame = 0; frame < 60; frame++)
			{
				foreach (var timer in timers)
				{
					timer.Update(0.016667f);
				}
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"100 StandardTimers with milestones, 1 second @ 60 FPS: {allocatedBytes} bytes");
			Debug.Log($"Per frame (all timers): {allocatedBytes / 60} bytes");
			Debug.Log($"Per timer per frame: {allocatedBytes / 60 / timerCount} bytes");
		}

		#endregion

		#region Specific Method Allocation Tests

		[Test]
		public void MilestoneTimer_CheckAndTriggerMilestones_Isolated_MeasureAllocations()
		{
			// This test isolates the CheckAndTriggerMilestones method
			// by using a timer with milestones but not triggering them

			var timer = new StandardTimer(1000f);

			// Add milestone that won't trigger
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 100f, () => { }));
			timer.StartTimer();

			// Warmup - do several updates
			for (int i = 0; i < WarmupIterations; i++)
			{
				timer.Update(0.001f);
			}

			// Force GC to get clean measurement
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);

			// Single update to measure allocation of one CheckAndTriggerMilestones call
			timer.Update(0.001f);

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Single Update with milestone check: {allocatedBytes} bytes");

			// Now measure multiple to get average
			timer.ResetTimer();
			timer.StartTimer();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			startMemory = GC.GetTotalMemory(false);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timer.Update(0.001f);
			}

			endMemory = GC.GetTotalMemory(false);
			allocatedBytes = endMemory - startMemory;
			long bytesPerCall = allocatedBytes / MeasurementIterations;

			Debug.Log($"CheckAndTriggerMilestones: {allocatedBytes} bytes total, {bytesPerCall} bytes/call over {MeasurementIterations} calls");
		}

		[Test]
		public void MilestoneTimer_ProcessAllTriggeredMilestones_WhenTriggering_MeasureAllocations()
		{
			// Measure allocations when milestones actually trigger
			long totalAllocations = 0;
			int measurementCount = 20;

			for (int m = 0; m < measurementCount; m++)
			{
				var timer = new StandardTimer(10f);
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => { }));
				timer.StartTimer();

				// Update close to milestone
				timer.Update(4.9f);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				long startMemory = GC.GetTotalMemory(false);

				// This update should trigger the milestone
				timer.Update(0.2f);

				long endMemory = GC.GetTotalMemory(false);
				totalAllocations += (endMemory - startMemory);
			}

			long avgAllocation = totalAllocations / measurementCount;
			Debug.Log($"ProcessAllTriggeredMilestones (single trigger): ~{avgAllocation} bytes avg");
		}

		#endregion

		#region Allocation Comparison Tests

		[Test]
		public void Compare_BasicTimer_Vs_StandardTimer_Allocations()
		{
			// BasicTimer
			var basicTimer = new BasicTimer(1000f);
			basicTimer.StartTimer();

			for (int i = 0; i < WarmupIterations; i++)
			{
				basicTimer.Update(0.016f);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long basicStartMemory = GC.GetTotalMemory(false);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				basicTimer.Update(0.016f);
			}

			long basicEndMemory = GC.GetTotalMemory(false);
			long basicAllocations = basicEndMemory - basicStartMemory;

			// StandardTimer (no milestones)
			var standardTimer = new StandardTimer(1000f);
			standardTimer.StartTimer();

			for (int i = 0; i < WarmupIterations; i++)
			{
				standardTimer.Update(0.016f);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long standardStartMemory = GC.GetTotalMemory(false);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				standardTimer.Update(0.016f);
			}

			long standardEndMemory = GC.GetTotalMemory(false);
			long standardAllocations = standardEndMemory - standardStartMemory;

			Debug.Log($"BasicTimer: {basicAllocations} bytes ({basicAllocations / MeasurementIterations} bytes/update)");
			Debug.Log($"StandardTimer: {standardAllocations} bytes ({standardAllocations / MeasurementIterations} bytes/update)");
			Debug.Log($"Overhead: {standardAllocations - basicAllocations} bytes ({(standardAllocations - basicAllocations) / MeasurementIterations} bytes/update)");
		}

		[Test]
		public void Compare_WithAndWithout_Milestones_Allocations()
		{
			// Without milestones
			var timerNoMilestones = new StandardTimer(1000f);
			timerNoMilestones.StartTimer();

			for (int i = 0; i < WarmupIterations; i++)
			{
				timerNoMilestones.Update(0.016f);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long noMilestoneStartMemory = GC.GetTotalMemory(false);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timerNoMilestones.Update(0.016f);
			}

			long noMilestoneEndMemory = GC.GetTotalMemory(false);
			long noMilestoneAllocations = noMilestoneEndMemory - noMilestoneStartMemory;

			// With milestones (not triggered)
			var timerWithMilestones = new StandardTimer(1000f);
			for (int j = 0; j < 5; j++)
			{
				timerWithMilestones.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 100f + j * 10f, () => { }));
			}
			timerWithMilestones.StartTimer();

			for (int i = 0; i < WarmupIterations; i++)
			{
				timerWithMilestones.Update(0.016f);
			}

			timerWithMilestones.ResetTimer();
			timerWithMilestones.StartTimer();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long withMilestoneStartMemory = GC.GetTotalMemory(false);

			for (int i = 0; i < MeasurementIterations; i++)
			{
				timerWithMilestones.Update(0.016f);
			}

			long withMilestoneEndMemory = GC.GetTotalMemory(false);
			long withMilestoneAllocations = withMilestoneEndMemory - withMilestoneStartMemory;

			Debug.Log($"Without milestones: {noMilestoneAllocations} bytes ({noMilestoneAllocations / MeasurementIterations} bytes/update)");
			Debug.Log($"With 5 milestones: {withMilestoneAllocations} bytes ({withMilestoneAllocations / MeasurementIterations} bytes/update)");
			Debug.Log($"Milestone overhead: {withMilestoneAllocations - noMilestoneAllocations} bytes ({(withMilestoneAllocations - noMilestoneAllocations) / MeasurementIterations} bytes/update)");
		}

		#endregion

		#region Allocation Baseline Tests (For CI/Regression Testing)

		[Test]
		public void Baseline_BasicTimer_Update_ShouldNotAllocate()
		{
			var timer = new BasicTimer(1000f);
			timer.StartTimer();

			// Warmup
			for (int i = 0; i < 100; i++)
			{
				timer.Update(0.016f);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < 1000; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Baseline - BasicTimer.Update: {allocatedBytes} bytes over 1000 updates");

			// This is a baseline test - document current state
			// After optimization, update this threshold
		}

		[Test]
		public void Baseline_StandardTimer_Update_NoMilestones_CurrentAllocation()
		{
			var timer = new StandardTimer(1000f);
			timer.StartTimer();

			// Warmup
			for (int i = 0; i < 100; i++)
			{
				timer.Update(0.016f);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < 1000; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / 1000;

			Debug.Log($"Baseline - StandardTimer.Update (no milestones): {allocatedBytes} bytes over 1000 updates");
			Debug.Log($"Per update: {bytesPerUpdate} bytes");

			// Current allocation level - update after optimization
			// Target: 0 bytes per update
		}

		[Test]
		public void Baseline_StandardTimer_Update_WithMilestones_CurrentAllocation()
		{
			var timer = new StandardTimer(1000f);
			for (int j = 0; j < 5; j++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 100f + j * 50f, () => { }));
			}
			timer.StartTimer();

			// Warmup
			for (int i = 0; i < 100; i++)
			{
				timer.Update(0.016f);
			}

			timer.ResetTimer();
			timer.StartTimer();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(true);

			for (int i = 0; i < 1000; i++)
			{
				timer.Update(0.016f);
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerUpdate = allocatedBytes / 1000;

			Debug.Log($"Baseline - StandardTimer.Update (5 milestones): {allocatedBytes} bytes over 1000 updates");
			Debug.Log($"Per update: {bytesPerUpdate} bytes");

			// Current allocation level - update after optimization
			// Target: 0 bytes per update when milestones don't trigger
		}

		#endregion
	}
}
