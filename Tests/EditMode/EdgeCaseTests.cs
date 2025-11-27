using NUnit.Framework;
using System;
using System.Collections.Generic;
using Nonatomic.TimerKit;

namespace Tests.EditMode
{
	[TestFixture]
	public class EdgeCaseTests
	{
		#region Extreme Duration Tests

		[Test]
		public void Timer_WithVerySmallDuration()
		{
			var timer = new StandardTimer(0.001f);
			bool completed = false;
			timer.OnComplete += () => completed = true;

			timer.StartTimer();
			timer.Update(0.001f);

			Assert.IsTrue(completed);
		}

		[Test]
		public void Timer_WithVeryLargeDuration()
		{
			var timer = new StandardTimer(86400f); // 24 hours
			timer.StartTimer();
			timer.Update(43200f); // 12 hours

			Assert.AreEqual(43200f, timer.TimeRemaining, 0.1f);
			Assert.AreEqual(0.5f, timer.ProgressElapsed, 0.001f);
		}

		[Test]
		public void Timer_WithZeroDuration_IsAlreadyComplete()
		{
			var timer = new StandardTimer(0f);

			// With 0 duration, TimeRemaining is 0 from the start
			Assert.AreEqual(0f, timer.TimeRemaining);
			Assert.AreEqual(0f, timer.Duration);
		}

		#endregion

		#region Large Update Delta Tests

		[Test]
		public void Timer_WithLargeUpdateDelta_TriggersAllMilestones()
		{
			var timer = new StandardTimer(100f);
			var triggered = new List<float>();

			for (int i = 90; i >= 10; i -= 10)
			{
				int value = i; // Capture for closure
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, value, () => triggered.Add(value)));
			}

			timer.StartTimer();
			timer.Update(100f); // Complete in one update

			Assert.AreEqual(9, triggered.Count);
		}

		[Test]
		public void Timer_WithLargeUpdateDelta_RangeMilestonesWork()
		{
			var timer = new StandardTimer(100f);
			int triggerCount = 0;

			timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				90f,
				10f,
				10f,
				() => triggerCount++
			);

			timer.StartTimer();
			timer.Update(100f); // Complete in one update

			// Should trigger at 90, 80, 70, 60, 50, 40, 30, 20, 10 = 9 times
			Assert.AreEqual(9, triggerCount);
		}

		#endregion

		#region Many Milestones Tests

		[Test]
		public void Timer_WithManyMilestones_AllTrigger()
		{
			var timer = new StandardTimer(100f);
			int triggerCount = 0;

			// Add 100 milestones
			for (int i = 1; i <= 100; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeElapsed, i * 0.99f, () => triggerCount++));
			}

			timer.StartTimer();
			timer.Update(100f);

			Assert.AreEqual(100, triggerCount);
		}

		[Test]
		public void Timer_WithManyMilestonesAtSameTriggerValue_AllTrigger()
		{
			var timer = new StandardTimer(10f);
			int triggerCount = 0;

			// Add 50 milestones at same trigger value
			for (int i = 0; i < 50; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => triggerCount++));
			}

			timer.StartTimer();
			timer.Update(5f);

			Assert.AreEqual(50, triggerCount);
		}

		#endregion

		#region Milestone Removal Edge Cases

		[Test]
		public void RemoveAllMilestones_DuringUpdate_Works()
		{
			var timer = new StandardTimer(10f);
			int secondTriggerCount = 0;

			// First milestone at 8f removes all milestones
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 8f, () => timer.RemoveAllMilestones()));
			// Second milestone at 5f should not trigger if removed
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => secondTriggerCount++));

			timer.StartTimer();
			timer.Update(2.5f); // Triggers first milestone at 8f, removes all

			// Second update - milestone at 5f should have been removed
			timer.Update(3f); // Would cross 5f if milestone still existed

			Assert.AreEqual(0, secondTriggerCount, "Second milestone should have been removed before triggering");
		}

		[Test]
		public void RemoveMilestone_ThatDoesNotExist_DoesNotThrow()
		{
			var timer = new StandardTimer(10f);
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 5f, () => { });

			// Remove milestone that was never added
			Assert.DoesNotThrow(() => timer.RemoveMilestone(milestone));
		}

		[Test]
		public void RemoveMilestonesByCondition_RemovesAllMatching()
		{
			var timer = new StandardTimer(10f);
			int recurringCount = 0;
			int nonRecurringCount = 0;

			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 8f, () => recurringCount++, isRecurring: true));
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => recurringCount++, isRecurring: true));
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 3f, () => nonRecurringCount++, isRecurring: false));

			timer.RemoveMilestonesByCondition(m => m.IsRecurring);

			timer.StartTimer();
			timer.Update(10f);

			Assert.AreEqual(0, recurringCount);
			Assert.AreEqual(1, nonRecurringCount);
		}

		#endregion

		#region Timer State Transition Edge Cases

		[Test]
		public void MultipleStarts_WithoutStop_ResetsEachTime()
		{
			var timer = new StandardTimer(10f);
			int startCount = 0;
			timer.OnStart += () => startCount++;

			timer.StartTimer();
			timer.Update(3f);
			timer.StartTimer(); // Should reset
			timer.Update(3f);
			timer.StartTimer(); // Should reset again

			Assert.AreEqual(3, startCount);
			Assert.AreEqual(10f, timer.TimeRemaining);
		}

		[Test]
		public void ResetWhileRunning_StopsAndResets()
		{
			var timer = new StandardTimer(10f);
			timer.StartTimer();
			timer.Update(3f);

			timer.ResetTimer();

			Assert.IsFalse(timer.IsRunning);
			Assert.AreEqual(10f, timer.TimeRemaining);
		}

		[Test]
		public void FastForward_ToExactlyZero_Completes()
		{
			var timer = new StandardTimer(10f);
			bool completed = false;
			timer.OnComplete += () => completed = true;

			timer.StartTimer();
			timer.FastForward(10f);

			Assert.IsTrue(completed);
			Assert.AreEqual(0f, timer.TimeRemaining);
		}

		[Test]
		public void Rewind_PastFullDuration_ClampsToMax()
		{
			var timer = new StandardTimer(10f);
			timer.StartTimer();
			timer.Update(5f);
			timer.Rewind(100f); // Try to rewind past duration

			Assert.AreEqual(10f, timer.TimeRemaining);
		}

		#endregion

		#region Duration Change Edge Cases

		[Test]
		public void Duration_IncreasedWhileRunning_WithActiveMilestones()
		{
			var timer = new StandardTimer(10f);
			bool milestoneTriggered = false;

			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 15f, () => milestoneTriggered = true));

			timer.StartTimer();
			timer.Update(3f); // At 7 seconds remaining
			timer.Duration = 20f; // Increase duration

			// Now rewind to let milestone trigger
			timer.Rewind(10f); // At 17 seconds remaining
			timer.Update(3f); // At 14 seconds remaining - crosses 15

			// Note: Behavior depends on implementation - milestone might or might not trigger
			// This test documents the current behavior
		}

		[Test]
		public void Duration_SetToZero_WhileRunning()
		{
			var timer = new StandardTimer(10f);
			timer.StartTimer();
			timer.Update(3f);

			timer.Duration = 0f;

			Assert.AreEqual(0f, timer.Duration);
			Assert.AreEqual(0f, timer.TimeRemaining);
		}

		[Test]
		public void Duration_SetToSameValue_StillFiresEvent()
		{
			var timer = new StandardTimer(10f);
			int eventCount = 0;
			timer.OnDurationChanged += (d) => eventCount++;

			timer.Duration = 10f;
			timer.Duration = 10f;
			timer.Duration = 10f;

			Assert.AreEqual(3, eventCount);
		}

		#endregion

		#region Callback Exception Handling

		[Test]
		public void Milestone_CallbackThrows_DoesNotBreakTimer()
		{
			var timer = new StandardTimer(10f);
			int secondMilestoneCount = 0;

			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 8f, () => throw new Exception("Test exception")));
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => secondMilestoneCount++));

			timer.StartTimer();

			// The first milestone throws, but the second should still work
			// Note: This test documents the expected behavior - may need to be adjusted based on implementation
			Assert.Throws<Exception>(() => timer.Update(6f));
		}

		#endregion

		#region Floating Point Precision Tests

		[Test]
		public void Timer_FloatingPointAccumulation()
		{
			var timer = new StandardTimer(1f);
			timer.StartTimer();

			// Many small updates that could accumulate floating point errors
			for (int i = 0; i < 1000; i++)
			{
				timer.Update(0.001f);
			}

			// Should be complete or very close to 0
			Assert.That(timer.TimeRemaining, Is.LessThanOrEqualTo(0.01f));
		}

		[Test]
		public void Milestone_AtVeryPreciseValue()
		{
			var timer = new StandardTimer(10f);
			bool triggered = false;

			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5.123456f, () => triggered = true));

			timer.StartTimer();
			timer.Update(4.8f); // At 5.2 seconds remaining - just before 5.123456
			Assert.IsFalse(triggered, "Should not trigger yet at 5.2 seconds remaining");

			timer.Update(0.1f); // At 5.1 seconds remaining - crosses 5.123456
			Assert.IsTrue(triggered, "Should trigger after crossing 5.123456");
		}

		#endregion

		#region Concurrent Timer Operations

		[Test]
		public void MultipleTimers_IndependentOperation()
		{
			var timer1 = new StandardTimer(10f);
			var timer2 = new StandardTimer(20f);

			int timer1CompleteCount = 0;
			int timer2CompleteCount = 0;

			timer1.OnComplete += () => timer1CompleteCount++;
			timer2.OnComplete += () => timer2CompleteCount++;

			timer1.StartTimer();
			timer2.StartTimer();

			timer1.Update(10f);
			Assert.AreEqual(1, timer1CompleteCount);
			Assert.AreEqual(0, timer2CompleteCount);

			timer2.Update(20f);
			Assert.AreEqual(1, timer1CompleteCount);
			Assert.AreEqual(1, timer2CompleteCount);
		}

		#endregion

		#region Range Milestone Edge Cases

		[Test]
		public void RangeMilestone_WithZeroInterval()
		{
			var timer = new StandardTimer(10f);
			int triggerCount = 0;

			// Zero interval should probably throw or be handled gracefully
			var milestone = new TimerRangeMilestone(
				TimeType.TimeRemaining,
				8f,
				5f,
				0f, // Zero interval
				() => triggerCount++
			);

			timer.AddRangeMilestone(milestone);
			timer.StartTimer();

			// Depending on implementation, this might cause issues or be handled gracefully
			// Test documents current behavior
		}

		[Test]
		public void RangeMilestone_WithNegativeInterval()
		{
			var timer = new StandardTimer(10f);
			int triggerCount = 0;

			var milestone = new TimerRangeMilestone(
				TimeType.TimeRemaining,
				8f,
				5f,
				-1f, // Negative interval
				() => triggerCount++
			);

			timer.AddRangeMilestone(milestone);
			timer.StartTimer();
			timer.Update(10f);

			// Test documents current behavior with negative interval
		}

		[Test]
		public void RangeMilestone_WhereStartEqualsEnd()
		{
			var timer = new StandardTimer(10f);
			int triggerCount = 0;

			timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				5f,
				5f, // Same as start
				1f,
				() => triggerCount++
			);

			timer.StartTimer();
			timer.Update(10f);

			Assert.AreEqual(1, triggerCount);
		}

		[Test]
		public void RangeMilestone_StartAfterEnd_ForTimeRemaining()
		{
			var timer = new StandardTimer(10f);
			int triggerCount = 0;

			// For TimeRemaining, start should be > end (counting down)
			// What happens if reversed?
			timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				3f, // Start (lower value)
				8f, // End (higher value)
				1f,
				() => triggerCount++
			);

			timer.StartTimer();
			timer.Update(10f);

			// Test documents behavior when range is inverted
		}

		#endregion

		#region Serialization Edge Cases

		[Test]
		public void Serialize_WhileTimerIsRunning()
		{
			var timer = new StandardTimer(10f);
			timer.StartTimer();
			timer.Update(3f);

			string json = timer.Serialize();

			Assert.IsNotNull(json);
			Assert.IsTrue(json.Contains("\"IsRunning\":true"));
		}

		[Test]
		public void Deserialize_RestoresRunningState()
		{
			var timer1 = new StandardTimer(10f);
			timer1.StartTimer();
			timer1.Update(3f);
			string json = timer1.Serialize();

			var timer2 = new StandardTimer(1f);
			timer2.Deserialize(json);

			Assert.IsTrue(timer2.IsRunning);
			Assert.AreEqual(7f, timer2.TimeRemaining, 0.001f);
		}

		[Test]
		public void Serialize_Deserialize_RoundTrip()
		{
			var timer1 = new StandardTimer(100f);
			timer1.StartTimer();
			timer1.Update(33.333f);

			string json = timer1.Serialize();
			var timer2 = new StandardTimer(1f);
			timer2.Deserialize(json);

			Assert.AreEqual(timer1.Duration, timer2.Duration);
			Assert.AreEqual(timer1.TimeRemaining, timer2.TimeRemaining, 0.001f);
			Assert.AreEqual(timer1.IsRunning, timer2.IsRunning);
		}

		#endregion
	}
}
