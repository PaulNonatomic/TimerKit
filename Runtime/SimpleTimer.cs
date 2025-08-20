using System;
using UnityEngine;

namespace Nonatomic.TimerKit
{
	/// <summary>
	/// [DEPRECATED] This class has been renamed to StandardTimer for clarity.
	/// SimpleTimer is maintained for backward compatibility but will be removed in a future version.
	/// Please use StandardTimer, MilestoneTimer, or BasicTimer depending on your needs.
	/// </summary>
	[Obsolete("SimpleTimer has been renamed to StandardTimer for clarity. Please use StandardTimer instead.", false)]
	public class SimpleTimer : StandardTimer
	{
		/// <summary>
		/// Initializes a new instance of the SimpleTimer class with a specified duration.
		/// This constructor is provided for backward compatibility.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		public SimpleTimer(float duration) : base(duration)
		{
		}
	}
}