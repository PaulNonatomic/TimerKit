namespace Nonatomic.TimerKit
{
	using System;
	using static System.TimeSpan;

	public static class TimerUtility
	{
		// Constants for commonly used time formats
		private const string HHMMSSFFF = @"hh\:mm\:ss\.fff";
		private const string HHMMSS = @"hh\:mm\:ss";
		private const string MMSS = @"mm\:ss";
		private const string SSFFF = @"ss\.fff";

		// Time Calculations as Static Methods
		public static double Hours(double seconds) => seconds / 3600;
		public static double Minutes(double seconds) => seconds / 60 % 60;
		public static double Seconds(double seconds) => seconds % 60;
		public static double Milliseconds(double seconds) => (seconds - Math.Truncate(seconds)) * 1000;

		// Overloads for IReadOnlyTimer
		public static double Hours(IReadOnlyTimer timer, TimeType type = TimeType.TimeRemaining) => Hours(timer.TimeByType(type));
		public static double Minutes(IReadOnlyTimer timer, TimeType type = TimeType.TimeRemaining) => Minutes(timer.TimeByType(type));
		public static double Seconds(IReadOnlyTimer timer, TimeType type = TimeType.TimeRemaining) => Seconds(timer.TimeByType(type));
		public static double Milliseconds(IReadOnlyTimer timer, TimeType type = TimeType.TimeRemaining) => Milliseconds(timer.TimeByType(type));

		// Formatted Time Strings as Static Methods
		public static string FormatTime(double totalSeconds, string format)
		{
			var timeSpan = FromSeconds(totalSeconds);
			return timeSpan.ToString(format);
		}

		// Overload for IReadOnlyTimer
		public static string FormatTime(IReadOnlyTimer timer, string format)
		{
			return FormatTime(timer.TimeRemaining, format);
		}

		// Static Formatting Methods for Common Time Formats
		public static string FormatHHMMSSFFF(double seconds) => FormatTime(seconds, HHMMSSFFF);
		public static string FormatHHMMSS(double seconds) => FormatTime(seconds, HHMMSS);
		public static string FormatMMSS(double seconds) => FormatTime(seconds, MMSS);
		public static string FormatSSFFF(double seconds) => FormatTime(seconds, SSFFF);

		// Overloads for IReadOnlyTimer
		public static string FormatHHMMSSFFF(IReadOnlyTimer timer) => FormatTime(timer, HHMMSSFFF);
		public static string FormatHHMMSS(IReadOnlyTimer timer) => FormatTime(timer, HHMMSS);
		public static string FormatMMSS(IReadOnlyTimer timer) => FormatTime(timer, MMSS);
		public static string FormatSSFFF(IReadOnlyTimer timer) => FormatTime(timer, SSFFF);
	}
}