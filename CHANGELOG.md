# Change Log
## [0.9.0] - 2025-10-26
### Added
- **Convenience API for AddMilestone**: New overload accepting milestone components directly
- `AddMilestone(TimeType type, float triggerValue, Action callback, bool isRecurring = false)` method
- Consistent API pattern - both TimerMilestone and TimerRangeMilestone now support component-based creation
- 9 comprehensive tests for convenience API covering all TimeTypes, recurring behavior, and equivalence with manual creation

### Changed
- ITimer interface extended with new AddMilestone overload

## [0.8.1] - 2025-10-26
### Fixed
- Null reference errors when timer methods or properties accessed before Unity lifecycle initialization
- Added lazy initialization to all Timer and BaseTimerService methods and properties
- OnDisable now safely handles cases where timer is not yet initialized

### Added
- TimerLifecycleTests with 24 tests verifying safe access at any Unity lifecycle point
- Tests cover ResetTimer, StartTimer, properties, milestones, and all timer operations before/after Awake/OnEnable

## [0.8.0] - 2025-10-24
### Added
- **OnDurationChanged Event**: New event triggered when timer Duration property is modified, providing the new duration value
- Event implemented across all timer classes (BasicTimer, Timer, MilestoneTimer, StandardTimer) and service wrappers
- Comprehensive test coverage for OnDurationChanged event behavior including multiple subscribers and edge cases

## [0.7.2] - 2025-01-16
### Fixed
- Recurring milestones triggering multiple times per round due to premature re-addition to trigger lookup
- Indentation issue in MilestoneTimer.cs where nested if statements were not properly aligned

## [0.7.1] - 2025-01-16
### Fixed
- Recurring regular milestones now trigger exactly once per timer round instead of multiple times
- Fixed issue where recurring milestones would trigger multiple times during a single Update() call
- Added `ResetRecurringRegularMilestones()` to properly re-add recurring milestones only when timer resets

### Changed
- Milestones now only trigger when timer is actively running (IsRunning = true)
- Recurring regular milestones are removed from trigger lookup after triggering and re-added on timer reset

### Added
- Added 7 tests verifying milestones don't trigger when timer is not running or stopped
- Added tests for FastForward/Rewind behavior when timer is not running
- Added test verifying milestones trigger correctly after ResumeTimer()
- Added de-duplication logic in milestone processing to prevent duplicate triggers

## [0.7.0] - 2025-01-16
### Added
- **Recurring Milestones**: New `IsRecurring` flag for milestones that persist across timer resets
- Recurring milestones automatically reset and re-trigger in each timer round
- Added `isRecurring` parameter to `AddRangeMilestone()` method with default value `false`
- Comprehensive tests for recurring milestone behavior across multiple timer rounds

### Changed
- **BREAKING**: Updated `AddRangeMilestone()` signature to include optional `isRecurring` parameter
- **BREAKING**: Updated `ITimer` interface to include `isRecurring` parameter
- Range milestones with `IsRecurring=true` are automatically reset when timer resets
- Non-recurring milestones (default) are removed after triggering as before

## [0.6.1] - 2025-01-16
### Added
- Added tests verifying callback time override works through base class and interface references
- Added tests for all overridden time properties (TimeRemaining, TimeElapsed, ProgressElapsed, ProgressRemaining)
- Added test verifying callback override is properly restored after callback execution

## [0.6.0] - 2025-01-16
### Changed
- **BREAKING**: Made TimeRemaining, TimeElapsed, ProgressElapsed, and ProgressRemaining properties virtual in BasicTimer
- Changed MilestoneTimer property hiding (`new`) to proper polymorphic override (`override`)
- This fixes an issue where callback time overrides weren't working through service wrapper chains

### Fixed
- Range milestone callbacks now correctly report interval values when accessed through ITimer interface or service wrappers
- VoiceOverCountdownService and similar implementations now receive correct TimeRemaining values in callbacks

## [0.5.2] - 2025-01-16
### Fixed
- Range milestones now trigger at correct interval values when crossing multiple intervals in a single Update
- Range milestones no longer trigger outside their defined range boundaries
- Added callback time override mechanism so callbacks see the interval value that triggered them
- Added iteration limit safeguard to prevent infinite loops in milestone processing

## [0.5.1] - 2025-01-16
### Changed
- **TimeSourceProvider** now works with any component implementiAdd tests 
- ng `ITimer` interface, not just the concrete `Timer` MonoBehaviour
- Removed `RequireComponent(typeof(Timer))` attribute from TimeSourceProvider for greater flexibility
- TimeSourceProvider now gracefully handles ITimer implementations that don't support SetTimeSource

### Fixed
- TimeSourceExamples.cs removed from package as it was project-specific

## [0.5.0] - 2025-01-16
### Added
- **ITimeSource Interface**: New interface for external time synchronization, allowing timers to use custom time sources
- **TimeSourceProvider Component**: Abstract MonoBehaviour that automatically connects to Timer components as their time source
- **preserveTimeSourceValue Parameter**: New optional parameter in timer constructors to preserve existing time source values when connecting
- Comprehensive tests for ITimeSource functionality and TimeSourceProvider integration

### Changed
- All timer constructors now accept optional ITimeSource parameter for external time synchronization
- Timer.SetTimeSource() method now preserves time source values by default when connecting providers
- Improved code organization with extracted helper methods for better readability and maintainability
- Enhanced variable naming throughout codebase for better self-documentation:
  - `processedMilestones` → `alreadyTriggeredMilestones`
  - `nextTriggerValue` → `lowestUnprocessedTriggerValue`
  - `idsToRemove` → `exhaustedMilestoneIds`
  - `rangeMilestonesToReAdd` → `recurringMilestones`

### Improved
- Code now better adheres to SOLID principles with smaller, focused methods
- Removed unnecessary explanatory comments in favor of self-documenting code
- Fixed typos and improved documentation clarity
- Extracted complex logic into well-named helper methods in MilestoneTimer

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