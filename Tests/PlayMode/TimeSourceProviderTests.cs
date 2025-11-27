using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nonatomic.TimerKit;
using LogType = UnityEngine.LogType;

namespace Tests.PlayMode
{
	/// <summary>
	/// A mock TimeSourceProvider for testing
	/// </summary>
	public class MockTimeSourceProvider : TimeSourceProvider
	{
		public float MockTime = 10f;
		public bool AllowSetTime = true;

		public override bool CanSetTime => AllowSetTime;

		public override float GetTimeRemaining() => MockTime;

		public override void SetTimeRemaining(float timeRemaining)
		{
			if (AllowSetTime)
			{
				MockTime = timeRemaining;
			}
		}
	}

	/// <summary>
	/// A read-only TimeSourceProvider for testing
	/// </summary>
	public class ReadOnlyTimeSourceProvider : TimeSourceProvider
	{
		public float MockTime = 15f;

		public override bool CanSetTime => false;

		public override float GetTimeRemaining() => MockTime;

		public override void SetTimeRemaining(float timeRemaining)
		{
			// Read-only - do nothing
		}
	}

	[TestFixture]
	public class TimeSourceProviderTests
	{
		private GameObject _gameObject;
		private Timer _timer;

		[SetUp]
		public void Setup()
		{
			_gameObject = new GameObject("TestTimerWithTimeSource");
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				Object.DestroyImmediate(_gameObject);
			}
		}

		#region Connection Tests

		[UnityTest]
		public IEnumerator TimeSourceProvider_ConnectsToTimerOnAwake()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<MockTimeSourceProvider>();

			yield return null; // Wait for Awake

			// The timer should use the time source's value
			Assert.AreEqual(10f, _timer.TimeRemaining);
		}

		[UnityTest]
		public IEnumerator TimeSourceProvider_TimerUsesTimeSourceValue()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<MockTimeSourceProvider>();
			timeSource.MockTime = 25f;

			yield return null;

			Assert.AreEqual(25f, _timer.TimeRemaining);
		}

		[UnityTest]
		public IEnumerator TimeSourceProvider_UpdatesTimeSource()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<MockTimeSourceProvider>();
			timeSource.MockTime = 10f;

			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			// Time source should have been updated
			Assert.Less(timeSource.MockTime, 10f);
		}

		#endregion

		#region Read-Only TimeSource Tests

		[UnityTest]
		public IEnumerator ReadOnlyTimeSource_TimerFollowsTimeSource()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<ReadOnlyTimeSourceProvider>();
			timeSource.MockTime = 15f;

			yield return null;

			Assert.AreEqual(15f, _timer.TimeRemaining);
		}

		[UnityTest]
		public IEnumerator ReadOnlyTimeSource_TimerCannotModifyTime()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<ReadOnlyTimeSourceProvider>();
			timeSource.MockTime = 15f;

			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.2f);

			// Time source value should not have changed
			Assert.AreEqual(15f, timeSource.MockTime);
		}

		[UnityTest]
		public IEnumerator ReadOnlyTimeSource_FastForwardDoesNothing()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<ReadOnlyTimeSourceProvider>();
			timeSource.MockTime = 15f;

			yield return null;

			_timer.FastForward(5f);

			Assert.AreEqual(15f, timeSource.MockTime);
		}

		[UnityTest]
		public IEnumerator ReadOnlyTimeSource_RewindDoesNothing()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<ReadOnlyTimeSourceProvider>();
			timeSource.MockTime = 15f;

			yield return null;

			_timer.Rewind(5f);

			Assert.AreEqual(15f, timeSource.MockTime);
		}

		#endregion

		#region Dynamic TimeSource Change Tests

		[UnityTest]
		public IEnumerator SetTimeSource_ChangesTimeSourceMidOperation()
		{
			_timer = _gameObject.AddComponent<Timer>();

			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			// Create a new time source
			var newTimeSource = _gameObject.AddComponent<MockTimeSourceProvider>();
			newTimeSource.MockTime = 50f;

			// This should update the timer to use the new time source
			_timer.SetTimeSource(newTimeSource);

			Assert.AreEqual(50f, _timer.TimeRemaining);
		}

		[UnityTest]
		public IEnumerator SetTimeSource_ToNull_UsesInternalTime()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<MockTimeSourceProvider>();
			timeSource.MockTime = 25f;

			yield return null;

			// Verify using time source
			Assert.AreEqual(25f, _timer.TimeRemaining);

			// Remove time source
			_timer.SetTimeSource(null);

			// Should now use internal time (duration default is 10)
			Assert.AreEqual(10f, _timer.Duration);
		}

		#endregion

		#region Destroy Tests

		[UnityTest]
		public IEnumerator TimeSourceProvider_ClearsOnDestroy()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<MockTimeSourceProvider>();
			timeSource.MockTime = 25f;

			yield return null;

			Assert.AreEqual(25f, _timer.TimeRemaining);

			// Destroy the time source
			Object.DestroyImmediate(timeSource);

			yield return null;

			// Timer should still work, using internal time
			Assert.AreEqual(_timer.Duration, _timer.TimeRemaining);
		}

		#endregion

		#region Milestone Tests with TimeSource

		[UnityTest]
		public IEnumerator TimeSource_MilestoneTriggersCorrectly()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<MockTimeSourceProvider>();

			yield return null;

			// Set a short duration so the test runs quickly
			_timer.Duration = 1f;
			timeSource.MockTime = 1f;

			bool triggered = false;
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 0.5f, () => triggered = true));

			_timer.StartTimer();

			// Wait for time to decrease past 0.5 seconds
			yield return new WaitForSeconds(0.7f);

			Assert.IsTrue(triggered);
		}

		[UnityTest]
		public IEnumerator TimeSource_TimerCompletionWorks()
		{
			_timer = _gameObject.AddComponent<Timer>();
			var timeSource = _gameObject.AddComponent<MockTimeSourceProvider>();

			yield return null;

			// Set a short duration so the test runs quickly
			_timer.Duration = 0.2f;
			timeSource.MockTime = 0.2f;

			bool completed = false;
			_timer.OnComplete += () => completed = true;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.3f);

			Assert.IsTrue(completed);
		}

		#endregion

		#region Error Handling Tests

		[UnityTest]
		public IEnumerator TimeSourceProvider_WithoutTimer_LogsError()
		{
			// Expect the error log message
			LogAssert.Expect(LogType.Error, "TimeSourceProvider requires a component implementing ITimer on the same GameObject");

			// Create without Timer component - this will log the expected error
			_gameObject.AddComponent<MockTimeSourceProvider>();

			yield return null;

			// Test passes if the expected error was logged
		}

		#endregion
	}

	[TestFixture]
	public class TimeScaleTests
	{
		private GameObject _gameObject;
		private Timer _timer;
		private float _originalTimeScale;

		[SetUp]
		public void Setup()
		{
			_originalTimeScale = Time.timeScale;
			_gameObject = new GameObject("TestTimer");
			_timer = _gameObject.AddComponent<Timer>();
		}

		[TearDown]
		public void TearDown()
		{
			Time.timeScale = _originalTimeScale;
			if (_gameObject != null)
			{
				Object.DestroyImmediate(_gameObject);
			}
		}

		[UnityTest]
		public IEnumerator Timer_WithScaledTime_AffectedByTimeScale()
		{
			yield return null;

			_timer.StartTimer();
			Time.timeScale = 2f;

			float startTime = _timer.TimeRemaining;

			yield return new WaitForSecondsRealtime(0.1f);

			float elapsed = startTime - _timer.TimeRemaining;

			// With time scale 2x, should have elapsed roughly 0.2 seconds of game time
			Assert.Greater(elapsed, 0.15f);
		}

		[UnityTest]
		public IEnumerator Timer_WithPausedTimeScale_DoesNotUpdate()
		{
			yield return null;

			_timer.StartTimer();
			Time.timeScale = 0f;

			float startTime = _timer.TimeRemaining;

			yield return new WaitForSecondsRealtime(0.2f);

			// Time should not have changed
			Assert.AreEqual(startTime, _timer.TimeRemaining, 0.01f);
		}
	}
}
