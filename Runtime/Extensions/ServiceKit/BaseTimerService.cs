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
			get => Timer.Duration;
			set => Timer.Duration = value;
		}

		public bool IsRunning => Timer.IsRunning;
		public float TimeRemaining => Timer.TimeRemaining;
		public float TimeElapsed => Timer.TimeElapsed;
		public float ProgressElapsed => Timer.ProgressElapsed;
		public float ProgressRemaining => Timer.ProgressRemaining;
		
		protected ITimer Timer;
		
		private Action _onCompleteHandler;
		private Action _onResumeHandler;

		private Action _onStartHandler;
		private Action _onStopHandler;
		private Action<IReadOnlyTimer> _onTickHandler;

		protected override void OnDestroy()
		{
			if (IsServiceReady && Timer != null)
			{
				Timer.OnStart -= _onStartHandler;
				Timer.OnResume -= _onResumeHandler;
				Timer.OnComplete -= _onCompleteHandler;
				Timer.OnStop -= _onStopHandler;
				Timer.OnTick -= _onTickHandler;
			}

			base.OnDestroy();
		}

		public event Action OnStart;
		public event Action OnComplete;
		public event Action OnResume;
		public event Action OnStop;
		public event Action<IReadOnlyTimer> OnTick;

		public float TimeByType(TimeType type)
		{
			return Timer.TimeByType(type);
		}

		public void StartTimer()
		{
			Timer.StartTimer();
		}

		public void ResumeTimer()
		{
			Timer.ResumeTimer();
		}

		public void StopTimer()
		{
			Timer.StopTimer();
		}

		public void ResetTimer()
		{
			Timer.ResetTimer();
		}

		public void FastForward(float seconds)
		{
			Timer.FastForward(seconds);
		}

		public void Rewind(float seconds)
		{
			Timer.Rewind(seconds);
		}

		public void AddMilestone(TimerMilestone milestone)
		{
			Timer.AddMilestone(milestone);
		}

		public void RemoveMilestone(TimerMilestone milestone)
		{
			Timer.RemoveMilestone(milestone);
		}

		public void RemoveAllMilestones()
		{
			Timer.RemoveAllMilestones();
		}

		public void RemoveMilestonesByCondition(Predicate<TimerMilestone> condition)
		{
			Timer.RemoveMilestonesByCondition(condition);
		}

		public TimerRangeMilestone AddRangeMilestone(TimeType timeType, float min, float max, float interval, Action action)
		{
			return Timer.AddRangeMilestone(timeType, min, max, interval, action);
		}

		protected override void InitializeService()
		{
			Timer ??= gameObject.AddComponent<Timer>();

			_onStartHandler = () => OnStart?.Invoke();
			_onResumeHandler = () => OnResume?.Invoke();
			_onCompleteHandler = () => OnComplete?.Invoke();
			_onStopHandler = () => OnStop?.Invoke();
			_onTickHandler = timer => OnTick?.Invoke(timer);

			Timer.OnStart += _onStartHandler;
			Timer.OnResume += _onResumeHandler;
			Timer.OnComplete += _onCompleteHandler;
			Timer.OnStop += _onStopHandler;
			Timer.OnTick += _onTickHandler;
		}
	}
}
#endif