#if TIMERS_SERVICE_KIT

namespace Nonatomic.Timers.Extensions.ServiceKit
{
	public interface ITimerService : IBaseTimerService
	{
		
	}
	
	public class TimerService : BaseTimerService<ITimerService>, ITimerService
	{
		
	}
}

#endif