using NUnit.Framework;
using Nonatomic.Timers;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Tests.EditMode
{
	[TestFixture]
	public class FixedSimpleTimerTests
	{
		private SimpleTimer _simpleTimer;
		private bool _onStartCalled;
		private bool _onCompleteCalled;
		private float _lastTickTime;

		[SetUp]
		public void Setup()
		{
			_simpleTimer = new SimpleTimer(10); // Duration of 10 seconds for testing

			_onStartCalled = false;
			_onCompleteCalled = false;
			_lastTickTime = 0;

			_simpleTimer.OnStart += () => _onStartCalled = true;
			_simpleTimer.OnComplete += () => _onCompleteCalled = true;
			_simpleTimer.OnTick += (timer) => _lastTickTime = timer.TimeRemaining;
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
				_simpleTimer.RemoveMilestone(milestone);
			});
			
			_simpleTimer.AddMilestone(milestone);
			_simpleTimer.StartTimer();
			
			// This should trigger the milestone
			Debug.Log("Updating timer to 8 seconds remaining");
			_simpleTimer.Update(2); // 8 seconds remaining
			
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
			
			_simpleTimer.AddMilestone(milestone1);
			_simpleTimer.AddMilestone(milestone2);
			
			_simpleTimer.StartTimer();
			Debug.Log("Updating timer to 5 seconds remaining");
			_simpleTimer.Update(5); // 5 seconds remaining
			
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
				_simpleTimer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 6, () => {
					Debug.Log("Inner milestone executing");
					innerMilestoneExecuted = true;
				}));
			});
			
			_simpleTimer.AddMilestone(outerMilestone);
			_simpleTimer.StartTimer();
			
			// Trigger the first milestone
			Debug.Log("Updating timer to 8 seconds remaining");
			_simpleTimer.Update(2); // 8 seconds remaining
			Assert.IsTrue(outerMilestoneExecuted, "Outer milestone should have executed");
			
			// Trigger the second milestone that was added dynamically
			Debug.Log("Updating timer to 6 seconds remaining");
			_simpleTimer.Update(2); // 6 seconds remaining
			Assert.IsTrue(innerMilestoneExecuted, "Inner milestone should have executed");
		}
		
		[Test, Timeout(1000)]
		public void Complex_Milestone_Interaction_Scenario()
		{
			Debug.Log("---------- Starting Complex_Milestone_Interaction_Scenario test ----------");
			List<string> executionOrder = new List<string>();
			bool milestone3Present = false;
			
			// Create milestones that interact with each other
			var milestone1 = new TimerMilestone(TimeType.TimeRemaining, 8, () => {
				Debug.Log("Executing milestone1");
				executionOrder.Add("milestone1");
				// Add a milestone that should trigger immediately
				Debug.Log("Adding dynamic milestone at 8 seconds");
				_simpleTimer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 8, () => {
					Debug.Log("Executing dynamic_milestone");
					executionOrder.Add("dynamic_milestone");
				}));
			});
			
			var milestone2 = new TimerMilestone(TimeType.TimeRemaining, 8, () => {
				Debug.Log("Executing milestone2");
				executionOrder.Add("milestone2");
				// Remove another milestone that should trigger at 6 seconds
				Debug.Log("Removing milestones with trigger value 6");
				_simpleTimer.RemoveMilestonesByCondition(m => m.TriggerValue == 6);
			});
			
			var milestone3 = new TimerMilestone(TimeType.TimeRemaining, 6, () => {
				Debug.Log("Executing milestone3 - THIS SHOULD NOT HAPPEN");
				executionOrder.Add("milestone3");
			});
			
			Debug.Log("Adding milestone1 (8s)");
			_simpleTimer.AddMilestone(milestone1);
			
			Debug.Log("Adding milestone2 (8s)");
			_simpleTimer.AddMilestone(milestone2);
			
			Debug.Log("Adding milestone3 (6s)");
			_simpleTimer.AddMilestone(milestone3);
			
			// Check if milestone3 is present in the milestones collection
			foreach (var key in _simpleTimer.GetType().GetField("_milestonesByTriggerValue", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_simpleTimer) as SortedList<float, List<Guid>>)
			{
				if (Math.Abs(key.Key - 6.0f) < 0.001f)
				{
					milestone3Present = true;
					Debug.Log("Verified milestone3 is present with trigger value 6");
				}
			}
			Assert.IsTrue(milestone3Present, "Milestone3 should be in the collection before update");
			
			_simpleTimer.StartTimer();
			
			// This should trigger milestone1 and milestone2, and the dynamic milestone
			Debug.Log("Updating timer to 8 seconds remaining");
			_simpleTimer.Update(2); // 8 seconds remaining
			
			// Check again if milestone3 is present
			milestone3Present = false;
			foreach (var key in _simpleTimer.GetType().GetField("_milestonesByTriggerValue", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_simpleTimer) as SortedList<float, List<Guid>>)
			{
				if (Math.Abs(key.Key - 6.0f) < 0.001f)
				{
					milestone3Present = true;
					Debug.Log("Milestone3 is still present with trigger value 6 - THIS SHOULD NOT HAPPEN");
				}
			}
			Assert.IsFalse(milestone3Present, "Milestone3 should have been removed after the first update");
			
			// This would trigger milestone3, but it should have been removed
			Debug.Log("Updating timer to 6 seconds remaining");
			_simpleTimer.Update(2); // 6 seconds remaining
			
			// Log the execution order for debugging
			Debug.Log("Execution order: " + String.Join(", ", executionOrder));
			
			// Check execution order
			Assert.AreEqual(3, executionOrder.Count, "Three milestones should have executed");
			Assert.Contains("milestone1", executionOrder);
			Assert.Contains("milestone2", executionOrder);
			Assert.Contains("dynamic_milestone", executionOrder);
			Assert.That(executionOrder, Does.Not.Contain("milestone3"), "milestone3 should not have executed");
			Debug.Log("---------- Completed Complex_Milestone_Interaction_Scenario test ----------");
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
			_simpleTimer.AddMilestone(milestone1);
			_simpleTimer.AddMilestone(milestone2);
			
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5); // 5 seconds remaining
			
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
				_simpleTimer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 3, () => {
					Debug.Log("Second milestone executing");
					executionOrder.Add(2);
				}));
				_simpleTimer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 3, () => {
					Debug.Log("Third milestone executing");
					executionOrder.Add(3);
				}));
			});
			
			_simpleTimer.AddMilestone(milestone1);
			_simpleTimer.StartTimer();
			
			// Trigger the first milestone
			Debug.Log("Updating timer to 5 seconds remaining");
			_simpleTimer.Update(5); // 5 seconds remaining
			
			// Trigger the dynamically added milestones
			Debug.Log("Updating timer to 3 seconds remaining");
			_simpleTimer.Update(2); // 3 seconds remaining
			
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
			_simpleTimer.StartTimer();
			Assert.IsTrue(_simpleTimer.IsRunning);
			Assert.IsTrue(_onStartCalled);
		}

		[Test, Timeout(1000)]
		public void Timer_UpdateDecreasesTimeRemaining()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(1); // Simulate 1 second of time passing
			Assert.AreEqual(9, _simpleTimer.TimeRemaining);
			Assert.AreEqual(9, _lastTickTime);
		}

		[Test, Timeout(1000)]
		public void Timer_ReachesZero_CompletesCorrectly()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(10); // Simulate 10 seconds of time passing
			Assert.AreEqual(0, _simpleTimer.TimeRemaining);
			Assert.IsTrue(_onCompleteCalled);
		}

		[Test, Timeout(1000)]
		public void FastForward_DecreasesTimeRemainingCorrectly()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.FastForward(3);
			Assert.AreEqual(7, _simpleTimer.TimeRemaining, "TimeRemaining should be decremented by 3 seconds.");
		}

		[Test, Timeout(1000)]
		public void Rewind_IncreasesTimeRemainingCorrectly()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5); // Move the timer to the halfway point
			_simpleTimer.Rewind(3);
			Assert.AreEqual(8, _simpleTimer.TimeRemaining, "TimeRemaining should be incremented by 3 seconds.");
		}
	}
}