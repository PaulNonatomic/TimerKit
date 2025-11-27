using NUnit.Framework;
using System.Collections.Generic;
using Nonatomic.TimerKit;

namespace Tests.EditMode
{
	[TestFixture]
	public class ProgressMilestoneTests
	{
		private StandardTimer _timer;

		[SetUp]
		public void Setup()
		{
			_timer = new StandardTimer(10f);
		}

		#region ProgressRemaining Milestone Tests

		[Test]
		public void ProgressRemaining_Milestone_TriggersAtCorrectTime()
		{
			bool triggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.5f, () => triggered = true));
			_timer.StartTimer();
			_timer.Update(5f); // 50% remaining

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressRemaining_Milestone_TriggersAt100Percent()
		{
			bool triggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 1.0f, () => triggered = true));
			_timer.StartTimer();

			Assert.IsFalse(triggered, "Should not trigger before reaching 100%");

			// Timer starts at 100% remaining, needs small update to cross
			_timer.Update(0.1f);

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressRemaining_Milestone_TriggersAtZeroPercent()
		{
			bool triggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0f, () => triggered = true));
			_timer.StartTimer();
			_timer.Update(10f); // Complete timer

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressRemaining_MultipleMilestones_AllTrigger()
		{
			var triggered = new List<float>();

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.7f, () => triggered.Add(0.7f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.5f, () => triggered.Add(0.5f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.3f, () => triggered.Add(0.3f)));

			_timer.StartTimer();
			_timer.Update(10f); // Complete timer

			Assert.AreEqual(3, triggered.Count, "All three milestones should trigger");
			Assert.Contains(0.7f, triggered);
			Assert.Contains(0.5f, triggered);
			Assert.Contains(0.3f, triggered);
		}

		[Test]
		public void ProgressRemaining_WithLargeUpdate_TriggersAllCrossedMilestones()
		{
			var triggered = new List<float>();

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.8f, () => triggered.Add(0.8f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.5f, () => triggered.Add(0.5f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.2f, () => triggered.Add(0.2f)));

			_timer.StartTimer();
			_timer.Update(8f); // Jump from 100% to 20% remaining

			Assert.AreEqual(3, triggered.Count);
		}

		#endregion

		#region ProgressElapsed Milestone Tests

		[Test]
		public void ProgressElapsed_Milestone_TriggersAtCorrectTime()
		{
			bool triggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.5f, () => triggered = true));
			_timer.StartTimer();
			_timer.Update(5f); // 50% elapsed

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressElapsed_Milestone_TriggersAtZeroPercent()
		{
			bool triggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0f, () => triggered = true));
			_timer.StartTimer();
			_timer.Update(0.1f);

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressElapsed_Milestone_TriggersAt100Percent()
		{
			bool triggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 1.0f, () => triggered = true));
			_timer.StartTimer();
			_timer.Update(10f);

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressElapsed_MultipleMilestones_TriggerInOrder()
		{
			var triggerOrder = new List<float>();

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.3f, () => triggerOrder.Add(0.3f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.5f, () => triggerOrder.Add(0.5f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.7f, () => triggerOrder.Add(0.7f)));

			_timer.StartTimer();
			_timer.Update(10f);

			Assert.AreEqual(3, triggerOrder.Count);
			Assert.AreEqual(0.3f, triggerOrder[0]);
			Assert.AreEqual(0.5f, triggerOrder[1]);
			Assert.AreEqual(0.7f, triggerOrder[2]);
		}

		#endregion

		#region Progress-Based Range Milestone Tests

		[Test]
		public void ProgressRemaining_RangeMilestone_TriggersAtIntervals()
		{
			int triggerCount = 0;

			_timer.AddRangeMilestone(
				TimeType.ProgressRemaining,
				0.8f, // Start at 80% remaining
				0.2f, // End at 20% remaining
				0.2f, // Every 20%
				() => triggerCount++
			);

			_timer.StartTimer();
			_timer.Update(10f);

			// Should trigger at 0.8, 0.6, 0.4, 0.2 = 4 times
			Assert.AreEqual(4, triggerCount, "Should trigger 4 times at 0.8, 0.6, 0.4, 0.2");
		}

		[Test]
		public void ProgressElapsed_RangeMilestone_TriggersAtIntervals()
		{
			int triggerCount = 0;

			_timer.AddRangeMilestone(
				TimeType.ProgressElapsed,
				0.2f, // Start at 20% elapsed
				0.8f, // End at 80% elapsed
				0.2f, // Every 20%
				() => triggerCount++
			);

			_timer.StartTimer();
			_timer.Update(10f);

			// Should trigger at 0.2, 0.4, 0.6, 0.8 = 4 times
			Assert.AreEqual(4, triggerCount, "Should trigger 4 times at 0.2, 0.4, 0.6, 0.8");
		}

		[Test]
		public void ProgressRemaining_RangeMilestone_DoesNotTriggerOutsideRange()
		{
			int triggerCount = 0;

			_timer.AddRangeMilestone(
				TimeType.ProgressRemaining,
				0.6f, // Start at 60% remaining
				0.4f, // End at 40% remaining
				0.1f, // Every 10%
				() => triggerCount++
			);

			_timer.StartTimer();
			_timer.Update(10f);

			// Should trigger at 0.6, 0.5, 0.4 = 3 times
			Assert.AreEqual(3, triggerCount);
		}

		[Test]
		public void ProgressElapsed_RangeMilestone_WithSmallInterval()
		{
			var triggerValues = new List<float>();

			_timer.AddRangeMilestone(
				TimeType.ProgressElapsed,
				0.1f,
				0.3f,
				0.05f,
				() => triggerValues.Add(_timer.ProgressElapsed)
			);

			_timer.StartTimer();
			_timer.Update(5f); // 50% elapsed

			// Should trigger at 0.1, 0.15, 0.2, 0.25, 0.3 = 5 times
			Assert.AreEqual(5, triggerValues.Count);
		}

		#endregion

		#region Mixed Progress Type Tests

		[Test]
		public void MixedProgressTypes_BothTriggerCorrectly()
		{
			bool progressElapsedTriggered = false;
			bool progressRemainingTriggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.5f, () => progressElapsedTriggered = true));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.5f, () => progressRemainingTriggered = true));

			_timer.StartTimer();
			_timer.Update(5f); // 50% elapsed, 50% remaining

			Assert.IsTrue(progressElapsedTriggered);
			Assert.IsTrue(progressRemainingTriggered);
		}

		[Test]
		public void AllFourTimeTypes_TriggersAtMidpoint()
		{
			bool timeRemainingTriggered = false;
			bool timeElapsedTriggered = false;
			bool progressElapsedTriggered = false;
			bool progressRemainingTriggered = false;

			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => timeRemainingTriggered = true));
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeElapsed, 5f, () => timeElapsedTriggered = true));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.5f, () => progressElapsedTriggered = true));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.5f, () => progressRemainingTriggered = true));

			_timer.StartTimer();
			_timer.Update(5f);

			Assert.IsTrue(timeRemainingTriggered, "TimeRemaining milestone should trigger");
			Assert.IsTrue(timeElapsedTriggered, "TimeElapsed milestone should trigger");
			Assert.IsTrue(progressElapsedTriggered, "ProgressElapsed milestone should trigger");
			Assert.IsTrue(progressRemainingTriggered, "ProgressRemaining milestone should trigger");
		}

		#endregion

		#region Recurring Progress Milestone Tests

		[Test]
		public void ProgressRemaining_RecurringMilestone_TriggersEveryRound()
		{
			int triggerCount = 0;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.5f, () => triggerCount++, isRecurring: true));

			// Round 1
			_timer.StartTimer();
			_timer.Update(5f);
			Assert.AreEqual(1, triggerCount);

			// Round 2
			_timer.ResetTimer();
			_timer.StartTimer();
			_timer.Update(5f);
			Assert.AreEqual(2, triggerCount);

			// Round 3
			_timer.ResetTimer();
			_timer.StartTimer();
			_timer.Update(5f);
			Assert.AreEqual(3, triggerCount);
		}

		[Test]
		public void ProgressElapsed_RecurringRangeMilestone_TriggersEveryRound()
		{
			int triggerCount = 0;

			_timer.AddRangeMilestone(
				TimeType.ProgressElapsed,
				0.25f,
				0.75f,
				0.25f,
				() => triggerCount++,
				isRecurring: true
			);

			// Round 1 - triggers at 0.25, 0.5, 0.75 = 3 times
			_timer.StartTimer();
			_timer.Update(10f);
			Assert.AreEqual(3, triggerCount);

			// Round 2 - another 3 times
			_timer.ResetTimer();
			_timer.StartTimer();
			_timer.Update(10f);
			Assert.AreEqual(6, triggerCount);
		}

		#endregion

		#region Edge Case Tests

		[Test]
		public void ProgressMilestone_WithShortDuration_Works()
		{
			var shortTimer = new StandardTimer(0.5f);
			bool triggered = false;

			shortTimer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.5f, () => triggered = true));
			shortTimer.StartTimer();
			shortTimer.Update(0.25f);

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressMilestone_WithLongDuration_Works()
		{
			var longTimer = new StandardTimer(3600f); // 1 hour
			bool triggered = false;

			longTimer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.5f, () => triggered = true));
			longTimer.StartTimer();
			longTimer.Update(1800f); // 30 minutes

			Assert.IsTrue(triggered);
		}

		[Test]
		public void ProgressRemaining_Milestone_OnlyTriggersOnce()
		{
			int triggerCount = 0;

			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressRemaining, 0.5f, () => triggerCount++));

			_timer.StartTimer();
			_timer.Update(1f); // 90% remaining
			_timer.Update(1f); // 80% remaining
			_timer.Update(1f); // 70% remaining
			_timer.Update(1f); // 60% remaining
			_timer.Update(1f); // 50% remaining - TRIGGER
			_timer.Update(1f); // 40% remaining
			_timer.Update(1f); // 30% remaining

			Assert.AreEqual(1, triggerCount);
		}

		[Test]
		public void ProgressMilestone_BoundaryPrecision()
		{
			var triggered = new List<float>();

			// Add milestones at exact boundaries
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.0f, () => triggered.Add(0f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.25f, () => triggered.Add(0.25f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.5f, () => triggered.Add(0.5f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 0.75f, () => triggered.Add(0.75f)));
			_timer.AddMilestone(new TimerMilestone(TimeType.ProgressElapsed, 1.0f, () => triggered.Add(1.0f)));

			_timer.StartTimer();

			// Update in exact quarters
			_timer.Update(2.5f); // 25%
			_timer.Update(2.5f); // 50%
			_timer.Update(2.5f); // 75%
			_timer.Update(2.5f); // 100%

			Assert.AreEqual(5, triggered.Count);
		}

		#endregion
	}
}
