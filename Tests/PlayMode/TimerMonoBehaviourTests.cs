using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nonatomic.TimerKit;

namespace Tests.PlayMode
{
	[TestFixture]
	public class TimerMonoBehaviourTests
	{
		private GameObject _gameObject;
		private Timer _timer;

		[SetUp]
		public void Setup()
		{
			_gameObject = new GameObject("TestTimer");
			_timer = _gameObject.AddComponent<Timer>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				Object.DestroyImmediate(_gameObject);
			}
		}

		#region Basic Functionality Tests

		[UnityTest]
		public IEnumerator Timer_StartsWithDefaultDuration()
		{
			yield return null; // Wait one frame for Awake

			Assert.AreEqual(10f, _timer.Duration);
		}

		[UnityTest]
		public IEnumerator Timer_IsNotRunningByDefault()
		{
			yield return null;

			Assert.IsFalse(_timer.IsRunning);
		}

		[UnityTest]
		public IEnumerator Timer_StartTimer_SetsIsRunningTrue()
		{
			yield return null;

			_timer.StartTimer();

			Assert.IsTrue(_timer.IsRunning);
		}

		[UnityTest]
		public IEnumerator Timer_StopTimer_SetsIsRunningFalse()
		{
			yield return null;

			_timer.StartTimer();
			_timer.StopTimer();

			Assert.IsFalse(_timer.IsRunning);
		}

		[UnityTest]
		public IEnumerator Timer_DecreasesOverTime()
		{
			yield return null;

			_timer.StartTimer();
			float initialTime = _timer.TimeRemaining;

			yield return new WaitForSeconds(0.1f);

			Assert.Less(_timer.TimeRemaining, initialTime);
		}

		#endregion

		#region Event Tests

		[UnityTest]
		public IEnumerator Timer_OnStart_FiresWhenStarted()
		{
			yield return null;

			bool eventFired = false;
			_timer.OnStart += () => eventFired = true;

			_timer.StartTimer();

			Assert.IsTrue(eventFired);
		}

		[UnityTest]
		public IEnumerator Timer_OnStop_FiresWhenStopped()
		{
			yield return null;

			bool eventFired = false;
			_timer.OnStop += () => eventFired = true;

			_timer.StartTimer();
			_timer.StopTimer();

			Assert.IsTrue(eventFired);
		}

		[UnityTest]
		public IEnumerator Timer_OnResume_FiresWhenResumed()
		{
			yield return null;

			bool eventFired = false;
			_timer.OnResume += () => eventFired = true;

			_timer.StartTimer();
			_timer.StopTimer();
			_timer.ResumeTimer();

			Assert.IsTrue(eventFired);
		}

		[UnityTest]
		public IEnumerator Timer_OnTick_FiresEveryFrame()
		{
			yield return null;

			int tickCount = 0;
			_timer.OnTick += (t) => tickCount++;

			_timer.StartTimer();

			yield return null; // 1 frame
			yield return null; // 2 frames
			yield return null; // 3 frames

			Assert.GreaterOrEqual(tickCount, 3);
		}

		[UnityTest]
		public IEnumerator Timer_OnComplete_FiresWhenComplete()
		{
			// Create a short duration timer
			Object.DestroyImmediate(_gameObject);
			_gameObject = new GameObject("ShortTimer");
			_timer = _gameObject.AddComponent<Timer>();

			yield return null;

			_timer.Duration = 0.1f;
			_timer.ResetTimer();

			bool completed = false;
			_timer.OnComplete += () => completed = true;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.2f);

			Assert.IsTrue(completed);
		}

		[UnityTest]
		public IEnumerator Timer_OnDurationChanged_FiresWhenDurationSet()
		{
			yield return null;

			float capturedDuration = 0;
			_timer.OnDurationChanged += (d) => capturedDuration = d;

			_timer.Duration = 20f;

			Assert.AreEqual(20f, capturedDuration);
		}

		#endregion

		#region Duration Property Tests

		[UnityTest]
		public IEnumerator Timer_Duration_CanBeSetAndGet()
		{
			yield return null;

			_timer.Duration = 30f;

			Assert.AreEqual(30f, _timer.Duration);
		}

		[UnityTest]
		public IEnumerator Timer_Duration_ClampsTimeRemainingWhenDecreased()
		{
			yield return null;

			_timer.Duration = 20f;
			_timer.ResetTimer();
			_timer.StartTimer();

			_timer.Duration = 5f;

			Assert.AreEqual(5f, _timer.TimeRemaining);
		}

		#endregion

		#region Time Properties Tests

		[UnityTest]
		public IEnumerator Timer_TimeElapsed_IncreasesOverTime()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			Assert.Greater(_timer.TimeElapsed, 0f);
		}

		[UnityTest]
		public IEnumerator Timer_ProgressElapsed_IncreasesOverTime()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			Assert.Greater(_timer.ProgressElapsed, 0f);
		}

		[UnityTest]
		public IEnumerator Timer_ProgressRemaining_DecreasesOverTime()
		{
			yield return null;

			_timer.StartTimer();
			float initialProgress = _timer.ProgressRemaining;

			yield return new WaitForSeconds(0.1f);

			Assert.Less(_timer.ProgressRemaining, initialProgress);
		}

		#endregion

		#region FastForward and Rewind Tests

		[UnityTest]
		public IEnumerator Timer_FastForward_DecreasesTimeRemaining()
		{
			yield return null;

			_timer.StartTimer();
			float initialTime = _timer.TimeRemaining;

			_timer.FastForward(3f);

			Assert.AreEqual(initialTime - 3f, _timer.TimeRemaining, 0.01f);
		}

		[UnityTest]
		public IEnumerator Timer_Rewind_IncreasesTimeRemaining()
		{
			yield return null;

			_timer.StartTimer();
			_timer.FastForward(5f);
			float timeAfterFastForward = _timer.TimeRemaining;

			_timer.Rewind(2f);

			Assert.AreEqual(timeAfterFastForward + 2f, _timer.TimeRemaining, 0.01f);
		}

		#endregion

		#region Milestone Tests

		[UnityTest]
		public IEnumerator Timer_Milestone_TriggersAtCorrectTime()
		{
			yield return null;

			_timer.Duration = 1f;
			_timer.ResetTimer();

			bool triggered = false;
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 0.5f, () => triggered = true));

			_timer.StartTimer();

			yield return new WaitForSeconds(0.6f);

			Assert.IsTrue(triggered);
		}

		[UnityTest]
		public IEnumerator Timer_RangeMilestone_TriggersAtIntervals()
		{
			yield return null;

			_timer.Duration = 1f;
			_timer.ResetTimer();

			int triggerCount = 0;
			_timer.AddRangeMilestone(
				TimeType.TimeRemaining,
				0.8f,
				0.2f,
				0.2f,
				() => triggerCount++
			);

			_timer.StartTimer();

			yield return new WaitForSeconds(1.1f);

			// Should trigger at 0.8, 0.6, 0.4, 0.2 = 4 times
			Assert.GreaterOrEqual(triggerCount, 3); // Allow some margin for timing
		}

		[UnityTest]
		public IEnumerator Timer_RemoveAllMilestones_PreventsTriggering()
		{
			yield return null;

			_timer.Duration = 1f;
			_timer.ResetTimer();

			int triggerCount = 0;
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 0.5f, () => triggerCount++));

			_timer.RemoveAllMilestones();
			_timer.StartTimer();

			yield return new WaitForSeconds(1.1f);

			Assert.AreEqual(0, triggerCount);
		}

		#endregion

		#region Component Enable/Disable Tests

		[UnityTest]
		public IEnumerator Timer_DisableComponent_StopsUpdating()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			float timeBeforeDisable = _timer.TimeRemaining;
			_timer.enabled = false;

			yield return new WaitForSeconds(0.2f);

			// Time should not have changed significantly while disabled
			Assert.AreEqual(timeBeforeDisable, _timer.TimeRemaining, 0.05f);
		}

		[UnityTest]
		public IEnumerator Timer_ReenableComponent_ResumesUpdating()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			_timer.enabled = false;

			yield return new WaitForSeconds(0.1f);

			float timeBeforeReenable = _timer.TimeRemaining;
			_timer.enabled = true;

			yield return new WaitForSeconds(0.1f);

			Assert.Less(_timer.TimeRemaining, timeBeforeReenable);
		}

		#endregion

		#region GameObject Active State Tests

		[UnityTest]
		public IEnumerator Timer_DeactivateGameObject_StopsUpdating()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			float timeBeforeDeactivate = _timer.TimeRemaining;
			_gameObject.SetActive(false);

			yield return new WaitForSeconds(0.2f);

			Assert.AreEqual(timeBeforeDeactivate, _timer.TimeRemaining, 0.05f);
		}

		[UnityTest]
		public IEnumerator Timer_ReactivateGameObject_ResumesUpdating()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			_gameObject.SetActive(false);

			yield return new WaitForSeconds(0.1f);

			float timeBeforeReactivate = _timer.TimeRemaining;
			_gameObject.SetActive(true);

			yield return new WaitForSeconds(0.1f);

			Assert.Less(_timer.TimeRemaining, timeBeforeReactivate);
		}

		#endregion

		#region Reset Timer Tests

		[UnityTest]
		public IEnumerator Timer_ResetTimer_RestoresToFullDuration()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.2f);

			_timer.ResetTimer();

			Assert.AreEqual(_timer.Duration, _timer.TimeRemaining);
		}

		[UnityTest]
		public IEnumerator Timer_ResetTimer_StopsTimer()
		{
			yield return null;

			_timer.StartTimer();
			_timer.ResetTimer();

			Assert.IsFalse(_timer.IsRunning);
		}

		#endregion

		#region TimeByType Tests

		[UnityTest]
		public IEnumerator Timer_TimeByType_ReturnsCorrectValues()
		{
			yield return null;

			_timer.StartTimer();

			yield return new WaitForSeconds(0.1f);

			Assert.Greater(_timer.TimeByType(TimeType.TimeElapsed), 0f);
			Assert.Less(_timer.TimeByType(TimeType.TimeRemaining), _timer.Duration);
			Assert.Greater(_timer.TimeByType(TimeType.ProgressElapsed), 0f);
			Assert.Less(_timer.TimeByType(TimeType.ProgressRemaining), 1f);
		}

		#endregion
	}
}
