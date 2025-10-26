using System;
using Nonatomic.ServiceKit;

#if TIMERKIT_SERVICEKIT_SUPPORT

namespace Nonatomic.TimerKit.Extensions.ServiceKit
{
	public interface IBaseTimerService : ITimer
	{
	
	}
	
	public abstract class BaseTimerService<T> : ServiceKitBehaviour<T> where T : class, IBaseTimerService
	{
		public float Duration
		{
			get
			{
				EnsureTimerInitialized();
				return Timer.Duration;
			}
			set
			{
				EnsureTimerInitialized();
				Timer.Duration = value;
			}
		}

		public bool IsRunning
		{
			get
			{
				EnsureTimerInitialized();
				return Timer.IsRunning;
			}
		}

		public float TimeRemaining
		{
			get
			{
				EnsureTimerInitialized();
				return Timer.TimeRemaining;
			}
		}

		public float TimeElapsed
		{
			get
			{
				EnsureTimerInitialized();
				return Timer.TimeElapsed;
			}
		}

		public float ProgressElapsed
		{
			get
			{
				EnsureTimerInitialized();
				return Timer.ProgressElapsed;
			}
		}

		public float ProgressRemaining
		{
			get
			{
				EnsureTimerInitialized();
				return Timer.ProgressRemaining;
			}
		}

		protected ITimer Timer;

		private Action _onCompleteHandler;
		private Action _onResumeHandler;

		private Action _onStartHandler;
		private Action _onStopHandler;
		private Action<IReadOnlyTimer> _onTickHandler;
		private Action<float> _onDurationChangedHandler;

		protected override void OnDestroy()
		{
			if (IsServiceReady && Timer != null)
			{
				Timer.OnStart -= _onStartHandler;
				Timer.OnResume -= _onResumeHandler;
				Timer.OnComplete -= _onCompleteHandler;
				Timer.OnStop -= _onStopHandler;
				Timer.OnTick -= _onTickHandler;
				Timer.OnDurationChanged -= _onDurationChangedHandler;
			}

			base.OnDestroy();
		}

		public event Action OnStart;
		public event Action OnComplete;
		public event Action OnResume;
		public event Action OnStop;
		public event Action<IReadOnlyTimer> OnTick;
		public event Action<float> OnDurationChanged;

		public float TimeByType(TimeType type)
		{
			EnsureTimerInitialized();
			return Timer.TimeByType(type);
		}

		public void StartTimer()
		{
			EnsureTimerInitialized();
			Timer.StartTimer();
		}

		public void ResumeTimer()
		{
			EnsureTimerInitialized();
			Timer.ResumeTimer();
		}

		public void StopTimer()
		{
			EnsureTimerInitialized();
			Timer.StopTimer();
		}

		public void ResetTimer()
		{
			EnsureTimerInitialized();
			Timer.ResetTimer();
		}

		public void FastForward(float seconds)
		{
			EnsureTimerInitialized();
			Timer.FastForward(seconds);
		}

		public void Rewind(float seconds)
		{
			EnsureTimerInitialized();
			Timer.Rewind(seconds);
		}

		public void AddMilestone(TimerMilestone milestone)
		{
			EnsureTimerInitialized();
			Timer.AddMilestone(milestone);
		}

		public void RemoveMilestone(TimerMilestone milestone)
		{
			EnsureTimerInitialized();
			Timer.RemoveMilestone(milestone);
		}

		public void RemoveAllMilestones()
		{
			EnsureTimerInitialized();
			Timer.RemoveAllMilestones();
		}

		public void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition)
		{
			EnsureTimerInitialized();
			Timer.RemoveMilestonesByCondition(condition);
		}

		public TimerRangeMilestone AddRangeMilestone(TimeType timeType, float min, float max, float interval, Action action, bool isRecurring = false)
		{
			EnsureTimerInitialized();
			return Timer.AddRangeMilestone(timeType, min, max, interval, action, isRecurring);
		}

		/// <summary>
		/// Ensures the timer is initialized before use.
		/// </summary>
		private void EnsureTimerInitialized()
		{
			if (Timer != null) return;

			Timer = gameObject.AddComponent<Timer>();

			_onStartHandler = () => OnStart?.Invoke();
			_onResumeHandler = () => OnResume?.Invoke();
			_onCompleteHandler = () => OnComplete?.Invoke();
			_onStopHandler = () => OnStop?.Invoke();
			_onTickHandler = timer => OnTick?.Invoke(timer);
			_onDurationChangedHandler = newDuration => OnDurationChanged?.Invoke(newDuration);

			Timer.OnStart += _onStartHandler;
			Timer.OnResume += _onResumeHandler;
			Timer.OnComplete += _onCompleteHandler;
			Timer.OnStop += _onStopHandler;
			Timer.OnTick += _onTickHandler;
			Timer.OnDurationChanged += _onDurationChangedHandler;
		}

		protected override void InitializeService()
		{
			EnsureTimerInitialized();
		}
	}
}
#endif