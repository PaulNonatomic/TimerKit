#if TIMERKIT_SERVICE_LOCATOR_SUPPORT

using System;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Nonatomic.TimerKit.Extensions.ServiceLocator
{
	public interface IBaseTimerService : ITimer
	{
	
	}
	
	public abstract class BaseTimerService<TInterface> : MonoService<TInterface>, IBaseTimerService
		where TInterface : class, IBaseTimerService
	{
		public event Action OnStart;
		public event Action OnResume;
		public event Action OnStop;
		public event Action OnComplete;
		public event Action<IReadOnlyTimer> OnTick;
		public event Action<float> OnDurationChanged;

		public bool IsRunning
		{
			get
			{
				InitializeTimerIfNeeded();
				return _timer.IsRunning;
			}
		}

		public float Duration
		{
			get
			{
				InitializeTimerIfNeeded();
				return _timer.Duration;
			}
			set
			{
				InitializeTimerIfNeeded();
				_timer.Duration = value;
			}
		}

		public float TimeRemaining
		{
			get
			{
				InitializeTimerIfNeeded();
				return _timer.TimeRemaining;
			}
		}

		public float TimeElapsed
		{
			get
			{
				InitializeTimerIfNeeded();
				return _timer.TimeElapsed;
			}
		}

		public float ProgressElapsed
		{
			get
			{
				InitializeTimerIfNeeded();
				return _timer.ProgressElapsed;
			}
		}

		public float ProgressRemaining
		{
			get
			{
				InitializeTimerIfNeeded();
				return _timer.ProgressRemaining;
			}
		}

		public virtual float TimeByType(TimeType type)
		{
			InitializeTimerIfNeeded();
			return _timer.TimeByType(type);
		}

		public virtual void StartTimer()
		{
			InitializeTimerIfNeeded();
			_timer.StartTimer();
		}

		public virtual void ResumeTimer()
		{
			InitializeTimerIfNeeded();
			_timer.ResumeTimer();
		}

		public virtual void StopTimer()
		{
			InitializeTimerIfNeeded();
			_timer.StopTimer();
		}

		public virtual void ResetTimer()
		{
			InitializeTimerIfNeeded();
			_timer.ResetTimer();
		}

		public virtual void FastForward(float seconds)
		{
			InitializeTimerIfNeeded();
			_timer.FastForward(seconds);
		}

		public virtual void Rewind(float seconds)
		{
			InitializeTimerIfNeeded();
			_timer.Rewind(seconds);
		}

		public virtual void AddMilestone(TimerMilestone milestone)
		{
			InitializeTimerIfNeeded();
			_timer.AddMilestone(milestone);
		}

		public virtual TimerMilestone AddMilestone(TimeType type, float triggerValue, Action callback, bool isRecurring = false)
		{
			var milestone = new TimerMilestone(type, triggerValue, callback, isRecurring);
			AddMilestone(milestone);
			return milestone;
		}

		public virtual void AddRangeMilestone(TimerRangeMilestone rangeMilestone)
		{
			InitializeTimerIfNeeded();
			_timer.AddRangeMilestone(rangeMilestone);
		}

		public virtual TimerRangeMilestone AddRangeMilestone(TimeType type, float rangeStart, float rangeEnd, float interval, Action callback, bool isRecurring = false)
		{
			InitializeTimerIfNeeded();
			return _timer.AddRangeMilestone(type, rangeStart, rangeEnd, interval, callback, isRecurring);
		}

		public virtual void RemoveMilestone(TimerMilestone milestone)
		{
			InitializeTimerIfNeeded();
			_timer.RemoveMilestone(milestone);
		}

		public virtual void RemoveAllMilestones()
		{
			InitializeTimerIfNeeded();
			_timer.RemoveAllMilestones();
		}

		public virtual void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition)
		{
			InitializeTimerIfNeeded();
			_timer.RemoveMilestonesByCondition(condition);
		}

		[SerializeField] protected float _duration = 10f;
		[SerializeField] protected bool _useScaledTime = true;
		[SerializeField] protected bool _runOnStart = false;

		protected StandardTimer _timer;

		protected override void Awake()
		{
			base.Awake();
			InitializeTimerIfNeeded();
			ServiceReady();
		}

		/// <summary>
		/// Initializes the timer if it hasn't been initialized yet.
		/// </summary>
		private void InitializeTimerIfNeeded()
		{
			if (_timer == null)
			{
				_timer = new StandardTimer(_duration);
			}
		}

		protected virtual void OnEnable()
		{
			InitializeTimerIfNeeded();
			_timer.OnStart += HandleTimerStart;
			_timer.OnResume += HandleTimerResume;
			_timer.OnStop += HandleTimerStopped;
			_timer.OnComplete += HandleTimerComplete;
			_timer.OnTick += HandleTimerTick;
			_timer.OnDurationChanged += HandleDurationChanged;
		}

		protected virtual void OnDisable()
		{
			if (_timer == null) return;

			_timer.OnStart -= HandleTimerStart;
			_timer.OnResume -= HandleTimerResume;
			_timer.OnStop -= HandleTimerStopped;
			_timer.OnComplete -= HandleTimerComplete;
			_timer.OnTick -= HandleTimerTick;
			_timer.OnDurationChanged -= HandleDurationChanged;
		}

		protected virtual void Start()
		{
			if (!_runOnStart) return;
			
			StartTimer();
		}

		protected virtual void Update()
		{
			var deltaTime = GetDeltaTime();
			_timer?.Update(deltaTime);
		}
		
		private float GetDeltaTime()
		{
			return _useScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
		}
		
		protected virtual void HandleTimerTick(IReadOnlyTimer timer) => OnTick?.Invoke(timer);
		protected virtual void HandleTimerComplete() => OnComplete?.Invoke();
		protected virtual void HandleTimerStart() => OnStart?.Invoke();
		protected virtual void HandleTimerResume() => OnResume?.Invoke();
		protected virtual void HandleTimerStopped() => OnStop?.Invoke();
		protected virtual void HandleDurationChanged(float newDuration) => OnDurationChanged?.Invoke(newDuration);
	}
}

#endif