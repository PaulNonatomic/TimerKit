using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using Nonatomic.TimerKit;

namespace Tests.EditMode
{
	/// <summary>
	/// Tests to verify Timer methods can be called at various Unity lifecycle points without null reference errors.
	/// These tests ensure the lazy initialization pattern protects against Unity's unpredictable execution order.
	/// </summary>
	[TestFixture]
	public class TimerLifecycleTests
	{
		private GameObject _gameObject;
		private Timer _timer;

		[SetUp]
		public void Setup()
		{
			_gameObject = new GameObject("LifecycleTestTimer");
			_timer = _gameObject.AddComponent<Timer>();
			// Note: We deliberately do NOT call Awake here to test pre-initialization calls
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}

		#region ResetTimer Tests

		[Test]
		public void ResetTimer_BeforeAwake_DoesNotThrowNullReference()
		{
			// Arrange: Timer component exists but Awake hasn't been called yet

			// Act & Assert: Should not throw
			Assert.DoesNotThrow(() => _timer.ResetTimer(),
				"ResetTimer should not throw before Awake is called");
		}

		[Test]
		public void ResetTimer_AfterAwake_DoesNotThrowNullReference()
		{
			// Arrange: Manually call Awake
			CallAwake(_timer);

			// Act & Assert: Should not throw
			Assert.DoesNotThrow(() => _timer.ResetTimer(),
				"ResetTimer should not throw after Awake is called");
		}

		[Test]
		public void ResetTimer_AfterOnEnable_DoesNotThrowNullReference()
		{
			// Arrange: Call OnEnable (which can run before Awake in some scenarios)
			CallOnEnable(_timer);

			// Act & Assert: Should not throw
			Assert.DoesNotThrow(() => _timer.ResetTimer(),
				"ResetTimer should not throw after OnEnable is called");
		}

		[Test]
		public void ResetTimer_MultipleCallsInSequence_DoesNotThrow()
		{
			// Act & Assert: Multiple calls should all succeed
			Assert.DoesNotThrow(() => _timer.ResetTimer(), "First ResetTimer call");
			Assert.DoesNotThrow(() => _timer.ResetTimer(), "Second ResetTimer call");

			CallAwake(_timer);

			Assert.DoesNotThrow(() => _timer.ResetTimer(), "Third ResetTimer call after Awake");
			Assert.DoesNotThrow(() => _timer.ResetTimer(), "Fourth ResetTimer call");
		}

		#endregion

		#region StartTimer Tests

		[Test]
		public void StartTimer_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.StartTimer(),
				"StartTimer should not throw before Awake is called");
		}

		[Test]
		public void StartTimer_AfterOnEnable_DoesNotThrowNullReference()
		{
			// Arrange
			CallOnEnable(_timer);

			// Act & Assert
			Assert.DoesNotThrow(() => _timer.StartTimer(),
				"StartTimer should not throw after OnEnable is called");
		}

		#endregion

		#region ResumeTimer Tests

		[Test]
		public void ResumeTimer_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.ResumeTimer(),
				"ResumeTimer should not throw before Awake is called");
		}

		[Test]
		public void ResumeTimer_AfterOnEnable_DoesNotThrowNullReference()
		{
			// Arrange
			CallOnEnable(_timer);

			// Act & Assert
			Assert.DoesNotThrow(() => _timer.ResumeTimer(),
				"ResumeTimer should not throw after OnEnable is called");
		}

		#endregion

		#region StopTimer Tests

		[Test]
		public void StopTimer_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.StopTimer(),
				"StopTimer should not throw before Awake is called");
		}

		[Test]
		public void StopTimer_AfterOnEnable_DoesNotThrowNullReference()
		{
			// Arrange
			CallOnEnable(_timer);

			// Act & Assert
			Assert.DoesNotThrow(() => _timer.StopTimer(),
				"StopTimer should not throw after OnEnable is called");
		}

		#endregion

		#region FastForward Tests

		[Test]
		public void FastForward_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.FastForward(5f),
				"FastForward should not throw before Awake is called");
		}

		[Test]
		public void FastForward_AfterOnEnable_DoesNotThrowNullReference()
		{
			// Arrange
			CallOnEnable(_timer);

			// Act & Assert
			Assert.DoesNotThrow(() => _timer.FastForward(5f),
				"FastForward should not throw after OnEnable is called");
		}

		#endregion

		#region Rewind Tests

		[Test]
		public void Rewind_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.Rewind(5f),
				"Rewind should not throw before Awake is called");
		}

		[Test]
		public void Rewind_AfterOnEnable_DoesNotThrowNullReference()
		{
			// Arrange
			CallOnEnable(_timer);

			// Act & Assert
			Assert.DoesNotThrow(() => _timer.Rewind(5f),
				"Rewind should not throw after OnEnable is called");
		}

		#endregion

		#region Property Access Tests

		[Test]
		public void PropertyAccess_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert: All property accesses should work
			Assert.DoesNotThrow(() => { var _ = _timer.IsRunning; }, "IsRunning property access");
			Assert.DoesNotThrow(() => { var _ = _timer.Duration; }, "Duration property get");
			Assert.DoesNotThrow(() => { _timer.Duration = 15f; }, "Duration property set");
			Assert.DoesNotThrow(() => { var _ = _timer.TimeRemaining; }, "TimeRemaining property access");
			Assert.DoesNotThrow(() => { var _ = _timer.TimeElapsed; }, "TimeElapsed property access");
			Assert.DoesNotThrow(() => { var _ = _timer.ProgressElapsed; }, "ProgressElapsed property access");
			Assert.DoesNotThrow(() => { var _ = _timer.ProgressRemaining; }, "ProgressRemaining property access");
		}

		[Test]
		public void PropertyAccess_AfterOnEnable_DoesNotThrowNullReference()
		{
			// Arrange
			CallOnEnable(_timer);

			// Act & Assert
			Assert.DoesNotThrow(() => { var _ = _timer.IsRunning; }, "IsRunning property access");
			Assert.DoesNotThrow(() => { var _ = _timer.Duration; }, "Duration property get");
			Assert.DoesNotThrow(() => { _timer.Duration = 15f; }, "Duration property set");
			Assert.DoesNotThrow(() => { var _ = _timer.TimeRemaining; }, "TimeRemaining property access");
			Assert.DoesNotThrow(() => { var _ = _timer.TimeElapsed; }, "TimeElapsed property access");
			Assert.DoesNotThrow(() => { var _ = _timer.ProgressElapsed; }, "ProgressElapsed property access");
			Assert.DoesNotThrow(() => { var _ = _timer.ProgressRemaining; }, "ProgressRemaining property access");
		}

		#endregion

		#region Milestone Tests

		[Test]
		public void AddMilestone_BeforeAwake_DoesNotThrowNullReference()
		{
			// Arrange
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 5f, () => { });

			// Act & Assert
			Assert.DoesNotThrow(() => _timer.AddMilestone(milestone),
				"AddMilestone should not throw before Awake is called");
		}

		[Test]
		public void AddRangeMilestone_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.AddRangeMilestone(TimeType.TimeRemaining, 10f, 0f, 1f, () => { }),
				"AddRangeMilestone should not throw before Awake is called");
		}

		[Test]
		public void RemoveMilestone_BeforeAwake_DoesNotThrowNullReference()
		{
			// Arrange
			var milestone = new TimerMilestone(TimeType.TimeRemaining, 5f, () => { });
			_timer.AddMilestone(milestone);

			// Act & Assert
			Assert.DoesNotThrow(() => _timer.RemoveMilestone(milestone),
				"RemoveMilestone should not throw before Awake is called");
		}

		[Test]
		public void RemoveAllMilestones_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.RemoveAllMilestones(),
				"RemoveAllMilestones should not throw before Awake is called");
		}

		[Test]
		public void RemoveMilestonesByCondition_BeforeAwake_DoesNotThrowNullReference()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _timer.RemoveMilestonesByCondition(m => true),
				"RemoveMilestonesByCondition should not throw before Awake is called");
		}

		#endregion

		#region Functional Verification Tests

		[Test]
		public void Timer_FunctionsCorrectly_WhenUsedBeforeAwake()
		{
			// Test that timer actually works when methods are called before Awake

			// Set duration and start timer
			_timer.Duration = 10f;
			_timer.StartTimer();

			Assert.IsTrue(_timer.IsRunning, "Timer should be running");
			Assert.AreEqual(10f, _timer.TimeRemaining, 0.01f, "Timer should have correct duration");

			// Now call Awake (simulating Unity's lifecycle)
			CallAwake(_timer);

			// Timer should still work correctly
			Assert.IsTrue(_timer.IsRunning, "Timer should still be running after Awake");
			Assert.AreEqual(10f, _timer.TimeRemaining, 0.01f, "Timer should maintain correct duration");
		}

		[Test]
		public void Timer_CanBeReset_AtAnyLifecyclePoint()
		{
			// Test reset before Awake
			_timer.Duration = 20f;
			_timer.ResetTimer();
			Assert.AreEqual(20f, _timer.TimeRemaining, 0.01f, "Reset before Awake should work");

			// Call OnEnable
			CallOnEnable(_timer);
			_timer.ResetTimer();
			Assert.AreEqual(20f, _timer.TimeRemaining, 0.01f, "Reset after OnEnable should work");

			// Call Awake
			CallAwake(_timer);
			_timer.ResetTimer();
			Assert.AreEqual(20f, _timer.TimeRemaining, 0.01f, "Reset after Awake should work");

			// Start and reset
			_timer.StartTimer();
			_timer.ResetTimer();
			Assert.AreEqual(20f, _timer.TimeRemaining, 0.01f, "Reset after StartTimer should work");
			Assert.IsFalse(_timer.IsRunning, "Timer should not be running after reset");
		}

		[Test]
		public void Timer_OnEnableCalledBeforeAwake_WorksCorrectly()
		{
			// Simulate Unity's unpredictable lifecycle where OnEnable runs first

			// Call OnEnable first (before Awake)
			CallOnEnable(_timer);

			// Use the timer
			_timer.Duration = 15f;
			_timer.StartTimer();

			Assert.IsTrue(_timer.IsRunning, "Timer should work when OnEnable is called before Awake");
			Assert.AreEqual(15f, _timer.TimeRemaining, 0.01f);

			// Now call Awake
			CallAwake(_timer);

			// Timer should still work
			Assert.IsTrue(_timer.IsRunning, "Timer should still work after Awake is called later");
			_timer.ResetTimer();
			Assert.IsFalse(_timer.IsRunning, "Reset should work correctly");
		}

		#endregion

		#region Helper Methods

		private void CallAwake(Timer timer)
		{
			var awakeMethod = typeof(Timer).GetMethod("Awake",
				BindingFlags.NonPublic | BindingFlags.Instance);
			awakeMethod?.Invoke(timer, null);
		}

		private void CallOnEnable(Timer timer)
		{
			var onEnableMethod = typeof(Timer).GetMethod("OnEnable",
				BindingFlags.NonPublic | BindingFlags.Instance);
			onEnableMethod?.Invoke(timer, null);
		}

		#endregion
	}
}
