using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nonatomic.TimerKit;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tests.EditMode
{
	[TestFixture]
	public class PerformanceTests
	{
		#region Timer Creation Performance

		[Test]
		public void Performance_CreateManyTimers()
		{
			const int timerCount = 10000;
			var timers = new List<StandardTimer>(timerCount);

			var stopwatch = Stopwatch.StartNew();

			for (int i = 0; i < timerCount; i++)
			{
				timers.Add(new StandardTimer(100f));
			}

			stopwatch.Stop();

			Debug.Log($"Created {timerCount} timers in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Creating 10000 timers should take less than 1 second");
		}

		[Test]
		public void Performance_CreateManyBasicTimers()
		{
			const int timerCount = 10000;
			var timers = new List<BasicTimer>(timerCount);

			var stopwatch = Stopwatch.StartNew();

			for (int i = 0; i < timerCount; i++)
			{
				timers.Add(new BasicTimer(100f));
			}

			stopwatch.Stop();

			Debug.Log($"Created {timerCount} BasicTimers in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Creating 10000 BasicTimers should take less than 500ms");
		}

		#endregion

		#region Timer Update Performance

		[Test]
		public void Performance_UpdateManyTimers()
		{
			const int timerCount = 1000;
			const int updateCount = 1000;

			var timers = new List<StandardTimer>(timerCount);
			for (int i = 0; i < timerCount; i++)
			{
				var timer = new StandardTimer(10000f);
				timer.StartTimer();
				timers.Add(timer);
			}

			var stopwatch = Stopwatch.StartNew();

			for (int u = 0; u < updateCount; u++)
			{
				foreach (var timer in timers)
				{
					timer.Update(0.016f); // ~60fps
				}
			}

			stopwatch.Stop();

			Debug.Log($"Updated {timerCount} timers {updateCount} times in {stopwatch.ElapsedMilliseconds}ms");
			Debug.Log($"Average: {(double)stopwatch.ElapsedMilliseconds / updateCount}ms per frame for {timerCount} timers");

			// 1000 timers * 1000 updates = 1 million updates should be fast
			Assert.Less(stopwatch.ElapsedMilliseconds, 2000, "1 million timer updates should take less than 2 seconds");
		}

		[Test]
		public void Performance_UpdateBasicTimers_VsStandardTimers()
		{
			const int timerCount = 1000;
			const int updateCount = 1000;

			// BasicTimer
			var basicTimers = new List<BasicTimer>(timerCount);
			for (int i = 0; i < timerCount; i++)
			{
				var timer = new BasicTimer(10000f);
				timer.StartTimer();
				basicTimers.Add(timer);
			}

			var basicStopwatch = Stopwatch.StartNew();
			for (int u = 0; u < updateCount; u++)
			{
				foreach (var timer in basicTimers)
				{
					timer.Update(0.016f);
				}
			}
			basicStopwatch.Stop();

			// StandardTimer
			var standardTimers = new List<StandardTimer>(timerCount);
			for (int i = 0; i < timerCount; i++)
			{
				var timer = new StandardTimer(10000f);
				timer.StartTimer();
				standardTimers.Add(timer);
			}

			var standardStopwatch = Stopwatch.StartNew();
			for (int u = 0; u < updateCount; u++)
			{
				foreach (var timer in standardTimers)
				{
					timer.Update(0.016f);
				}
			}
			standardStopwatch.Stop();

			Debug.Log($"BasicTimer: {basicStopwatch.ElapsedMilliseconds}ms");
			Debug.Log($"StandardTimer: {standardStopwatch.ElapsedMilliseconds}ms");

			// StandardTimer has milestone processing overhead even without milestones
			// Just verify both complete in reasonable time
			Assert.Less(basicStopwatch.ElapsedMilliseconds, 5000, "BasicTimer updates should complete in under 5 seconds");
			Assert.Less(standardStopwatch.ElapsedMilliseconds, 5000, "StandardTimer updates should complete in under 5 seconds");
		}

		#endregion

		#region Milestone Performance

		[Test]
		public void Performance_ManyMilestones()
		{
			var timer = new StandardTimer(1000f);
			const int milestoneCount = 1000;

			var stopwatch = Stopwatch.StartNew();

			for (int i = 1; i <= milestoneCount; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, i * 0.99f, () => { }));
			}

			stopwatch.Stop();
			Debug.Log($"Added {milestoneCount} milestones in {stopwatch.ElapsedMilliseconds}ms");

			Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Adding 1000 milestones should take less than 500ms");

			// Now test update performance with many milestones
			timer.StartTimer();

			stopwatch.Restart();
			timer.Update(1000f); // Trigger all milestones
			stopwatch.Stop();

			Debug.Log($"Triggered {milestoneCount} milestones in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Triggering 1000 milestones should take less than 500ms");
		}

		[Test]
		public void Performance_ManyRangeMilestones()
		{
			var timer = new StandardTimer(100f);
			const int rangeMilestoneCount = 100;

			for (int i = 0; i < rangeMilestoneCount; i++)
			{
				timer.AddRangeMilestone(
					TimeType.TimeRemaining,
					99f,
					1f,
					1f,
					() => { }
				);
			}

			timer.StartTimer();

			var stopwatch = Stopwatch.StartNew();
			timer.Update(100f); // Complete timer, triggering all range milestones
			stopwatch.Stop();

			// Each range milestone triggers ~99 times, so 100 * 99 = 9900 triggers
			Debug.Log($"Completed timer with {rangeMilestoneCount} range milestones in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "100 range milestones should complete in under 1 second");
		}

		[Test]
		public void Performance_MilestoneAddRemoveDuringCallback()
		{
			var timer = new StandardTimer(100f);
			int dynamicMilestoneCount = 0;

			// Add milestone that adds more milestones
			timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 90f, () =>
			{
				for (int i = 0; i < 100; i++)
				{
					timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 80f - i * 0.1f, () => dynamicMilestoneCount++));
				}
			}));

			timer.StartTimer();

			var stopwatch = Stopwatch.StartNew();
			timer.Update(100f);
			stopwatch.Stop();

			Debug.Log($"Dynamic milestone add during callback: {dynamicMilestoneCount} milestones triggered in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Dynamic milestone operations should be fast");
		}

		#endregion

		#region Memory Performance

		[Test]
		public void Performance_MemoryUsage_ManyTimers()
		{
			// Force GC to get a clean baseline
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long memoryBefore = GC.GetTotalMemory(true);

			const int timerCount = 10000;
			var timers = new List<StandardTimer>(timerCount);
			for (int i = 0; i < timerCount; i++)
			{
				timers.Add(new StandardTimer(100f));
			}

			long memoryAfter = GC.GetTotalMemory(true);
			long memoryUsed = memoryAfter - memoryBefore;

			Debug.Log($"Memory used for {timerCount} timers: {memoryUsed / 1024}KB ({memoryUsed / timerCount} bytes per timer)");

			// Rough estimate - each timer shouldn't use more than 1KB
			Assert.Less(memoryUsed / timerCount, 1024, "Each timer should use less than 1KB of memory");
		}

		[Test]
		public void Performance_MemoryUsage_ManyMilestones()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long memoryBefore = GC.GetTotalMemory(true);

			var timer = new StandardTimer(10000f);
			const int milestoneCount = 10000;
			for (int i = 0; i < milestoneCount; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, i * 0.99f, () => { }));
			}

			long memoryAfter = GC.GetTotalMemory(true);
			long memoryUsed = memoryAfter - memoryBefore;

			Debug.Log($"Memory used for {milestoneCount} milestones: {memoryUsed / 1024}KB ({memoryUsed / milestoneCount} bytes per milestone)");

			// Each milestone shouldn't use more than 500 bytes
			Assert.Less(memoryUsed / milestoneCount, 500, "Each milestone should use less than 500 bytes");
		}

		#endregion

		#region Callback Performance

		[Test]
		public void Performance_MilestoneCallback_Overhead()
		{
			var timer = new StandardTimer(10f);
			int callbackCount = 0;
			Action callback = () => callbackCount++;

			// Add 100 milestones with the same callback
			for (int i = 1; i <= 100; i++)
			{
				timer.AddMilestone(new TimerMilestone(TimeType.TimeElapsed, i * 0.09f, callback));
			}

			timer.StartTimer();

			var stopwatch = Stopwatch.StartNew();
			timer.Update(10f);
			stopwatch.Stop();

			Debug.Log($"Triggered {callbackCount} callbacks in {stopwatch.ElapsedMilliseconds}ms");
			Assert.AreEqual(100, callbackCount);
			Assert.Less(stopwatch.ElapsedMilliseconds, 100, "100 callbacks should execute in under 100ms");
		}

		[Test]
		public void Performance_EventSubscribers()
		{
			var timer = new StandardTimer(10f);
			int tickCount = 0;

			// Add many subscribers
			for (int i = 0; i < 100; i++)
			{
				timer.OnTick += (t) => tickCount++;
			}

			timer.StartTimer();

			var stopwatch = Stopwatch.StartNew();
			for (int u = 0; u < 1000; u++)
			{
				timer.Update(0.001f);
			}
			stopwatch.Stop();

			Debug.Log($"1000 updates with 100 OnTick subscribers: {stopwatch.ElapsedMilliseconds}ms ({tickCount} total callbacks)");
			Assert.AreEqual(100000, tickCount);
			Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Event invocation should be fast");
		}

		#endregion

		#region Serialization Performance

		[Test]
		public void Performance_Serialization()
		{
			var timer = new StandardTimer(100f);
			timer.StartTimer();
			timer.Update(33.33f);

			const int serializeCount = 10000;

			var stopwatch = Stopwatch.StartNew();
			for (int i = 0; i < serializeCount; i++)
			{
				string json = timer.Serialize();
			}
			stopwatch.Stop();

			Debug.Log($"Serialized timer {serializeCount} times in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "10000 serializations should complete in under 1 second");
		}

		[Test]
		public void Performance_Deserialization()
		{
			var timer = new StandardTimer(100f);
			timer.StartTimer();
			timer.Update(33.33f);
			string json = timer.Serialize();

			const int deserializeCount = 10000;

			var stopwatch = Stopwatch.StartNew();
			for (int i = 0; i < deserializeCount; i++)
			{
				timer.Deserialize(json);
			}
			stopwatch.Stop();

			Debug.Log($"Deserialized timer {deserializeCount} times in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "10000 deserializations should complete in under 1 second");
		}

		#endregion

		#region TimerUtility Performance

		[Test]
		public void Performance_TimerUtility_Formatting()
		{
			const int formatCount = 100000;

			var stopwatch = Stopwatch.StartNew();
			for (int i = 0; i < formatCount; i++)
			{
				TimerUtility.FormatHHMMSSFFF(3661.123);
			}
			stopwatch.Stop();

			Debug.Log($"Formatted time {formatCount} times in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "100000 format operations should complete in under 1 second");
		}

		[Test]
		public void Performance_TimerUtility_TimeCalculations()
		{
			const int calcCount = 1000000;
			double total = 0;

			var stopwatch = Stopwatch.StartNew();
			for (int i = 0; i < calcCount; i++)
			{
				total += TimerUtility.Hours(3661.123);
				total += TimerUtility.Minutes(3661.123);
				total += TimerUtility.Seconds(3661.123);
				total += TimerUtility.Milliseconds(3661.123);
			}
			stopwatch.Stop();

			Debug.Log($"Performed {calcCount * 4} time calculations in {stopwatch.ElapsedMilliseconds}ms");
			Assert.Less(stopwatch.ElapsedMilliseconds, 500, "4 million calculations should complete in under 500ms");
			Assert.Greater(total, 0); // Prevent optimization from removing the calculations
		}

		#endregion
	}
}
