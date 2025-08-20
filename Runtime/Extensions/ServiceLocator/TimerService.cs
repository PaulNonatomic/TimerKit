#if TIMERKIT_SERVICE_LOCATOR_SUPPORT

namespace Nonatomic.TimerKit.Extensions.ServiceLocator
{
	public interface ITimerService : IBaseTimerService
	{
		
	}
	
	public class TimerService : BaseTimerService<ITimerService>, ITimerService
	{
		
	}
}

#endif