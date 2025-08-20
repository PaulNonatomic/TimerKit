#if TIMERKIT_SERVICEKIT_SUPPORT

namespace Nonatomic.TimerKit.Extensions.ServiceKit
{
	public interface ITimerService : IBaseTimerService
	{
		
	}
	
	public class TimerService : BaseTimerService<ITimerService>, ITimerService
	{
		
	}
}

#endif