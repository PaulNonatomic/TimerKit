# Change Log
## [0.4.3] - Sep 2, 2025
- Hotfix for ServiceKit update

## [0.4.2] - Aug 20, 2025
- The Timers package has been renamed to TimerKit

## [0.4.1] - Aug 20, 2025
- Added ServiceKit support

## [0.4.0] - Aug 20, 2025
- No longer in beta

### Added
- **Range Milestones**: New `TimerRangeMilestone` class for triggering events at regular intervals within a time range
- **New Timer Architecture**: Introduced `BasicTimer`, `MilestoneTimer`, and `StandardTimer` for better separation of concerns
- **Interface Segregation**: Added `IBasicTimer` interface for basic timer operations
- **AddRangeMilestone()** method to all timer interfaces and implementations
- Comprehensive milestone edge case handling (self-removal, dynamic addition, multiple milestones with same trigger value)
- Enhanced test coverage for complex milestone scenarios

### Changed
- **BREAKING**: `SimpleTimer` is now deprecated in favor of `StandardTimer` (backward compatible with obsolete warning)
- Refactored timer inheritance hierarchy: `BasicTimer` → `MilestoneTimer` → `StandardTimer`
- `ITimer` interface now inherits from `IBasicTimer` for better interface segregation
- Updated all internal references to use `StandardTimer` instead of `SimpleTimer`
- Improved milestone processing to handle complex interaction scenarios
- Enhanced self-documenting code patterns throughout codebase

### Fixed
- Multiple milestones with the same trigger value now all execute correctly
- Milestone self-removal during callback execution no longer causes issues
- Dynamic milestone addition during milestone execution now works properly
- Range milestone reset functionality now works correctly after timer reset

### Deprecated
- **`SimpleTimer`** class is deprecated; use `StandardTimer` instead (maintains full backward compatibility)

### Migration Guide
**No breaking changes** - existing code continues to work unchanged.

For new projects, prefer:
- `BasicTimer` for simple timing needs
- `MilestoneTimer` when you need milestone support
- `StandardTimer` for full functionality (recommended)
- Replace `new SimpleTimer()` with `new StandardTimer()` when convenient

## [0.3.8-beta] - Jul 26, 2025
- Removed empty assembly

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