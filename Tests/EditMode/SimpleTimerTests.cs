using NUnit.Framework;
using Nonatomic.Timers;

namespace Tests.EditMode
{
	[TestFixture]
	public class SimpleTimerTests
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

		[Test]
		public void StartTimer_SetsIsRunningTrueAndFiresOnStartEvent()
		{
			_simpleTimer.StartTimer();
			Assert.IsTrue(_simpleTimer.IsRunning);
			Assert.IsTrue(_onStartCalled);
		}

		[Test]
		public void Timer_UpdateDecreasesTimeRemaining()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(1); // Simulate 1 second of time passing
			Assert.AreEqual(9, _simpleTimer.TimeRemaining);
			Assert.AreEqual(9, _lastTickTime);
		}

		[Test]
		public void Timer_ReachesZero_CompletesCorrectly()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(10); // Simulate 10 seconds of time passing
			Assert.AreEqual(0, _simpleTimer.TimeRemaining);
			Assert.IsTrue(_onCompleteCalled);
		}

		[Test]
		public void StopTimer_StopsTheTimerWithoutCompleting()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.StopTimer();
			Assert.IsFalse(_simpleTimer.IsRunning);
			Assert.IsFalse(_onCompleteCalled);
		}

		[Test]
		public void ResetTimer_ResetsTimeRemaining()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5); // Simulate half the duration passing
			_simpleTimer.ResetTimer();
			Assert.AreEqual(10, _simpleTimer.TimeRemaining);
			Assert.IsFalse(_simpleTimer.IsRunning);
		}

		[Test]
		public void Timer_DoesNotTickWhenStopped()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.StopTimer();
			_simpleTimer.Update(1); // Simulate time passing
			Assert.AreNotEqual(1, _lastTickTime); // Ensure last tick time is not updated
		}
		
		[Test]
		public void FastForward_DecreasesTimeRemainingCorrectly()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.FastForward(3);
			Assert.AreEqual(7, _simpleTimer.TimeRemaining, "TimeRemaining should be decremented by 3 seconds.");
		}

		[Test]
		public void FastForward_DoesNotSetTimeBelowZero()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.FastForward(15); // Fast forward beyond the duration
			Assert.AreEqual(0, _simpleTimer.TimeRemaining, "TimeRemaining should not go below zero.");
			Assert.IsFalse(_simpleTimer.IsRunning, "SimpleTimer should stop running when it reaches zero.");
		}

		[Test]
		public void Rewind_IncreasesTimeRemainingCorrectly()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5); // Move the timer to the halfway point
			_simpleTimer.Rewind(3);
			Assert.AreEqual(8, _simpleTimer.TimeRemaining, "TimeRemaining should be incremented by 3 seconds.");
		}

		[Test]
		public void Rewind_DoesNotExceedOriginalDuration()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Rewind(1); // Try to rewind when the timer hasn't advanced
			Assert.AreEqual(10, _simpleTimer.TimeRemaining, "TimeRemaining should not exceed the initial duration.");
		}

		[Test]
		public void Rewind_And_FastForward_AreAccurate()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5);  // 5 seconds elapsed, 5 remaining
			_simpleTimer.Rewind(3);  // 8 seconds should remain
			_simpleTimer.FastForward(4); // 4 seconds should remain
			Assert.AreEqual(4, _simpleTimer.TimeRemaining, "Operations should accurately modify TimeRemaining.");
		}
		
		[Test]
		public void RemoveMilestonesByCondition_Removes_Correctly()
		{
			var triggered1 = false;
			var triggered2 = false;
			var milestone1 = new TimerMilestone(TimeType.TimeRemaining, 5, () => triggered1 = true);
			var milestone2 = new TimerMilestone(TimeType.TimeRemaining, 3, () => triggered2 = true);
			_simpleTimer.AddMilestone(milestone1);
			_simpleTimer.AddMilestone(milestone2);
			_simpleTimer.RemoveMilestonesByCondition(m => m.TriggerValue == 5);
			_simpleTimer.StartTimer();
			_simpleTimer.Update(7); // Triggers 3, not 5
			Assert.IsFalse(triggered1, "Removed milestone should not trigger.");
			Assert.IsTrue(triggered2, "Remaining milestone should trigger.");
		}
		
		[Test]
		public void Milestone_Should_Trigger_When_Time_Remaining_Reaches_Specified_Value()
		{
			var milestoneTriggered = false;
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 5, () => milestoneTriggered = true);
			_simpleTimer.AddMilestone(milestone);

			// Simulate timer running until the milestone should trigger
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5);  // 5 seconds passed, 5 seconds remaining

			Assert.IsTrue(milestoneTriggered, "Milestone should trigger when time remaining is exactly 5 seconds.");
		}

		[Test]
		public void Milestone_Should_Not_Trigger_Prematurely()
		{
			var milestoneTriggered = false;
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 3, () => milestoneTriggered = true);
			_simpleTimer.AddMilestone(milestone);

			// Update the timer but not enough to trigger the milestone
			_simpleTimer.StartTimer();
			_simpleTimer.Update(6);  // 6 seconds passed, 4 seconds remaining

			Assert.IsFalse(milestoneTriggered, "Milestone should not trigger prematurely.");
		}

		[Test]
		public void Milestone_Should_Be_Removable()
		{
			var milestoneTriggered = false;
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 3, () => milestoneTriggered = true);
			_simpleTimer.AddMilestone(milestone);
			_simpleTimer.RemoveMilestone(milestone);

			// Try to trigger the removed milestone
			_simpleTimer.StartTimer();
			_simpleTimer.Update(7);  // 7 seconds passed, 3 seconds remaining

			Assert.IsFalse(milestoneTriggered, "Milestone should not trigger after being removed.");
		}

		[Test]
		public void All_Milestones_Should_Be_Clearable()
		{
			var firstMilestoneTriggered = false;
			var secondMilestoneTriggered = false;
			_simpleTimer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 3, () => firstMilestoneTriggered = true));
			_simpleTimer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 6, () => secondMilestoneTriggered = true));
			
			_simpleTimer.ClearMilestones();

			// Try to trigger any milestones after clearing them
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5);  // 5 seconds passed, 5 seconds remaining

			Assert.IsFalse(firstMilestoneTriggered, "First milestone should not trigger after milestones are cleared.");
			Assert.IsFalse(secondMilestoneTriggered, "Second milestone should not trigger after milestones are cleared.");
		}
		
		[Test]
		public void Timer_Handles_NonIntegerDeltaTime_Precisely()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(0.333f);
			_simpleTimer.Update(0.333f);
			_simpleTimer.Update(0.334f); // Sum to approximately 1 second

			// Since the timer started with 10 seconds, it should now show approximately 9 seconds remaining
			Assert.AreEqual(9f, _simpleTimer.TimeRemaining, 0.01, "TimeRemaining should accurately reflect non-integer delta time updates.");
		}

		[Test]
		public void Repeated_Start_And_Stop_Maintains_Correct_TimeRemaining()
		{
			_simpleTimer.StartTimer();
			for (var i = 0; i < 5; i++)
			{
				_simpleTimer.Update(1); // Update 1 second
				_simpleTimer.StopTimer();
				_simpleTimer.ResumeTimer();
			}
			// Check that only 5 seconds have elapsed despite starts and stops
			Assert.AreEqual(5, _simpleTimer.TimeRemaining, "Time remaining should correctly account for starts and stops.");
		}
		
		[Test]
		public void Negative_Inputs_Do_Not_Affect_Timer()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.FastForward(-5); // Attempt to fast forward negatively
			_simpleTimer.Rewind(-5); // Attempt to rewind negatively
			_simpleTimer.Update(1); // Update 1 second
			Assert.AreEqual(9, _simpleTimer.TimeRemaining, "Negative inputs should not affect timer.");
		}
		
		[Test]
		public void Deactivate_And_Reactivate_Milestone()
		{
			var milestoneTriggered = false;
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 5, () => milestoneTriggered = true);
			_simpleTimer.AddMilestone(milestone);
			_simpleTimer.RemoveMilestone(milestone);
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5); // Should not trigger because it's deactivated
			Assert.IsFalse(milestoneTriggered, "Milestone should not trigger when deactivated.");

			_simpleTimer.AddMilestone(milestone);
			_simpleTimer.Update(5); // Update further to trigger
			Assert.IsTrue(milestoneTriggered, "Milestone should trigger when reactivated.");
		}
		
		[Test]
		public void Serialization_And_Deserialization()
		{
			_simpleTimer.StartTimer();
			_simpleTimer.Update(5); // 5 seconds elapsed
			var savedState = _simpleTimer.Serialize();
			var newTimer = new SimpleTimer(10);
			newTimer.Deserialize(savedState);
			Assert.AreEqual(5, newTimer.TimeRemaining, "Deserialized timer should have the correct remaining time.");
		}
		
		[Test]
		public void High_Frequency_Update_Accuracy()
		{
			_simpleTimer.StartTimer();
			var totalElapsed = 0f;
			while (totalElapsed < 10)
			{
				_simpleTimer.Update(0.01f); // Simulate high-frequency updates
				totalElapsed += 0.01f;
			}
			Assert.AreEqual(0, _simpleTimer.TimeRemaining, 0.1, "Timer should accurately track time under high-frequency updates.");
		}
	}
}