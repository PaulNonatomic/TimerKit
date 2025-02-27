#if SERVICE_LOCATOR

using System;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Nonatomic.Timers.Extensions.ServiceLocator
{
	public interface IBaseTimerService
	{
	
	}
	
	// Create a generic base timer service that works with any interface extending ITimer
	public abstract class BaseTimerService<TInterface> : MonoService<TInterface>, IBaseTimerService
		where TInterface : class, IBaseTimerService
	{
		public event Action OnStart;
		public event Action OnResume;
		public event Action OnStop;
		public event Action OnComplete;
		public event Action<IReadOnlyTimer> OnTick;

		public bool IsRunning => _timer.IsRunning;
		
		public float Duration
		{
			get => _timer.Duration;
			set => _timer.Duration = value;
		}

		public float TimeRemaining => _timer.TimeRemaining;
		public float TimeElapsed => _timer.TimeElapsed;
		public float ProgressElapsed => _timer.ProgressElapsed;
		public float ProgressRemaining => _timer.ProgressRemaining;

		[SerializeField] protected float _duration = 10f;
		[SerializeField] protected bool _useScaledTime = true;
		[SerializeField] protected bool _runOnStart = false;

		protected SimpleTimer _timer;

		public virtual float TimeByType(TimeType type) => _timer.TimeByType(type);
		public virtual void StartTimer() => _timer.StartTimer();
		public virtual void ResumeTimer() => _timer.ResumeTimer();
		public virtual void StopTimer() => _timer.StopTimer();
		public virtual void ResetTimer() => _timer.ResetTimer();
		public virtual void FastForward(float seconds) => _timer.FastForward(seconds);
		public virtual void Rewind(float seconds) => _timer.Rewind(seconds);
		public virtual void AddMilestone(TimerMilestone milestone) => _timer.AddMilestone(milestone);
		public virtual void RemoveMilestone(TimerMilestone milestone) => _timer.RemoveMilestone(milestone);
		public virtual void ClearMilestones() => _timer.ClearMilestones();
		public virtual void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition) => _timer.RemoveMilestonesByCondition(condition);

		protected override void Awake()
		{
			base.Awake();
			_timer = new SimpleTimer(_duration);
			ServiceReady(); // This registers the service
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

		protected virtual void Update()
		{
			var deltaTime = _useScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
			_timer?.Update(deltaTime);
		}
		
		protected virtual void HandleTimerTick(IReadOnlyTimer timer) => OnTick?.Invoke(timer);
		protected virtual void HandleTimerComplete() => OnComplete?.Invoke();
		protected virtual void HandleTimerStart() => OnStart?.Invoke();
		protected virtual void HandleTimerResume() => OnResume?.Invoke();
		protected virtual void HandleTimerStopped() => OnStop?.Invoke();
	}
}

#endif