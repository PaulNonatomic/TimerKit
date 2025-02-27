namespace Nonatomic.Timers.Extensions.ServiceLocator
{
	public interface ITimerService : IBaseTimerService, ITimer
	{
		
	}
	
	public class TimerService : BaseTimerService<ITimerService>, ITimerService
	{
		
	}
}