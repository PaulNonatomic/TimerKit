using System;
using UnityEngine;

namespace Nonatomic.TimerKit
{
	/// <summary>
	/// The standard full-featured timer implementation with milestone support and serialization.
	/// This is the recommended timer class for most use cases requiring milestone functionality.
	/// </summary>
	public class StandardTimer : MilestoneTimer
	{
		/// <summary>
		/// Initializes a new instance of the StandardTimer class with a specified duration.
		/// </summary>
		/// <param name="duration">The total time in seconds that the timer will run.</param>
		public StandardTimer(float duration) : base(duration)
		{
		}

		/// <summary>
		/// Removes an array of milestones from the timer.
		/// This method is provided for compatibility with legacy code patterns.
		/// </summary>
		/// <param name="milestones">The array of milestones to remove.</param>
		public virtual void RemoveMilestones(TimerMilestone[] milestones)
		{
			foreach (var milestone in milestones)
			{
				RemoveMilestone(milestone);
			}
		}
	}
}