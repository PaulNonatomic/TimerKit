using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Nonatomic.TimerKit;

namespace Tests.EditMode
{
	[TestFixture]
	public class StandardTimerTests
	{
		private StandardTimer _timer;
		private bool _onStartCalled;
		private bool _onCompleteCalled;
		private float _lastTickTime;

		[SetUp]
		public void Setup()
		{
			_timer = new StandardTimer(10); // Duration of 10 seconds for testing

			_onStartCalled = false;
			_onCompleteCalled = false;
			_lastTickTime = 0;

			_timer.OnStart += () => _onStartCalled = true;
			_timer.OnComplete += () => _onCompleteCalled = true;
			_timer.OnTick += (timer) => _lastTickTime = timer.TimeRemaining;
		}

		[Test, Timeout(1000)]
		public void Milestone_SelfRemoval_During_Callback_Works()
		{
			// Create a milestone and variable to track that the callback executed
			bool callbackExecuted = false;
			TimerMilestone milestone = null;
			
			// Create a milestone that removes itself during its callback
			milestone = new TimerMilestone(TimeType.TimeRemaining, 8, () => {
				Debug.Log("Self-removing milestone callback executing");
				callbackExecuted = true;
				_timer.RemoveMilestone(milestone);
			});
			
			_timer.AddMilestone(milestone);
			_timer.StartTimer();
			
			// This should trigger the milestone
			Debug.Log("Updating timer to 8 seconds remaining");
			_timer.Update(2); // 8 seconds remaining
			
			// The callback should have executed and not thrown an exception
			Assert.IsTrue(callbackExecuted, "Milestone callback should have executed");
		}
		
		[Test, Timeout(1000)]
		public void Multiple_Milestones_With_Same_TriggerValue_Both_Execute()
		{
			bool milestone1Executed = false;
			bool milestone2Executed = false;
			
			// Create two milestones with the same trigger value
			var milestone1 = new TimerMilestone(TimeType.TimeRemaining, 5, () => {
				Debug.Log("First milestone executing");
				milestone1Executed = true;
			});
			
			var milestone2 = new TimerMilestone(TimeType.TimeRemaining, 5, () => {
				Debug.Log("Second milestone executing");
				milestone2Executed = true;
			});
			
			_timer.AddMilestone(milestone1);
			_timer.AddMilestone(milestone2);
			
			_timer.StartTimer();
			Debug.Log("Updating timer to 5 seconds remaining");
			_timer.Update(5); // 5 seconds remaining
			
			// Both callbacks should have been called in our fixed implementation
			Assert.IsTrue(milestone1Executed, "First milestone should have executed");
			Assert.IsTrue(milestone2Executed, "Second milestone should have executed");
		}
		
		[Test, Timeout(1000)]
		public void Milestone_Adding_Another_Milestone_During_Callback_Works()
		{
			bool outerMilestoneExecuted = false;
			bool innerMilestoneExecuted = false;
			
			// Create a milestone that adds another milestone during its callback
			var outerMilestone = new TimerMilestone(TimeType.TimeRemaining, 8, () => {
				Debug.Log("Outer milestone executing");
				outerMilestoneExecuted = true;
				_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 6, () => {
					Debug.Log("Inner milestone executing");
					innerMilestoneExecuted = true;
				}));
			});
			
			_timer.AddMilestone(outerMilestone);
			_timer.StartTimer();
			
			// Trigger the first milestone
			Debug.Log("Updating timer to 8 seconds remaining");
			_timer.Update(2); // 8 seconds remaining
			Assert.IsTrue(outerMilestoneExecuted, "Outer milestone should have executed");
			
			// Trigger the second milestone that was added dynamically
			Debug.Log("Updating timer to 6 seconds remaining");
			_timer.Update(2); // 6 seconds remaining
			Assert.IsTrue(innerMilestoneExecuted, "Inner milestone should have executed");
		}
		
		[Test, Timeout(1000)]
		public void Complex_Milestone_Interaction_Scenario()
		{
			List<string> executionOrder = new List<string>();
			
			// Create milestones that interact with each other
			var milestone1 = new TimerMilestone(TimeType.TimeRemaining, 8, () => {
				executionOrder.Add("milestone1");
				// Add a milestone that should trigger immediately
				_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 8, () => {
					executionOrder.Add("dynamic_milestone");
				}));
			});
			
			var milestone2 = new TimerMilestone(TimeType.TimeRemaining, 8, () => {
				executionOrder.Add("milestone2");
				// Remove another milestone that should trigger at 6 seconds
				_timer.RemoveMilestonesByCondition(m => m.TriggerValue == 6);
			});
			
			var milestone3 = new TimerMilestone(TimeType.TimeRemaining, 6, () => {
				executionOrder.Add("milestone3");
			});
			
			// Add all milestones
			_timer.AddMilestone(milestone1);
			_timer.AddMilestone(milestone2);
			_timer.AddMilestone(milestone3);
			
			_timer.StartTimer();
			
			// This should trigger milestone1 and milestone2, and the dynamic milestone
			_timer.Update(2); // 8 seconds remaining
			
			// This would trigger milestone3, but it should have been removed
			_timer.Update(2); // 6 seconds remaining
			
			// Check execution order - the key test is that milestone3 should not execute
			Assert.AreEqual(3, executionOrder.Count, "Three milestones should have executed");
			Assert.Contains("milestone1", executionOrder);
			Assert.Contains("milestone2", executionOrder);
			Assert.Contains("dynamic_milestone", executionOrder);
			Assert.That(executionOrder, Does.Not.Contain("milestone3"), "milestone3 should not have executed");
		}
		
		[Test, Timeout(1000)]
		public void Multiple_Milestones_With_Same_TriggerValue_Only_One_Executes_Original_Bug()
		{
			// This test demonstrates the bug in the original implementation
			// where only one milestone with a given trigger value executes
			bool callbackCalled1 = false;
			bool callbackCalled2 = false;
			
			// Create two milestones with the same trigger value
			var milestone1 = new TimerMilestone(TimeType.TimeRemaining, 5, () => {
				Debug.Log("First milestone executing");
				callbackCalled1 = true;
			});
			
			var milestone2 = new TimerMilestone(TimeType.TimeRemaining, 5, () => {
				Debug.Log("Second milestone executing");
				callbackCalled2 = true;
			});
			
			// This test shows that both milestones will now execute
			// when previously only one would execute
			_timer.AddMilestone(milestone1);
			_timer.AddMilestone(milestone2);
			
			_timer.StartTimer();
			_timer.Update(5); // 5 seconds remaining
			
			// In the fixed implementation, both callbacks should be called
			Assert.IsTrue(callbackCalled1, "First callback should be called");
			Assert.IsTrue(callbackCalled2, "Second callback should be called");
		}
		
		[Test, Timeout(1000)]
		public void Milestones_With_Same_TriggerValue_Added_Dynamically_All_Execute()
		{
			List<int> executionOrder = new List<int>();
			
			// Create initial milestone
			var milestone1 = new TimerMilestone(TimeType.TimeRemaining, 5, () => {
				Debug.Log("First milestone executing");
				executionOrder.Add(1);
				
				// Add two more milestones with the same trigger value during execution
				_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 3, () => {
					Debug.Log("Second milestone executing");
					executionOrder.Add(2);
				}));
				_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 3, () => {
					Debug.Log("Third milestone executing");
					executionOrder.Add(3);
				}));
			});
			
			_timer.AddMilestone(milestone1);
			_timer.StartTimer();
			
			// Trigger the first milestone
			Debug.Log("Updating timer to 5 seconds remaining");
			_timer.Update(5); // 5 seconds remaining
			
			// Trigger the dynamically added milestones
			Debug.Log("Updating timer to 3 seconds remaining");
			_timer.Update(2); // 3 seconds remaining
			
			// Log the execution order for debugging
			Debug.Log("Execution order: " + String.Join(", ", executionOrder));
			
			// Check that all milestones executed in the correct order
			Assert.AreEqual(3, executionOrder.Count, "All three milestones should have executed");
			Assert.AreEqual(1, executionOrder[0], "First milestone should execute first");
			Assert.Contains(2, executionOrder, "Second milestone should execute");
			Assert.Contains(3, executionOrder, "Third milestone should execute");
		}
		
		// Ensure existing functionality still works
		[Test, Timeout(1000)]
		public void StartTimer_SetsIsRunningTrueAndFiresOnStartEvent()
		{
			_timer.StartTimer();
			Assert.IsTrue(_timer.IsRunning);
			Assert.IsTrue(_onStartCalled);
		}

		[Test, Timeout(1000)]
		public void Timer_UpdateDecreasesTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Update(1); // Simulate 1 second of time passing
			Assert.AreEqual(9, _timer.TimeRemaining);
			Assert.AreEqual(9, _lastTickTime);
		}

		[Test, Timeout(1000)]
		public void Timer_ReachesZero_CompletesCorrectly()
		{
			_timer.StartTimer();
			_timer.Update(10); // Simulate 10 seconds of time passing
			Assert.AreEqual(0, _timer.TimeRemaining);
			Assert.IsTrue(_onCompleteCalled);
		}

		[Test, Timeout(1000)]
		public void FastForward_DecreasesTimeRemainingCorrectly()
		{
			_timer.StartTimer();
			_timer.FastForward(3);
			Assert.AreEqual(7, _timer.TimeRemaining, "TimeRemaining should be decremented by 3 seconds.");
		}

		[Test, Timeout(1000)]
		public void Rewind_IncreasesTimeRemainingCorrectly()
		{
			_timer.StartTimer();
			_timer.Update(5); // Move the timer to the halfway point
			_timer.Rewind(3);
			Assert.AreEqual(8, _timer.TimeRemaining, "TimeRemaining should be incremented by 3 seconds.");
		}
		
		[Test, Timeout(1000)]
		public void RangeMilestone_TriggersEverySecond_ForLastXSeconds()
		{
			int triggerCount = 0;
			var triggerTimes = new List<float>();
			
			// Add a range milestone for the last 5 seconds (TimeRemaining: 5 to 0, every 1 second)
			var rangeMilestone = _timer.AddRangeMilestone(
				TimeType.TimeRemaining, 
				5,  // Start at 5 seconds remaining
				0,  // End at 0 seconds remaining
				1,  // Trigger every 1 second
				() => {
					triggerCount++;
					triggerTimes.Add(_timer.TimeRemaining);
					Debug.Log($"Range milestone triggered at {_timer.TimeRemaining} seconds remaining");
				}
			);
			
			_timer.StartTimer();
			
			// Update timer to 5 seconds remaining
			_timer.Update(5); // Now at 5 seconds remaining - should trigger first time
			Assert.AreEqual(1, triggerCount, "Should have triggered once at 5 seconds");
			
			// Update to 4 seconds remaining
			_timer.Update(1); // Now at 4 seconds remaining - should trigger second time
			Assert.AreEqual(2, triggerCount, "Should have triggered twice");
			
			// Update to 3 seconds remaining
			_timer.Update(1); // Now at 3 seconds remaining
			Assert.AreEqual(3, triggerCount, "Should have triggered three times");
			
			// Update to 2 seconds remaining
			_timer.Update(1); // Now at 2 seconds remaining
			Assert.AreEqual(4, triggerCount, "Should have triggered four times");
			
			// Update to 1 second remaining
			_timer.Update(1); // Now at 1 second remaining
			Assert.AreEqual(5, triggerCount, "Should have triggered five times");
			
			// Update to 0 seconds remaining
			_timer.Update(1); // Now at 0 seconds remaining
			Assert.AreEqual(6, triggerCount, "Should have triggered six times (including at 0)");
			
			// Verify the trigger times
			Assert.AreEqual(6, triggerTimes.Count);
			Assert.AreEqual(5, triggerTimes[0], 0.01f, "First trigger at 5 seconds");
			Assert.AreEqual(4, triggerTimes[1], 0.01f, "Second trigger at 4 seconds");
			Assert.AreEqual(3, triggerTimes[2], 0.01f, "Third trigger at 3 seconds");
			Assert.AreEqual(2, triggerTimes[3], 0.01f, "Fourth trigger at 2 seconds");
			Assert.AreEqual(1, triggerTimes[4], 0.01f, "Fifth trigger at 1 second");
			Assert.AreEqual(0, triggerTimes[5], 0.01f, "Sixth trigger at 0 seconds");
		}
		
		[Test, Timeout(1000)]
		public void RangeMilestone_WithLargeStep_TriggersCorrectly()
		{
			int triggerCount = 0;
			
			// Test with a large step that skips multiple intervals at once
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				8,  // Start at 8 seconds
				2,  // End at 2 seconds
				2,  // Trigger every 2 seconds
				() => {
					triggerCount++;
					Debug.Log($"Triggered at {_timer.TimeRemaining} seconds remaining");
				}
			);
			
			_timer.StartTimer();
			
			// Jump directly from 10 to 3 seconds remaining (skipping multiple intervals)
			_timer.Update(7); // Now at 3 seconds remaining
			
			// Should have triggered at 8, 6, and 4 (but not 2 yet since we're at 3)
			Assert.AreEqual(3, triggerCount, "Should have triggered for 8, 6, and 4 seconds");
			
			// Update to 1 second remaining
			_timer.Update(2); // Now at 1 second remaining
			
			// Should have triggered at 2 seconds as well
			Assert.AreEqual(4, triggerCount, "Should have triggered at 2 seconds as well");
		}
		
		[Test, Timeout(1000)]
		public void RangeMilestone_TimeElapsed_WorksCorrectly()
		{
			int triggerCount = 0;
			var triggerTimes = new List<float>();
			
			// Add a range milestone for TimeElapsed from 2 to 5 seconds, every 1 second
			_timer.AddRangeMilestone(
				TimeType.TimeElapsed,
				2,  // Start at 2 seconds elapsed
				5,  // End at 5 seconds elapsed
				1,  // Trigger every 1 second
				() => {
					triggerCount++;
					triggerTimes.Add(_timer.TimeElapsed);
					Debug.Log($"Triggered at {_timer.TimeElapsed} seconds elapsed");
				}
			);
			
			_timer.StartTimer();
			
			// Update to 1 second elapsed - no trigger yet
			_timer.Update(1);
			Assert.AreEqual(0, triggerCount, "Should not trigger before range start");
			
			// Update to 2 seconds elapsed - first trigger
			_timer.Update(1);
			Assert.AreEqual(1, triggerCount, "Should trigger at range start");
			
			// Update to 3 seconds elapsed
			_timer.Update(1);
			Assert.AreEqual(2, triggerCount, "Should trigger at 3 seconds");
			
			// Update to 4 seconds elapsed
			_timer.Update(1);
			Assert.AreEqual(3, triggerCount, "Should trigger at 4 seconds");
			
			// Update to 5 seconds elapsed
			_timer.Update(1);
			Assert.AreEqual(4, triggerCount, "Should trigger at 5 seconds");
			
			// Update to 6 seconds elapsed - no more triggers
			_timer.Update(1);
			Assert.AreEqual(4, triggerCount, "Should not trigger after range end");
		}
		
		[Test, Timeout(1000)]
		public void RangeMilestone_RemovalDuringCallback_Works()
		{
			int triggerCount = 0;
			TimerRangeMilestone rangeMilestone = null;
			
			// Create a range milestone that removes itself after 2 triggers
			rangeMilestone = _timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5,
				0,
				1,
				() => {
					triggerCount++;
					Debug.Log($"Range milestone triggered (count: {triggerCount})");
					if (triggerCount == 2)
					{
						_timer.RemoveMilestone(rangeMilestone);
						Debug.Log("Range milestone removed itself");
					}
				}
			);
			
			_timer.StartTimer();
			
			// Trigger at 5 seconds
			_timer.Update(5);
			Assert.AreEqual(1, triggerCount, "First trigger");
			
			// Trigger at 4 seconds and remove
			_timer.Update(1);
			Assert.AreEqual(2, triggerCount, "Second trigger (milestone removes itself)");
			
			// No more triggers
			_timer.Update(1); // 3 seconds
			_timer.Update(1); // 2 seconds
			_timer.Update(1); // 1 second
			_timer.Update(1); // 0 seconds
			
			Assert.AreEqual(2, triggerCount, "Should not trigger after removal");
		}
		
		[Test, Timeout(1000)]
		public void RangeMilestone_Reset_WorksCorrectly()
		{
			int triggerCount = 0;
			
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				3,
				0,
				1,
				() => triggerCount++
			);
			
			_timer.StartTimer();
			
			// Trigger some milestones
			_timer.Update(7); // 3 seconds remaining
			_timer.Update(1); // 2 seconds remaining
			Assert.AreEqual(2, triggerCount, "Should have triggered twice");
			
			// Reset the timer
			_timer.ResetTimer();
			_timer.StartTimer();
			
			// Should trigger again from the beginning
			_timer.Update(7); // 3 seconds remaining
			Assert.AreEqual(3, triggerCount, "Should trigger again after reset");
			
			_timer.Update(1); // 2 seconds remaining
			Assert.AreEqual(4, triggerCount, "Should continue triggering after reset");
		}
		
		[Test, Timeout(1000)]
		public void Multiple_RangeMilestones_WorkTogether()
		{
			int everySecondCount = 0;
			int everyTwoSecondsCount = 0;

			// Add milestone for every second in last 5 seconds
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5, 0, 1,
				() => everySecondCount++
			);

			// Add milestone for every 2 seconds in last 6 seconds
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				6, 0, 2,
				() => everyTwoSecondsCount++
			);

			_timer.StartTimer();

			// Update to 6 seconds remaining
			_timer.Update(4);
			Assert.AreEqual(0, everySecondCount, "First milestone not triggered yet");
			Assert.AreEqual(1, everyTwoSecondsCount, "Second milestone triggered once");

			// Update to 5 seconds remaining
			_timer.Update(1);
			Assert.AreEqual(1, everySecondCount, "First milestone triggered once");
			Assert.AreEqual(1, everyTwoSecondsCount, "Second milestone still once");

			// Update to 4 seconds remaining
			_timer.Update(1);
			Assert.AreEqual(2, everySecondCount, "First milestone triggered twice");
			Assert.AreEqual(2, everyTwoSecondsCount, "Second milestone triggered twice");

			// Update to 2 seconds remaining
			_timer.Update(2);
			Assert.AreEqual(4, everySecondCount, "First milestone triggered 4 times");
			Assert.AreEqual(3, everyTwoSecondsCount, "Second milestone triggered 3 times");

			// Update to 0 seconds remaining
			_timer.Update(2);
			Assert.AreEqual(6, everySecondCount, "First milestone triggered 6 times total");
			Assert.AreEqual(4, everyTwoSecondsCount, "Second milestone triggered 4 times total");
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_DoesNotFireOutsideRange_AboveRange()
		{
			var triggerTimes = new List<float>();

			// Add a range milestone for 5 to 1 seconds remaining
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5, // rangeStart
				1, // rangeEnd
				1, // interval
				() => {
					triggerTimes.Add(_timer.TimeRemaining);
					Debug.Log($"Triggered at {_timer.TimeRemaining} seconds remaining");
				}
			);

			_timer.StartTimer();

			// Update to 10 seconds remaining (still at start) - should not trigger
			Assert.AreEqual(0, triggerTimes.Count, "Should not trigger before reaching range start");

			// Update to 6 seconds remaining - should not trigger (outside range)
			_timer.Update(4);
			Assert.AreEqual(0, triggerTimes.Count, "Should not trigger at 6 seconds (above range)");

			// Update to 5 seconds remaining - should trigger (at range start)
			_timer.Update(1);
			Assert.AreEqual(1, triggerTimes.Count, "Should trigger at 5 seconds (range start)");
			Assert.AreEqual(5f, triggerTimes[0], 0.01f);

			// Continue through range
			_timer.Update(1); // 4 seconds
			_timer.Update(1); // 3 seconds
			_timer.Update(1); // 2 seconds
			_timer.Update(1); // 1 second

			Assert.AreEqual(5, triggerTimes.Count, "Should have triggered 5 times total (5, 4, 3, 2, 1)");

			// Update to 0 seconds - should not trigger (below range end of 1)
			_timer.Update(1);
			Assert.AreEqual(5, triggerTimes.Count, "Should not trigger at 0 seconds (below range end)");
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_OnlyFiresWithinSpecifiedRange()
		{
			var triggerTimes = new List<float>();
			int triggerCount = 0;

			// Simulate the VoiceOverCountdownService scenario: range 5 to 1, interval 1
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5f, // rangeStart
				1f, // rangeEnd
				1f, // interval
				() => {
					triggerCount++;
					var timeRemaining = _timer.TimeRemaining;
					triggerTimes.Add(timeRemaining);
					Debug.Log($"Milestone triggered at {timeRemaining} seconds remaining (trigger #{triggerCount})");

					// This is the check from VoiceOverCountdownService
					if (timeRemaining < 1 || timeRemaining > 5)
					{
						Assert.Fail($"Milestone fired outside intended range at {timeRemaining} seconds");
					}
				}
			);

			_timer.StartTimer();

			// Simulate updates through the entire timer duration
			for (float time = 10f; time > 0; time -= 0.5f)
			{
				_timer.Update(0.5f);
				Debug.Log($"Timer updated: {_timer.TimeRemaining} seconds remaining, triggers so far: {triggerCount}");
			}

			// Should have triggered exactly at 5, 4, 3, 2, 1
			Assert.AreEqual(5, triggerCount, "Should trigger exactly 5 times (at 5, 4, 3, 2, 1 seconds)");
			Assert.AreEqual(5, triggerTimes.Count, "Should have recorded 5 trigger times");

			// Verify all trigger times are within range
			foreach (var triggerTime in triggerTimes)
			{
				Assert.GreaterOrEqual(triggerTime, 1f, $"Trigger at {triggerTime} should be >= 1 second");
				Assert.LessOrEqual(triggerTime, 5f, $"Trigger at {triggerTime} should be <= 5 seconds");
			}

			// Verify exact trigger points
			Assert.Contains(5f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList(), "Should trigger at 5 seconds");
			Assert.Contains(4f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList(), "Should trigger at 4 seconds");
			Assert.Contains(3f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList(), "Should trigger at 3 seconds");
			Assert.Contains(2f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList(), "Should trigger at 2 seconds");
			Assert.Contains(1f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList(), "Should trigger at 1 second");
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_TriggersAtEachInterval_WhenSkippingMultipleIntervals()
		{
			var triggerTimes = new List<float>();

			// Range from 5 to 1, interval 1
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5f,
				1f,
				1f,
				() => triggerTimes.Add(_timer.TimeRemaining)
			);

			_timer.StartTimer();

			// Make a large jump from 10 seconds to 0.5 seconds (crossing the entire range)
			_timer.Update(9.5f);

			// Should have triggered at 5, 4, 3, 2, 1 (but not at 0.5)
			Assert.AreEqual(5, triggerTimes.Count, "Should trigger at all intervals within range");

			// The triggers should be in descending order since we're using TimeRemaining
			Assert.That(triggerTimes[0], Is.EqualTo(5f).Within(0.01f));
			Assert.That(triggerTimes[1], Is.EqualTo(4f).Within(0.01f));
			Assert.That(triggerTimes[2], Is.EqualTo(3f).Within(0.01f));
			Assert.That(triggerTimes[3], Is.EqualTo(2f).Within(0.01f));
			Assert.That(triggerTimes[4], Is.EqualTo(1f).Within(0.01f));
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_DoesNotTrigger_WhenTimerNeverEntersRange()
		{
			int triggerCount = 0;

			// Create a 3-second timer with range 5 to 1
			var shortTimer = new StandardTimer(3f);

			shortTimer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5f, // rangeStart (timer never reaches 5 seconds remaining)
				1f, // rangeEnd
				1f, // interval
				() => triggerCount++
			);

			shortTimer.StartTimer();

			// Complete the entire timer
			shortTimer.Update(3f);

			// Should never trigger because timer duration (3s) is less than range start (5s)
			Assert.AreEqual(0, triggerCount, "Should not trigger when timer never enters the range");
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_MissedTrigger_BugReproduction()
		{
			var triggerTimes = new List<float>();
			int triggerCount = 0;

			// This test specifically tries to reproduce the bug where milestones
			// might not fire within the intended range
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5f,
				1f,
				1f,
				() => {
					triggerCount++;
					triggerTimes.Add(_timer.TimeRemaining);
					Debug.Log($"Trigger #{triggerCount} at {_timer.TimeRemaining}s remaining");
				}
			);

			_timer.StartTimer();

			// Update in small increments to ensure we don't skip any intervals
			_timer.Update(4.5f); // 5.5 seconds remaining - no trigger yet
			Assert.AreEqual(0, triggerCount, "No trigger yet at 5.5s");

			_timer.Update(0.6f); // 4.9 seconds remaining - should have triggered at 5
			Assert.AreEqual(1, triggerCount, "Should have triggered at 5 seconds");

			_timer.Update(0.95f); // 3.95 seconds remaining - should have triggered at 4
			Assert.AreEqual(2, triggerCount, "Should have triggered at 4 seconds");

			_timer.Update(1.0f); // 2.95 seconds remaining - should have triggered at 3
			Assert.AreEqual(3, triggerCount, "Should have triggered at 3 seconds");

			_timer.Update(1.0f); // 1.95 seconds remaining - should have triggered at 2
			Assert.AreEqual(4, triggerCount, "Should have triggered at 2 seconds");

			_timer.Update(0.96f); // 0.99 seconds remaining - should have triggered at 1
			Assert.AreEqual(5, triggerCount, "Should have triggered at 1 second");

			_timer.Update(1.0f); // 0 seconds or complete - no more triggers
			Assert.AreEqual(5, triggerCount, "No more triggers after range end");

			// Verify all triggers were within range [1, 5]
			foreach (var time in triggerTimes)
			{
				Assert.GreaterOrEqual(time, 1f, $"Trigger at {time}s should be >= 1");
				Assert.LessOrEqual(time, 5f, $"Trigger at {time}s should be <= 5");
			}
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_FiresExactlyAtBoundaries()
		{
			var triggerTimes = new List<float>();

			// Range exactly from 5.0 to 1.0 with 1.0 interval
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5.0f,
				1.0f,
				1.0f,
				() => triggerTimes.Add(_timer.TimeRemaining)
			);

			_timer.StartTimer();

			// Reach exactly 5.0
			_timer.Update(5.0f);
			Assert.AreEqual(1, triggerTimes.Count, "Should trigger at exactly 5.0");
			Assert.That(triggerTimes[0], Is.EqualTo(5.0f).Within(0.001f));

			// Reach exactly 4.0
			_timer.Update(1.0f);
			Assert.AreEqual(2, triggerTimes.Count, "Should trigger at exactly 4.0");
			Assert.That(triggerTimes[1], Is.EqualTo(4.0f).Within(0.001f));

			// Reach exactly 3.0
			_timer.Update(1.0f);
			Assert.AreEqual(3, triggerTimes.Count, "Should trigger at exactly 3.0");
			Assert.That(triggerTimes[2], Is.EqualTo(3.0f).Within(0.001f));

			// Reach exactly 2.0
			_timer.Update(1.0f);
			Assert.AreEqual(4, triggerTimes.Count, "Should trigger at exactly 2.0");
			Assert.That(triggerTimes[3], Is.EqualTo(2.0f).Within(0.001f));

			// Reach exactly 1.0 (range end)
			_timer.Update(1.0f);
			Assert.AreEqual(5, triggerTimes.Count, "Should trigger at exactly 1.0 (range end inclusive)");
			Assert.That(triggerTimes[4], Is.EqualTo(1.0f).Within(0.001f));

			// Go to 0.0 - should not trigger anymore
			_timer.Update(1.0f);
			Assert.AreEqual(5, triggerTimes.Count, "Should not trigger at 0.0 (below range end)");
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_WithFloatingPointImprecision()
		{
			var triggerTimes = new List<float>();
			int triggerCount = 0;

			// Using slightly imprecise values that might accumulate error
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5.0f,
				1.0f,
				1.0f,
				() => {
					triggerCount++;
					triggerTimes.Add(_timer.TimeRemaining);
					Debug.Log($"Triggered at {_timer.TimeRemaining:F6}");
				}
			);

			_timer.StartTimer();

			// Use Update values that might introduce floating point errors
			_timer.Update(0.1f);
			_timer.Update(0.1f);
			_timer.Update(0.1f); // Should be at ~9.7
			_timer.Update(0.2f);
			_timer.Update(0.2f);
			_timer.Update(0.2f); // Should be at ~9.1
			_timer.Update(0.7f); // Should be at ~8.4
			_timer.Update(0.7f); // Should be at ~7.7
			_timer.Update(0.7f); // Should be at ~7.0
			_timer.Update(0.7f); // Should be at ~6.3
			_timer.Update(0.7f); // Should be at ~5.6
			_timer.Update(0.7f); // Should be at ~4.9 - should have triggered at 5

			Assert.GreaterOrEqual(triggerCount, 1, "Should have triggered at 5 seconds");

			// Continue to end
			for (int i = 0; i < 10; i++)
			{
				_timer.Update(0.5f);
			}

			Assert.AreEqual(5, triggerCount, "Should trigger exactly 5 times despite floating point imprecision");

			// Verify all are in range
			foreach (var time in triggerTimes)
			{
				Assert.GreaterOrEqual(time, 0.99f, $"Trigger at {time} should be >= 1 (with small tolerance)");
				Assert.LessOrEqual(time, 5.01f, $"Trigger at {time} should be <= 5 (with small tolerance)");
			}
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_TriggerOutsideRange_BugScenario()
		{
			var allTriggerTimes = new List<(float timeRemaining, string where)>();
			int outsideRangeTriggers = 0;

			// Add a range milestone for 5 to 1 seconds
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5f,
				1f,
				1f,
				() => {
					var time = _timer.TimeRemaining;
					allTriggerTimes.Add((time, $"TimeRemaining={time}"));
					Debug.Log($"Milestone triggered at {time} seconds");

					// Check if outside range - this should NEVER happen
					if (time < 1f || time > 5f)
					{
						outsideRangeTriggers++;
						Debug.LogError($"BUG: Milestone triggered OUTSIDE range at {time} seconds!");
					}
				}
			);

			_timer.StartTimer();

			// Run through entire timer with various update patterns
			// Pattern 1: Small steps
			for (int i = 0; i < 5; i++)
			{
				_timer.Update(0.3f);
			}

			// Pattern 2: Medium steps
			for (int i = 0; i < 3; i++)
			{
				_timer.Update(0.8f);
			}

			// Pattern 3: Large step
			_timer.Update(2.0f);

			// Pattern 4: Complete the timer
			_timer.Update(10f);

			// The main assertion: NO triggers should occur outside the range
			Assert.AreEqual(0, outsideRangeTriggers,
				$"Milestones should NEVER trigger outside the range [1, 5]. " +
				$"Found {outsideRangeTriggers} triggers outside range. " +
				$"All triggers: {string.Join(", ", allTriggerTimes.Select(t => t.timeRemaining))}");

			// Should have triggered exactly 5 times (at 5, 4, 3, 2, 1)
			Assert.AreEqual(5, allTriggerTimes.Count,
				$"Should trigger exactly 5 times. Triggers: {string.Join(", ", allTriggerTimes.Select(t => t.timeRemaining))}");
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_EdgeCase_ZeroRemaining()
		{
			var triggerTimes = new List<float>();

			// Range from 3 to 0 (inclusive of 0)
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				3f,
				0f,
				1f,
				() => triggerTimes.Add(_timer.TimeRemaining)
			);

			_timer.StartTimer();

			// Complete the timer
			_timer.Update(10f);

			// Should trigger at 3, 2, 1, 0
			Assert.AreEqual(4, triggerTimes.Count, "Should trigger at 3, 2, 1, and 0");
			Assert.Contains(3f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList());
			Assert.Contains(2f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList());
			Assert.Contains(1f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList());
			Assert.Contains(0f, triggerTimes.Select(t => Mathf.Round(t * 100) / 100).ToList());
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_OnlyRangeEnd()
		{
			var triggerTimes = new List<float>();

			// Range where start == end (should trigger exactly once)
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				3f,
				3f,
				1f,
				() => triggerTimes.Add(_timer.TimeRemaining)
			);

			_timer.StartTimer();

			_timer.Update(7f); // Down to 3 seconds

			Assert.AreEqual(1, triggerTimes.Count, "Should trigger exactly once when start == end");
			Assert.That(triggerTimes[0], Is.EqualTo(3f).Within(0.01f));

			// Continue to completion
			_timer.Update(10f);
			Assert.AreEqual(1, triggerTimes.Count, "Should only trigger once");
		}

		[Test, Timeout(1000)]
		public void RangeMilestone_NonIntegerInterval()
		{
			var triggerTimes = new List<float>();

			// Range with non-integer interval (1.5 seconds)
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				6f,
				0f,
				1.5f,
				() => triggerTimes.Add(_timer.TimeRemaining)
			);

			_timer.StartTimer();

			_timer.Update(10f); // Complete the timer

			// Should trigger at 6, 4.5, 3, 1.5, 0
			Assert.AreEqual(5, triggerTimes.Count, "Should trigger 5 times");

			// Verify approximate values (allowing for floating point)
			Assert.That(triggerTimes[0], Is.EqualTo(6.0f).Within(0.01f), "First trigger at 6.0");
			Assert.That(triggerTimes[1], Is.EqualTo(4.5f).Within(0.01f), "Second trigger at 4.5");
			Assert.That(triggerTimes[2], Is.EqualTo(3.0f).Within(0.01f), "Third trigger at 3.0");
			Assert.That(triggerTimes[3], Is.EqualTo(1.5f).Within(0.01f), "Fourth trigger at 1.5");
			Assert.That(triggerTimes[4], Is.EqualTo(0.0f).Within(0.01f), "Fifth trigger at 0.0");
		}
	}
	
	/// <summary>
	/// Tests for backward compatibility with the deprecated SimpleTimer class.
	/// Ensures that existing code using SimpleTimer continues to work.
	/// </summary>
	[TestFixture]
	public class SimpleTimerBackwardCompatibilityTests
	{
		[Test, Timeout(1000)]
		public void SimpleTimer_DeprecatedClass_StillWorks()
		{
			// This test ensures the deprecated SimpleTimer class still functions correctly
			#pragma warning disable CS0618 // Type or member is obsolete
			var timer = new SimpleTimer(5.0f);
			#pragma warning restore CS0618
			
			bool started = false;
			bool completed = false;
			
			timer.OnStart += () => started = true;
			timer.OnComplete += () => completed = true;
			
			timer.StartTimer();
			Assert.IsTrue(started, "OnStart should fire");
			Assert.IsTrue(timer.IsRunning, "Timer should be running");
			Assert.AreEqual(5.0f, timer.Duration, "Duration should be 5 seconds");
			
			timer.Update(5.0f);
			Assert.IsTrue(completed, "OnComplete should fire");
			Assert.AreEqual(0.0f, timer.TimeRemaining, "Timer should be complete");
		}
		
		[Test, Timeout(1000)]
		public void SimpleTimer_MilestoneSupport_StillWorks()
		{
			#pragma warning disable CS0618 // Type or member is obsolete
			var timer = new SimpleTimer(10.0f);
			#pragma warning restore CS0618
			
			bool milestoneTriggered = false;
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 5.0f, () => milestoneTriggered = true);
			
			timer.AddMilestone(milestone);
			timer.StartTimer();
			timer.Update(5.0f); // Should trigger milestone at 5 seconds remaining
			
			Assert.IsTrue(milestoneTriggered, "Milestone should have triggered");
		}
	}
}