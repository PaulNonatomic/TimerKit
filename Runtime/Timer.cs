using System;
using UnityEngine;

namespace Nonatomic.Timers
{
	/// <summary>
	/// A MonoBehaviour that manages a timer, allowing it to be used within the Unity lifecycle.
	/// It delegates timer functionality to a standard SimpleTimer object and synchronizes with Unity's game time.
	/// </summary>
	public class Timer : MonoBehaviour, ITimer
	{
		public event Action OnStart;
		public event Action OnResume;
		public event Action OnStop;
		public event Action OnComplete;
		public event Action<IReadOnlyTimer> OnTick;

		/// <summary>
		/// Gets the running state of the timer.
		/// </summary>
		public bool IsRunning => _timer.IsRunning;
		
		/// <summary>
		/// Gets or sets the total duration of the timer in seconds.
		/// Exposed in the Unity Inspector to allow easy adjustments.
		/// </summary>
		public float Duration
		{
			get => _timer.Duration;
			set => _timer.Duration = value;
		}

		/// <summary>
		/// Gets the remaining time in seconds.
		/// </summary>
		public float TimeRemaining => _timer.TimeRemaining;

		/// <summary>
		/// Gets the elapsed time in seconds since the timer was started.
		/// </summary>
		public float TimeElapsed => _timer.TimeElapsed;

		/// <summary>
		/// Gets the progress of the timer as a fraction of the elapsed time over the total duration.
		/// </summary>
		public float ProgressElapsed => _timer.ProgressElapsed;

		/// <summary>
		/// Gets the remaining progress of the timer as a fraction.
		/// </summary>
		public float ProgressRemaining => _timer.ProgressRemaining;

		[SerializeField] private float _duration = 10f;
		[SerializeField] private bool _useScaledTime = true;
		[SerializeField] private bool _runOnStart = false;

		private SimpleTimer _timer;

		/// <summary>
		/// Gets the time as either TimeRemaining, TimeElapsed, ProgressElapsed, ProgressRemaining
		/// </summary>
		public virtual float TimeByType(TimeType type) => _timer.TimeByType(type);

		/// <summary>
		/// Starts or restarts the timer.
		/// </summary>
		public virtual void StartTimer() => _timer.StartTimer();
		
		/// <summary>
		///Resumes the timer without resetting.
		/// </summary>
		public virtual void ResumeTimer() => _timer.ResumeTimer();

		/// <summary>
		/// Stops the timer, pausing the countdown.
		/// </summary>
		public virtual void StopTimer() => _timer.StopTimer();

		/// <summary>
		/// Resets the timer to its initial state with the full duration remaining.
		/// </summary>
		public virtual void ResetTimer() => _timer.ResetTimer();

		/// <summary>
		/// Advances the timer forward by a specified amount of time.
		/// </summary>
		/// <param name="time">The duration in seconds to fast forward the timer.</param>
		public virtual void FastForward(float time) => _timer.FastForward(time);

		/// <summary>
		/// Rewinds the timer backward by a specified number of seconds.
		/// </summary>
		/// <param name="time">The amount of time, in seconds, to rewind the timer.</param>
		public virtual void Rewind(float time) => _timer.Rewind(time);

		/// <summary>
		/// Adds a milestone to the timer that will trigger a callback when a specific timer condition is met.
		/// </summary>
		public virtual void AddMilestone(TimerMilestone milestone) => _timer.AddMilestone(milestone);
		
		/// <summary>
		/// Adds a range milestone that triggers at regular intervals within a specified range.
		/// </summary>
		/// <param name="type">The time type (TimeRemaining, TimeElapsed, etc.)</param>
		/// <param name="rangeStart">The start of the range (higher value for TimeRemaining)</param>
		/// <param name="rangeEnd">The end of the range (lower value for TimeRemaining)</param>
		/// <param name="interval">The interval at which to trigger callbacks</param>
		/// <param name="callback">The callback to execute at each interval</param>
		/// <returns>The created TimerRangeMilestone</returns>
		public virtual TimerRangeMilestone AddRangeMilestone(TimeType type, float rangeStart, float rangeEnd, float interval, Action callback) 
			=> _timer.AddRangeMilestone(type, rangeStart, rangeEnd, interval, callback);
		
		/// <summary>
		/// Removes a specific milestone from the timer.
		/// </summary>
		public virtual void RemoveMilestone(TimerMilestone milestone) => _timer.RemoveMilestone(milestone);

		/// <summary>
		/// Removes all milestones associated with the timer. This operation clears any timing checkpoints or markers that have been added.
		/// </summary>
		public void RemoveAllMilestones() => _timer.RemoveAllMilestones();
		
		/// <summary>
		/// Removes milestones that meet a specific condition.
		/// </summary>
		public virtual void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition) => _timer.RemoveMilestonesByCondition(condition);

		protected virtual void Awake()
		{
			_timer = new SimpleTimer(_duration);
		}
		
		protected virtual void OnEnable()
		{
			_timer.OnStart += HandleTimerStart;
			_timer.OnResume += HandleTimerResume;
			_timer.OnStop += HandleTimerStopped;
			_timer.OnComplete += HandleTimerComplete;
			_timer.OnTick += HandleTimerTick;
		}

		protected virtual void OnDisable()
		{
			_timer.OnStart -= HandleTimerStart;
			_timer.OnResume -= HandleTimerResume;
			_timer.OnStop -= HandleTimerStopped;
			_timer.OnComplete -= HandleTimerComplete;
			_timer.OnTick -= HandleTimerTick;
		}

		protected virtual void Start()
		{
			if (!_runOnStart) return;
			
			StartTimer();
		}

		/// <summary>
		/// Updates the timer based on Unity's game time, using scaled or unscaled time as configured.
		/// </summary>
		protected virtual void Update()
		{
			var deltaTime = GetDeltaTime();
			_timer?.Update(deltaTime);
		}
		
		private float GetDeltaTime()
		{
			return _useScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
		}
		
		/// <summary>
		/// Invokes the OnTick event with the current remaining time.
		/// </summary>
		/// <param name="timer">A readonly instance of the timer allowing for time queries</param>
		protected virtual void HandleTimerTick(IReadOnlyTimer timer) => OnTick?.Invoke(timer);

		/// <summary>
		/// Invokes the OnComplete event when the timer completes.
		/// </summary>
		protected virtual void HandleTimerComplete() => OnComplete?.Invoke();

		/// <summary>
		/// Invokes the OnStart event when the timer starts.
		/// </summary>
		protected virtual void HandleTimerStart() => OnStart?.Invoke();

		/// <summary>
		/// Invokes the OnResume event when the timer resumes.
		/// </summary>
		protected virtual void HandleTimerResume() => OnResume?.Invoke();
		
		/// <summary>
		/// Invokes the OnStop event when the timer is stopped.
		/// </summary>
		protected virtual void HandleTimerStopped() => OnStop?.Invoke();
	}
}
