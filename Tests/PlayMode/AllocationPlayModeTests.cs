using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nonatomic.TimerKit;

namespace Tests.PlayMode
{
	/// <summary>
	/// PlayMode allocation tests to measure GC allocations in real Unity runtime conditions.
	/// These tests capture allocations that occur during actual Unity Update loops.
	/// </summary>
	[TestFixture]
	public class AllocationPlayModeTests
	{
		private GameObject _gameObject;
		private Timer _timer;

		[SetUp]
		public void Setup()
		{
			_gameObject = new GameObject("AllocationTestTimer");
			_timer = _gameObject.AddComponent<Timer>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}

		#region Timer MonoBehaviour Allocation Tests

		[UnityTest]
		public IEnumerator Timer_MonoBehaviour_Update_MeasureAllocations()
		{
			yield return null; // Wait for Awake

			_timer.Duration = 1000f;
			_timer.ResetTimer();
			_timer.StartTimer();

			// Warmup - let the timer run for a bit
			for (int i = 0; i < 30; i++)
			{
				yield return null;
			}

			// Force GC
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);
			int frameCount = 0;

			// Measure allocations over 60 frames (approximately 1 second)
			while (frameCount < 60)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Timer MonoBehaviour: {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame: {allocatedBytes / frameCount} bytes");
		}

		[UnityTest]
		public IEnumerator Timer_MonoBehaviour_WithOnTickSubscriber_MeasureAllocations()
		{
			yield return null;

			_timer.Duration = 1000f;
			_timer.ResetTimer();

			int tickCount = 0;
			_timer.OnTick += (t) => tickCount++;

			_timer.StartTimer();

			// Warmup
			for (int i = 0; i < 30; i++)
			{
				yield return null;
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);
			int frameCount = 0;

			while (frameCount < 60)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Timer with OnTick subscriber: {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame: {allocatedBytes / frameCount} bytes");
			Debug.Log($"Tick count: {tickCount}");
		}

		[UnityTest]
		public IEnumerator Timer_MonoBehaviour_WithMilestone_NotTriggered_MeasureAllocations()
		{
			yield return null;

			_timer.Duration = 1000f;
			_timer.ResetTimer();
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 500f, () => { }));
			_timer.StartTimer();

			// Warmup
			for (int i = 0; i < 30; i++)
			{
				yield return null;
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);
			int frameCount = 0;

			while (frameCount < 60)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Timer with milestone (not triggered): {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame: {allocatedBytes / frameCount} bytes");
		}

		[UnityTest]
		public IEnumerator Timer_MonoBehaviour_MilestoneTriggering_MeasureAllocations()
		{
			yield return null;

			_timer.Duration = 0.5f; // Short timer
			_timer.ResetTimer();

			int triggerCount = 0;
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 0.25f, () => triggerCount++));

			// Warmup by running a complete cycle
			_timer.StartTimer();
			yield return new WaitForSeconds(0.6f);

			_timer.ResetTimer();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);

			_timer.StartTimer();

			// Wait for milestone to trigger
			yield return new WaitForSeconds(0.4f);

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Timer milestone trigger: {allocatedBytes} bytes");
			Debug.Log($"Trigger count: {triggerCount}");
		}

		#endregion

		#region Multiple Timers Tests

		[UnityTest]
		public IEnumerator MultipleTimers_Update_MeasureAllocations()
		{
			yield return null;

			const int timerCount = 10;
			var timerObjects = new List<GameObject>();
			var timers = new List<Timer>();

			for (int i = 0; i < timerCount; i++)
			{
				var go = new GameObject($"Timer_{i}");
				var timer = go.AddComponent<Timer>();
				timer.Duration = 1000f;
				timerObjects.Add(go);
				timers.Add(timer);
			}

			yield return null;

			foreach (var timer in timers)
			{
				timer.ResetTimer();
				timer.StartTimer();
			}

			// Warmup
			for (int i = 0; i < 30; i++)
			{
				yield return null;
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);
			int frameCount = 0;

			while (frameCount < 60)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"{timerCount} Timer MonoBehaviours: {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame (all timers): {allocatedBytes / frameCount} bytes");
			Debug.Log($"Per timer per frame: {allocatedBytes / frameCount / timerCount} bytes");

			// Cleanup
			foreach (var go in timerObjects)
			{
				UnityEngine.Object.DestroyImmediate(go);
			}
		}

		[UnityTest]
		public IEnumerator MultipleTimers_WithMilestones_Update_MeasureAllocations()
		{
			yield return null;

			const int timerCount = 10;
			var timerObjects = new List<GameObject>();
			var timers = new List<Timer>();

			for (int i = 0; i < timerCount; i++)
			{
				var go = new GameObject($"Timer_{i}");
				var timer = go.AddComponent<Timer>();
				timer.Duration = 1000f;
				timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 500f, () => { }));
				timerObjects.Add(go);
				timers.Add(timer);
			}

			yield return null;

			foreach (var timer in timers)
			{
				timer.ResetTimer();
				timer.StartTimer();
			}

			// Warmup
			for (int i = 0; i < 30; i++)
			{
				yield return null;
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);
			int frameCount = 0;

			while (frameCount < 60)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"{timerCount} Timers with milestones: {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame (all timers): {allocatedBytes / frameCount} bytes");
			Debug.Log($"Per timer per frame: {allocatedBytes / frameCount / timerCount} bytes");

			// Cleanup
			foreach (var go in timerObjects)
			{
				UnityEngine.Object.DestroyImmediate(go);
			}
		}

		#endregion

		#region Event Forwarding Allocation Tests

		[UnityTest]
		public IEnumerator Timer_HandleTimerTick_EventForwarding_MeasureAllocations()
		{
			yield return null;

			_timer.Duration = 1000f;
			_timer.ResetTimer();

			// Subscribe to multiple events
			int startCount = 0, tickCount = 0, stopCount = 0;
			_timer.OnStart += () => startCount++;
			_timer.OnTick += (t) => tickCount++;
			_timer.OnStop += () => stopCount++;

			_timer.StartTimer();

			// Warmup
			for (int i = 0; i < 30; i++)
			{
				yield return null;
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(false);
			int frameCount = 0;

			while (frameCount < 60)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(false);
			long allocatedBytes = endMemory - startMemory;

			Debug.Log($"Timer with event subscribers: {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame: {allocatedBytes / frameCount} bytes");
			Debug.Log($"Events fired - Start: {startCount}, Tick: {tickCount}, Stop: {stopCount}");
		}

		#endregion

		#region Baseline Tests for CI

		[UnityTest]
		public IEnumerator Baseline_Timer_MonoBehaviour_CurrentAllocation()
		{
			yield return null;

			_timer.Duration = 1000f;
			_timer.ResetTimer();
			_timer.StartTimer();

			// Warmup
			for (int i = 0; i < 60; i++)
			{
				yield return null;
			}

			_timer.ResetTimer();
			_timer.StartTimer();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(true);
			int frameCount = 0;

			// Measure over 120 frames (approximately 2 seconds)
			while (frameCount < 120)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerFrame = allocatedBytes / frameCount;

			Debug.Log($"BASELINE - Timer MonoBehaviour: {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame: {bytesPerFrame} bytes");

			// Document current allocation level
			// After optimization, this can be converted to an assertion
		}

		[UnityTest]
		public IEnumerator Baseline_Timer_WithMilestone_CurrentAllocation()
		{
			yield return null;

			_timer.Duration = 1000f;
			_timer.ResetTimer();
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 500f, () => { }));
			_timer.StartTimer();

			// Warmup
			for (int i = 0; i < 60; i++)
			{
				yield return null;
			}

			_timer.ResetTimer();
			_timer.StartTimer();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long startMemory = GC.GetTotalMemory(true);
			int frameCount = 0;

			while (frameCount < 120)
			{
				yield return null;
				frameCount++;
			}

			long endMemory = GC.GetTotalMemory(true);
			long allocatedBytes = endMemory - startMemory;
			long bytesPerFrame = allocatedBytes / frameCount;

			Debug.Log($"BASELINE - Timer with milestone: {allocatedBytes} bytes over {frameCount} frames");
			Debug.Log($"Per frame: {bytesPerFrame} bytes");

			// Document current allocation level
			// Target after optimization: 0 bytes per frame when milestone doesn't trigger
		}

		#endregion
	}
}
