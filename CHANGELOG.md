# Change Log

## [0.3.7-beta] - Mar 28, 2025
- Fix for Milestones triggering milestone removal during iteration
- Added support for multiple Milestones with the same time rather than overwriting the previous one

## [0.3.6-beta] - Mar 13, 2025
- Added call to ResetTimer in the constructor so the TimeRemaining is set to the duration of the timer

## [0.3.5-beta] - Feb 27, 2025
- Fix for missing pre processor flag in the TimerService class

## [0.3.4-beta] - Feb 27, 2025
- Removed Obsolete Milestone methods

## [0.3.3-beta] - Feb 27, 2025
- Added a new interface for the BaseTimerService this allows derived timers to limit the exposed elements of the timer

## [0.3.2-beta] - Feb 27, 2025
- Made a BaseTimerService that is easier to inherit from

## [0.3.1-beta] - Feb 27, 2025
- Fix: Forgot to add Rewind and FastForward to the ITimer interface and the TimerService
- 
## [0.3.0-beta] - Feb 27, 2025
- Exposed SimpleTimer functionality for Rewind and FastForward in the Timer class

## [0.2.1-beta] - Feb 26, 2025
- Reduced the ServiceLocator version support to 0.5.0

## [0.2.0-beta] - Feb 26, 2025
- Added extension support for ServiceLocator

## [0.1.0-beta] - Feb 26, 2025
- Optimized Milstones
- Added additional test for Milestone removal
- Adjust namespace to be more consistent with other Nonatomic packages

## [0.0.0-beta] - Sept 28, 2024
- First commit