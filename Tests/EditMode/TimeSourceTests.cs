using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using Nonatomic.TimerKit;

namespace Tests.EditMode
{
	/// <summary>
	/// Tests for ITimeSource functionality with StandardTimer
	/// </summary>
	[TestFixture]
	public class TimeSourceTests
	{
		private StandardTimer _timer;
		private MockTimeSource _mockTimeSource;
		
		[SetUp]
		public void Setup()
		{
			_mockTimeSource = new MockTimeSource(10f);
			_timer = new StandardTimer(10f, _mockTimeSource);
		}
		
		[Test]
		public void Timer_WithTimeSource_UsesExternalTimeValue()
		{
			// Set external time to 7 seconds
			_mockTimeSource.SetTimeRemaining(7f);
			
			// Timer should reflect external time
			Assert.AreEqual(7f, _timer.TimeRemaining, "Timer should use external time source value");
		}
		
		[Test]
		public void Timer_WithTimeSource_UpdatesExternalTime()
		{
			_timer.StartTimer();
			Assert.AreEqual(10f, _mockTimeSource.GetTimeRemaining(), "Time source should be set to duration on start");
			
			_timer.Update(3f);
			Assert.AreEqual(7f, _mockTimeSource.GetTimeRemaining(), "Time source should be updated after timer update");
		}
		
		[Test]
		public void Timer_WithReadOnlyTimeSource_CannotModifyTime()
		{
			var readOnlySource = new ReadOnlyMockTimeSource(5f);
			var timer = new StandardTimer(10f, readOnlySource);
			
			// Starting timer should not change read-only source
			timer.StartTimer();
			Assert.AreEqual(5f, readOnlySource.GetTimeRemaining(), "Read-only source should not change on start");
			
			// FastForward should not work with read-only source
			timer.FastForward(2f);
			Assert.AreEqual(5f, timer.TimeRemaining, "FastForward should not affect read-only source");
			
			// Rewind should not work with read-only source
			timer.Rewind(2f);
			Assert.AreEqual(5f, timer.TimeRemaining, "Rewind should not affect read-only source");
		}
		
		[Test]
		public void Timer_WithoutTimeSource_UsesInternalTime()
		{
			var timer = new StandardTimer(10f); // No time source
			
			timer.StartTimer();
			Assert.AreEqual(10f, timer.TimeRemaining, "Should use internal time management");
			
			timer.Update(3f);
			Assert.AreEqual(7f, timer.TimeRemaining, "Should update internal time");
		}
		
		[Test]
		public void Timer_WithTimeSource_MilestonesWorkCorrectly()
		{
			bool milestoneTriggered = false;
			
			_timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 5f, () => milestoneTriggered = true));
			_timer.StartTimer();
			
			// Set time source to trigger milestone
			_mockTimeSource.SetTimeRemaining(5f);
			_timer.Update(0f); // Trigger check without changing time
			
			Assert.IsTrue(milestoneTriggered, "Milestone should trigger based on time source value");
		}
		
		[Test]
		public void Timer_WithTimeSource_CompletionWorks()
		{
			bool completed = false;
			_timer.OnComplete += () => completed = true;
			
			_timer.StartTimer();
			_mockTimeSource.SetTimeRemaining(0.1f);
			_timer.Update(0.2f); // Should complete
			
			Assert.IsTrue(completed, "Timer should complete when time source reaches zero");
			Assert.AreEqual(0f, _timer.TimeRemaining, "Time should be clamped to zero");
		}
		
		[Test]
		public void Timer_TimeElapsedCalculation_WithTimeSource()
		{
			_mockTimeSource.SetTimeRemaining(3f);
			
			Assert.AreEqual(7f, _timer.TimeElapsed, "TimeElapsed should be Duration - TimeRemaining");
			Assert.AreEqual(0.7f, _timer.ProgressElapsed, 0.01f, "ProgressElapsed should be correct");
			Assert.AreEqual(0.3f, _timer.ProgressRemaining, 0.01f, "ProgressRemaining should be correct");
		}
		
		[Test]
		public void DefaultTimeSource_BehavesCorrectly()
		{
			var defaultSource = new DefaultTimeSource(10f);
			
			Assert.IsTrue(defaultSource.CanSetTime, "DefaultTimeSource should allow setting time");
			Assert.AreEqual(10f, defaultSource.GetTimeRemaining(), "Should return initial value");
			
			defaultSource.SetTimeRemaining(5f);
			Assert.AreEqual(5f, defaultSource.GetTimeRemaining(), "Should update value");
			
			defaultSource.UpdateTime(2f);
			Assert.AreEqual(3f, defaultSource.GetTimeRemaining(), "UpdateTime should decrease time");
		}
		
		[Test]
		public void Timer_Serialization_WithTimeSource()
		{
			_timer.StartTimer();
			_timer.Update(3f); // Now at 7 seconds
			
			string json = _timer.Serialize();
			
			// Create new timer with same time source
			var newTimer = new StandardTimer(10f, _mockTimeSource);
			newTimer.Deserialize(json);
			
			Assert.AreEqual(7f, newTimer.TimeRemaining, "Deserialized timer should restore time");
			Assert.AreEqual(7f, _mockTimeSource.GetTimeRemaining(), "Time source should be updated");
		}
		
		[Test]
		public void Timer_ResetWithTimeSource_Works()
		{
			_timer.StartTimer();
			_timer.Update(5f); // Now at 5 seconds
			
			_timer.ResetTimer();
			
			Assert.AreEqual(10f, _timer.TimeRemaining, "Timer should reset to duration");
			Assert.AreEqual(10f, _mockTimeSource.GetTimeRemaining(), "Time source should be reset");
			Assert.IsFalse(_timer.IsRunning, "Timer should not be running after reset");
		}
		
		// Mock implementations for testing
		private class MockTimeSource : ITimeSource
		{
			private float _time;
			public bool CanSetTime => true;
			
			public MockTimeSource(float initialTime)
			{
				_time = initialTime;
			}
			
			public float GetTimeRemaining() => _time;
			public void SetTimeRemaining(float timeRemaining) => _time = timeRemaining;
		}
		
		private class ReadOnlyMockTimeSource : ITimeSource
		{
			private readonly float _time;
			public bool CanSetTime => false;
			
			public ReadOnlyMockTimeSource(float time)
			{
				_time = time;
			}
			
			public float GetTimeRemaining() => _time;
			public void SetTimeRemaining(float timeRemaining) { /* Read-only */ }
		}
	}
	
	/// <summary>
	/// Tests for TimeSourceProvider component
	/// </summary>
	[TestFixture]
	public class TimeSourceProviderTests
	{
		private GameObject _gameObject;
		private Timer _timerComponent;
		private MockTimeSourceProvider _provider;
		
		[SetUp]
		public void Setup()
		{
			_gameObject = new GameObject("TestTimer");
			_timerComponent = _gameObject.AddComponent<Timer>();
			// Manually trigger Awake since it doesn't run automatically in edit mode tests
			var awakeMethod = typeof(Timer).GetMethod("Awake", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			awakeMethod?.Invoke(_timerComponent, null);
			// Provider will be added in individual tests
		}
		
		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}
		
		[Test]
		public void TimeSourceProvider_RequiresTimerComponent()
		{
			// The RequireComponent attribute ensures Timer is always present
			// So we'll test that the component setup works correctly
			var testObject = new GameObject("TestWithTimer");
			
			try
			{
				// Adding provider should automatically add Timer due to RequireComponent
				var provider = testObject.AddComponent<MockTimeSourceProvider>();
				
				// Verify Timer was automatically added
				var timer = testObject.GetComponent<Timer>();
				Assert.IsNotNull(timer, "Timer should be automatically added by RequireComponent");
				
				// Set a test value before awakening
				provider.SetTestTime(7f);
				
				// First awaken the Timer (creates internal timer without time source)
				var timerAwakeMethod = typeof(Timer).GetMethod("Awake", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				timerAwakeMethod?.Invoke(timer, null);
				
				// Then awaken the provider (sets itself as time source)
				provider.TestAwake();
				
				// Verify the timer uses the provider's value
				Assert.AreEqual(7f, timer.TimeRemaining, 
					"Timer should be connected to TimeSourceProvider and preserve its value");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(testObject);
			}
		}
		
		[Test]
		public void TimeSourceProvider_SetsItselfAsTimeSource()
		{
			_provider = _gameObject.AddComponent<MockTimeSourceProvider>();
			_provider.SetTestTime(5f);
			
			// Force awake call (Unity doesn't call it automatically in edit mode tests)
			_provider.TestAwake();
			
			// Before starting, timer should reflect provider's current value
			Assert.AreEqual(5f, _timerComponent.TimeRemaining, 
				"Timer should use TimeSourceProvider's initial time value");
			
			// StartTimer should set the time to Duration (10f by default)
			_timerComponent.StartTimer();
			Assert.AreEqual(10f, _timerComponent.TimeRemaining, 
				"After StartTimer, time should be reset to Duration");
			Assert.AreEqual(10f, _provider.GetTimeRemaining(), 
				"Provider should be updated when timer starts");
		}
		
		[Test]
		public void TimeSourceProvider_ClearsOnDestroy()
		{
			_provider = _gameObject.AddComponent<MockTimeSourceProvider>();
			_provider.SetTestTime(5f);
			_provider.TestAwake();
			
			// Verify provider is set - timer reflects provider's value
			Assert.AreEqual(5f, _timerComponent.TimeRemaining, "Timer should use provider's initial time");
			
			// Call OnDestroy to clear time source
			_provider.TestOnDestroy();
			
			// After clearing time source, timer needs to be recreated or reset
			// Since SetTimeSource(null) was called, the timer was recreated
			_timerComponent.StartTimer();
			
			// Now it should use the default duration from the serialized field
			var duration = _timerComponent.Duration;
			Assert.AreEqual(duration, _timerComponent.TimeRemaining, 
				"Timer should use internal time after provider is destroyed");
		}
		
		[Test]
		public void TimeSourceProvider_UpdatesTimerTime()
		{
			_provider = _gameObject.AddComponent<MockTimeSourceProvider>();
			_provider.SetTestTime(8f);
			_provider.TestAwake();
			
			// Initially timer reflects provider's value
			Assert.AreEqual(8f, _timerComponent.TimeRemaining, "Initial time from provider");
			
			// Update provider's time
			_provider.SetTestTime(4f);
			
			// Timer should reflect new time
			Assert.AreEqual(4f, _timerComponent.TimeRemaining, "Timer should reflect provider's updated time");
			
			// StartTimer should set to Duration
			_timerComponent.StartTimer();
			Assert.AreEqual(10f, _timerComponent.TimeRemaining, "StartTimer should reset to Duration");
		}
		
		// Mock TimeSourceProvider for testing
		private class MockTimeSourceProvider : TimeSourceProvider
		{
			private float _testTime = 10f;
			private bool _canSet = true;
			
			public override bool CanSetTime => _canSet;
			
			public override float GetTimeRemaining()
			{
				return _testTime;
			}
			
			public override void SetTimeRemaining(float timeRemaining)
			{
				if (_canSet)
					_testTime = timeRemaining;
			}
			
			public void SetTestTime(float time)
			{
				_testTime = time;
			}
			
			public void SetCanSetTime(bool canSet)
			{
				_canSet = canSet;
			}
			
			// Expose Awake for testing
			public void TestAwake()
			{
				base.Awake();
			}
			
			// Expose OnDestroy for testing
			public void TestOnDestroy()
			{
				base.OnDestroy();
			}
		}
	}
}