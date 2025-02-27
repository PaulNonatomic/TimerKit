namespace Nonatomic.Timers.Extensions.ServiceLocator
{
	public interface ITimerService : ITimer
	{
		
	}
	
	public class TimerService : BaseTimerService<ITimerService>, ITimerService
	{
		
	}
}