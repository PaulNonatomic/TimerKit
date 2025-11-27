using NUnit.Framework;
using Nonatomic.TimerKit;

namespace Tests.EditMode
{
	[TestFixture]
	public class TimerUtilityTests
	{
		#region Time Calculation Tests - Double Input

		[Test]
		public void Hours_ReturnsCorrectValue()
		{
			Assert.AreEqual(1.0, TimerUtility.Hours(3600), 0.001);
			Assert.AreEqual(2.5, TimerUtility.Hours(9000), 0.001);
			Assert.AreEqual(0.5, TimerUtility.Hours(1800), 0.001);
		}

		[Test]
		public void Hours_WithZero_ReturnsZero()
		{
			Assert.AreEqual(0, TimerUtility.Hours(0));
		}

		[Test]
		public void Minutes_ReturnsCorrectValue()
		{
			Assert.AreEqual(30, TimerUtility.Minutes(1800), 0.001);
			Assert.AreEqual(45, TimerUtility.Minutes(2700), 0.001);
			Assert.AreEqual(0, TimerUtility.Minutes(3600), 0.001); // Exactly 1 hour
		}

		[Test]
		public void Minutes_WrapsAt60()
		{
			// 90 seconds = 1.5 minutes, but Minutes() returns the remainder (30)
			Assert.AreEqual(30, TimerUtility.Minutes(5400), 0.001); // 90 minutes = 30 remainder
		}

		[Test]
		public void Seconds_ReturnsCorrectValue()
		{
			Assert.AreEqual(30, TimerUtility.Seconds(30), 0.001);
			Assert.AreEqual(45, TimerUtility.Seconds(45), 0.001);
			Assert.AreEqual(0, TimerUtility.Seconds(60), 0.001); // Exactly 1 minute
		}

		[Test]
		public void Seconds_WrapsAt60()
		{
			Assert.AreEqual(30, TimerUtility.Seconds(90), 0.001); // 90 seconds = 30 remainder
			Assert.AreEqual(15, TimerUtility.Seconds(75), 0.001);
		}

		[Test]
		public void Milliseconds_ReturnsCorrectValue()
		{
			Assert.AreEqual(500, TimerUtility.Milliseconds(1.5), 0.1);
			Assert.AreEqual(250, TimerUtility.Milliseconds(2.25), 0.1);
			Assert.AreEqual(0, TimerUtility.Milliseconds(3.0), 0.1);
		}

		[Test]
		public void Milliseconds_WithFractionalSeconds()
		{
			Assert.AreEqual(123, TimerUtility.Milliseconds(10.123), 1);
			Assert.AreEqual(999, TimerUtility.Milliseconds(5.999), 1);
		}

		#endregion

		#region Time Calculation Tests - IReadOnlyTimer Input

		[Test]
		public void Hours_WithTimer_ReturnsCorrectValue()
		{
			var timer = new StandardTimer(7200f); // 2 hours
			timer.StartTimer();

			Assert.AreEqual(2.0, TimerUtility.Hours(timer, TimeType.TimeRemaining), 0.001);
		}

		[Test]
		public void Minutes_WithTimer_ReturnsCorrectValue()
		{
			var timer = new StandardTimer(150f); // 2 minutes 30 seconds
			timer.StartTimer();

			// 150 seconds = 2.5 minutes, Minutes() returns remainder after hours = 2.5
			Assert.AreEqual(2.5, TimerUtility.Minutes(timer, TimeType.TimeRemaining), 0.001);
		}

		[Test]
		public void Seconds_WithTimer_ReturnsCorrectValue()
		{
			var timer = new StandardTimer(75f); // 1 minute 15 seconds
			timer.StartTimer();

			Assert.AreEqual(15, TimerUtility.Seconds(timer, TimeType.TimeRemaining), 0.001);
		}

		[Test]
		public void Milliseconds_WithTimer_ReturnsCorrectValue()
		{
			var timer = new StandardTimer(10.5f);
			timer.StartTimer();

			Assert.AreEqual(500, TimerUtility.Milliseconds(timer, TimeType.TimeRemaining), 1);
		}

		[Test]
		public void Timer_WithTimeElapsedType()
		{
			var timer = new StandardTimer(100f);
			timer.StartTimer();
			timer.Update(65f); // 65 seconds elapsed

			Assert.AreEqual(1, TimerUtility.Minutes(timer, TimeType.TimeElapsed), 0.1);
			Assert.AreEqual(5, TimerUtility.Seconds(timer, TimeType.TimeElapsed), 0.1);
		}

		#endregion

		#region Format Time Tests

		[Test]
		public void FormatTime_WithCustomFormat()
		{
			string result = TimerUtility.FormatTime(3665.5, @"hh\:mm\:ss\.fff");
			Assert.AreEqual("01:01:05.500", result);
		}

		[Test]
		public void FormatHHMMSSFFF_ReturnsCorrectFormat()
		{
			string result = TimerUtility.FormatHHMMSSFFF(3661.123);
			Assert.AreEqual("01:01:01.123", result);
		}

		[Test]
		public void FormatHHMMSS_ReturnsCorrectFormat()
		{
			string result = TimerUtility.FormatHHMMSS(3661);
			Assert.AreEqual("01:01:01", result);
		}

		[Test]
		public void FormatMMSS_ReturnsCorrectFormat()
		{
			string result = TimerUtility.FormatMMSS(125);
			Assert.AreEqual("02:05", result);
		}

		[Test]
		public void FormatSSFFF_ReturnsCorrectFormat()
		{
			string result = TimerUtility.FormatSSFFF(45.678);
			Assert.AreEqual("45.678", result);
		}

		#endregion

		#region Format Time Tests - IReadOnlyTimer Input

		[Test]
		public void FormatTime_WithTimer()
		{
			var timer = new StandardTimer(3665.5f);
			timer.StartTimer();

			string result = TimerUtility.FormatTime(timer, @"hh\:mm\:ss\.fff");
			Assert.AreEqual("01:01:05.500", result);
		}

		[Test]
		public void FormatHHMMSSFFF_WithTimer()
		{
			var timer = new StandardTimer(3661.123f);
			timer.StartTimer();

			string result = TimerUtility.FormatHHMMSSFFF(timer);
			Assert.AreEqual("01:01:01.123", result);
		}

		[Test]
		public void FormatHHMMSS_WithTimer()
		{
			var timer = new StandardTimer(3661f);
			timer.StartTimer();

			string result = TimerUtility.FormatHHMMSS(timer);
			Assert.AreEqual("01:01:01", result);
		}

		[Test]
		public void FormatMMSS_WithTimer()
		{
			var timer = new StandardTimer(125f);
			timer.StartTimer();

			string result = TimerUtility.FormatMMSS(timer);
			Assert.AreEqual("02:05", result);
		}

		[Test]
		public void FormatSSFFF_WithTimer()
		{
			var timer = new StandardTimer(45.678f);
			timer.StartTimer();

			string result = TimerUtility.FormatSSFFF(timer);
			Assert.AreEqual("45.678", result);
		}

		#endregion

		#region Edge Case Tests

		[Test]
		public void FormatHHMMSSFFF_WithZero()
		{
			string result = TimerUtility.FormatHHMMSSFFF(0);
			Assert.AreEqual("00:00:00.000", result);
		}

		[Test]
		public void FormatHHMMSS_WithLargeValue()
		{
			// 25 hours - TimeSpan.ToString with "hh" wraps at 24, showing only 0-23
			// The actual output will be "01:00:00" (25 mod 24 = 1)
			string result = TimerUtility.FormatHHMMSS(90000);
			Assert.AreEqual("01:00:00", result);
		}

		[Test]
		public void FormatMMSS_WithExactMinutes()
		{
			string result = TimerUtility.FormatMMSS(120);
			Assert.AreEqual("02:00", result);
		}

		[Test]
		public void FormatSSFFF_WithWholeSeconds()
		{
			string result = TimerUtility.FormatSSFFF(30.0);
			Assert.AreEqual("30.000", result);
		}

		[Test]
		public void Hours_WithSmallValue()
		{
			// 1 second = 0.000278 hours
			Assert.AreEqual(0.000278, TimerUtility.Hours(1), 0.0001);
		}

		[Test]
		public void Seconds_WithDecimalValue()
		{
			// 90.5 seconds = 30.5 seconds remainder
			Assert.AreEqual(30.5, TimerUtility.Seconds(90.5), 0.001);
		}

		[Test]
		public void Milliseconds_PrecisionTest()
		{
			// Test very precise milliseconds
			Assert.AreEqual(1, TimerUtility.Milliseconds(0.001), 0.1);
			Assert.AreEqual(999, TimerUtility.Milliseconds(0.999), 0.1);
		}

		#endregion

		#region Timer State Tests

		[Test]
		public void FormatMethods_AfterTimerUpdate()
		{
			var timer = new StandardTimer(125f);
			timer.StartTimer();
			timer.Update(65f); // 60 seconds remaining

			string result = TimerUtility.FormatMMSS(timer);
			Assert.AreEqual("01:00", result);
		}

		[Test]
		public void TimeCalculations_AfterTimerUpdate()
		{
			var timer = new StandardTimer(3700f); // 1 hour + 100 seconds
			timer.StartTimer();
			timer.Update(100f); // 3600 seconds remaining (1 hour)

			Assert.AreEqual(1, TimerUtility.Hours(timer), 0.001);
			Assert.AreEqual(0, TimerUtility.Minutes(timer), 0.001);
			Assert.AreEqual(0, TimerUtility.Seconds(timer), 0.001);
		}

		[Test]
		public void TimeCalculations_WithTimeElapsed()
		{
			var timer = new StandardTimer(3700f);
			timer.StartTimer();
			timer.Update(100f); // 100 seconds elapsed

			// 100 seconds = 1 minute 40 seconds
			// Minutes(100) = 100/60 % 60 = 1.666...
			Assert.AreEqual(1.666, TimerUtility.Minutes(timer, TimeType.TimeElapsed), 0.01);
			Assert.AreEqual(40, TimerUtility.Seconds(timer, TimeType.TimeElapsed), 0.001);
		}

		#endregion
	}
}
