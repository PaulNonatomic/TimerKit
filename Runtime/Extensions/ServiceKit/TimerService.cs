#if TIMERKIT_SERVICEKIT_SUPPORT

using Nonatomic.ServiceKit;

namespace Nonatomic.TimerKit.Extensions.ServiceKit
{
	public interface ITimerService : IBaseTimerService
	{

	}

	[Service(typeof(ITimerService))]
	public class TimerService : BaseTimerService, ITimerService
	{

	}
}

#endif