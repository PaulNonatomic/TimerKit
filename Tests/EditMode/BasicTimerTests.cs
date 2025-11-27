using NUnit.Framework;
using System;
using Nonatomic.TimerKit;

namespace Tests.EditMode
{
	[TestFixture]
	public class BasicTimerTests
	{
		private BasicTimer _timer;
		private bool _onStartCalled;
		private bool _onResumeCalled;
		private bool _onStopCalled;
		private bool _onCompleteCalled;
		private float _lastTickTime;

		[SetUp]
		public void Setup()
		{
			_timer = new BasicTimer(10f);

			_onStartCalled = false;
			_onResumeCalled = false;
			_onStopCalled = false;
			_onCompleteCalled = false;
			_lastTickTime = 0;

			_timer.OnStart += () => _onStartCalled = true;
			_timer.OnResume += () => _onResumeCalled = true;
			_timer.OnStop += () => _onStopCalled = true;
			_timer.OnComplete += () => _onCompleteCalled = true;
			_timer.OnTick += (timer) => _lastTickTime = timer.TimeRemaining;
		}

		#region Constructor Tests

		[Test]
		public void Constructor_SetsInitialDuration()
		{
			var timer = new BasicTimer(15f);
			Assert.AreEqual(15f, timer.Duration);
		}

		[Test]
		public void Constructor_SetsTimeRemainingToDuration()
		{
			var timer = new BasicTimer(20f);
			Assert.AreEqual(20f, timer.TimeRemaining);
		}

		[Test]
		public void Constructor_IsNotRunningByDefault()
		{
			var timer = new BasicTimer(10f);
			Assert.IsFalse(timer.IsRunning);
		}

		[Test]
		public void Constructor_WithZeroDuration_Works()
		{
			var timer = new BasicTimer(0f);
			Assert.AreEqual(0f, timer.Duration);
			Assert.AreEqual(0f, timer.TimeRemaining);
		}

		[Test]
		public void Constructor_WithNegativeDuration_Works()
		{
			var timer = new BasicTimer(-5f);
			Assert.AreEqual(-5f, timer.Duration);
		}

		#endregion

		#region Start Timer Tests

		[Test]
		public void StartTimer_SetsIsRunningTrue()
		{
			_timer.StartTimer();
			Assert.IsTrue(_timer.IsRunning);
		}

		[Test]
		public void StartTimer_FiresOnStartEvent()
		{
			_timer.StartTimer();
			Assert.IsTrue(_onStartCalled);
		}

		[Test]
		public void StartTimer_ResetsTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			Assert.AreEqual(5f, _timer.TimeRemaining);

			_timer.StartTimer();
			Assert.AreEqual(10f, _timer.TimeRemaining);
		}

		[Test]
		public void StartTimer_CalledMultipleTimes_ResetsEachTime()
		{
			int startCount = 0;
			_timer.OnStart += () => startCount++;

			_timer.StartTimer();
			_timer.Update(3f);
			_timer.StartTimer();
			_timer.Update(2f);
			_timer.StartTimer();

			Assert.AreEqual(3, startCount); // 3 calls to StartTimer
			Assert.AreEqual(10f, _timer.TimeRemaining);
		}

		#endregion

		#region Resume Timer Tests

		[Test]
		public void ResumeTimer_SetsIsRunningTrue()
		{
			_timer.StartTimer();
			_timer.Update(3f);
			_timer.StopTimer();

			_timer.ResumeTimer();
			Assert.IsTrue(_timer.IsRunning);
		}

		[Test]
		public void ResumeTimer_FiresOnResumeEvent()
		{
			_timer.StartTimer();
			_timer.StopTimer();
			_timer.ResumeTimer();
			Assert.IsTrue(_onResumeCalled);
		}

		[Test]
		public void ResumeTimer_DoesNotResetTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Update(4f);
			_timer.StopTimer();

			_timer.ResumeTimer();
			Assert.AreEqual(6f, _timer.TimeRemaining);
		}

		[Test]
		public void ResumeTimer_DoesNothingWhenTimeRemainingIsZero()
		{
			_timer.StartTimer();
			_timer.Update(10f);

			_onResumeCalled = false;
			_timer.ResumeTimer();

			Assert.IsFalse(_timer.IsRunning);
			Assert.IsFalse(_onResumeCalled);
		}

		#endregion

		#region Stop Timer Tests

		[Test]
		public void StopTimer_SetsIsRunningFalse()
		{
			_timer.StartTimer();
			_timer.StopTimer();
			Assert.IsFalse(_timer.IsRunning);
		}

		[Test]
		public void StopTimer_FiresOnStopEvent()
		{
			_timer.StartTimer();
			_timer.StopTimer();
			Assert.IsTrue(_onStopCalled);
		}

		[Test]
		public void StopTimer_PreservesTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Update(3f);
			_timer.StopTimer();

			Assert.AreEqual(7f, _timer.TimeRemaining);
		}

		#endregion

		#region Reset Timer Tests

		[Test]
		public void ResetTimer_SetsTimeRemainingToDuration()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			_timer.ResetTimer();

			Assert.AreEqual(10f, _timer.TimeRemaining);
		}

		[Test]
		public void ResetTimer_SetsIsRunningFalse()
		{
			_timer.StartTimer();
			_timer.ResetTimer();

			Assert.IsFalse(_timer.IsRunning);
		}

		#endregion

		#region Update Tests

		[Test]
		public void Update_DecreasesTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Update(2f);
			Assert.AreEqual(8f, _timer.TimeRemaining);
		}

		[Test]
		public void Update_FiresOnTickEvent()
		{
			_timer.StartTimer();
			_timer.Update(3f);
			Assert.AreEqual(7f, _lastTickTime);
		}

		[Test]
		public void Update_DoesNothingWhenNotRunning()
		{
			_timer.Update(5f);
			Assert.AreEqual(10f, _timer.TimeRemaining);
			Assert.AreEqual(0f, _lastTickTime);
		}

		[Test]
		public void Update_CompletesWhenTimeReachesZero()
		{
			_timer.StartTimer();
			_timer.Update(10f);

			Assert.IsTrue(_onCompleteCalled);
			Assert.AreEqual(0f, _timer.TimeRemaining);
			Assert.IsFalse(_timer.IsRunning);
		}

		[Test]
		public void Update_CompletesWhenTimeGoesNegative()
		{
			_timer.StartTimer();
			_timer.Update(15f);

			Assert.IsTrue(_onCompleteCalled);
			Assert.AreEqual(0f, _timer.TimeRemaining);
		}

		[Test]
		public void Update_WithZeroDeltaTime_DoesNotChangeTime()
		{
			_timer.StartTimer();
			float initialTime = _timer.TimeRemaining;
			_timer.Update(0f);

			Assert.AreEqual(initialTime, _timer.TimeRemaining);
		}

		[Test]
		public void Update_WithNegativeDeltaTime_IncreasesTime()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			_timer.Update(-2f);

			Assert.AreEqual(7f, _timer.TimeRemaining);
		}

		#endregion

		#region FastForward Tests

		[Test]
		public void FastForward_DecreasesTimeRemaining()
		{
			_timer.StartTimer();
			_timer.FastForward(3f);
			Assert.AreEqual(7f, _timer.TimeRemaining);
		}

		[Test]
		public void FastForward_IgnoresNegativeValues()
		{
			_timer.StartTimer();
			_timer.FastForward(-5f);
			Assert.AreEqual(10f, _timer.TimeRemaining);
		}

		[Test]
		public void FastForward_CompletesWhenTimeReachesZero()
		{
			_timer.StartTimer();
			_timer.FastForward(10f);

			Assert.IsTrue(_onCompleteCalled);
			Assert.AreEqual(0f, _timer.TimeRemaining);
		}

		[Test]
		public void FastForward_FiresOnTickEvent()
		{
			_timer.StartTimer();
			_timer.FastForward(3f);
			Assert.AreEqual(7f, _lastTickTime);
		}

		[Test]
		public void FastForward_WorksWhenNotRunning()
		{
			_timer.FastForward(3f);
			Assert.AreEqual(7f, _timer.TimeRemaining);
		}

		#endregion

		#region Rewind Tests

		[Test]
		public void Rewind_IncreasesTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			_timer.Rewind(3f);
			Assert.AreEqual(8f, _timer.TimeRemaining);
		}

		[Test]
		public void Rewind_IgnoresNegativeValues()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			_timer.Rewind(-3f);
			Assert.AreEqual(5f, _timer.TimeRemaining);
		}

		[Test]
		public void Rewind_ClampsToMaxDuration()
		{
			_timer.StartTimer();
			_timer.Rewind(5f);
			Assert.AreEqual(10f, _timer.TimeRemaining);
		}

		[Test]
		public void Rewind_FiresOnTickEvent()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			_timer.Rewind(2f);
			Assert.AreEqual(7f, _lastTickTime);
		}

		#endregion

		#region Duration Property Tests

		[Test]
		public void Duration_Set_FiresOnDurationChangedEvent()
		{
			float capturedDuration = 0;
			_timer.OnDurationChanged += (d) => capturedDuration = d;

			_timer.Duration = 20f;
			Assert.AreEqual(20f, capturedDuration);
		}

		[Test]
		public void Duration_Decreased_ClampsTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Duration = 5f;

			Assert.AreEqual(5f, _timer.TimeRemaining);
		}

		[Test]
		public void Duration_Increased_DoesNotChangeTimeRemaining()
		{
			_timer.StartTimer();
			_timer.Update(3f);
			float timeBeforeChange = _timer.TimeRemaining;

			_timer.Duration = 20f;
			Assert.AreEqual(timeBeforeChange, _timer.TimeRemaining);
		}

		[Test]
		public void Duration_ChangedWhileNotRunning_DoesNotClampTimeRemaining()
		{
			_timer.Duration = 5f;
			Assert.AreEqual(10f, _timer.TimeRemaining);
		}

		#endregion

		#region Time Properties Tests

		[Test]
		public void TimeElapsed_ReturnsCorrectValue()
		{
			_timer.StartTimer();
			_timer.Update(3f);
			Assert.AreEqual(3f, _timer.TimeElapsed, 0.001f);
		}

		[Test]
		public void ProgressElapsed_ReturnsCorrectValue()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			Assert.AreEqual(0.5f, _timer.ProgressElapsed, 0.001f);
		}

		[Test]
		public void ProgressRemaining_ReturnsCorrectValue()
		{
			_timer.StartTimer();
			_timer.Update(5f);
			Assert.AreEqual(0.5f, _timer.ProgressRemaining, 0.001f);
		}

		[Test]
		public void TimeByType_ReturnsCorrectValues()
		{
			_timer.StartTimer();
			_timer.Update(4f);

			Assert.AreEqual(6f, _timer.TimeByType(TimeType.TimeRemaining), 0.001f);
			Assert.AreEqual(4f, _timer.TimeByType(TimeType.TimeElapsed), 0.001f);
			Assert.AreEqual(0.4f, _timer.TimeByType(TimeType.ProgressElapsed), 0.001f);
			Assert.AreEqual(0.6f, _timer.TimeByType(TimeType.ProgressRemaining), 0.001f);
		}

		#endregion

		#region Serialization Tests

		[Test]
		public void Serialize_ReturnsValidJson()
		{
			_timer.StartTimer();
			_timer.Update(3f);

			string json = _timer.Serialize();

			Assert.IsNotNull(json);
			Assert.IsTrue(json.Contains("Duration"));
			Assert.IsTrue(json.Contains("TimeRemaining"));
			Assert.IsTrue(json.Contains("IsRunning"));
		}

		[Test]
		public void Deserialize_RestoresState()
		{
			_timer.StartTimer();
			_timer.Update(3f);
			string json = _timer.Serialize();

			var newTimer = new BasicTimer(1f);
			newTimer.Deserialize(json);

			Assert.AreEqual(10f, newTimer.Duration);
			Assert.AreEqual(7f, newTimer.TimeRemaining, 0.001f);
			Assert.IsTrue(newTimer.IsRunning);
		}

		[Test]
		public void Deserialize_StopsTimerWhenTimeRemainingIsZero()
		{
			var timer = new BasicTimer(10f);
			timer.StartTimer();
			timer.Update(10f);
			string json = timer.Serialize();

			var newTimer = new BasicTimer(1f);
			newTimer.Deserialize(json);

			Assert.IsFalse(newTimer.IsRunning);
		}

		[Test]
		public void Deserialize_WithInvalidJson_ThrowsException()
		{
			Assert.Throws<ArgumentException>(() => _timer.Deserialize("invalid json"));
		}

		#endregion

		#region Event Subscription Tests

		[Test]
		public void MultipleSubscribers_AllReceiveEvents()
		{
			int count1 = 0, count2 = 0, count3 = 0;

			_timer.OnStart += () => count1++;
			_timer.OnStart += () => count2++;
			_timer.OnStart += () => count3++;

			_timer.StartTimer();

			Assert.AreEqual(1, count1);
			Assert.AreEqual(1, count2);
			Assert.AreEqual(1, count3);
		}

		[Test]
		public void UnsubscribedHandler_DoesNotReceiveEvents()
		{
			int count = 0;
			Action handler = () => count++;

			_timer.OnStart += handler;
			_timer.StartTimer();
			Assert.AreEqual(1, count);

			_timer.OnStart -= handler;
			_timer.StartTimer();
			Assert.AreEqual(1, count);
		}

		#endregion
	}
}
