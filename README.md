# Timers #

## Overview ##
Timer is a versatile, easy-to-use timer component designed for Unity projects. It supports basic timing operations like start, stop, and reset, along with advanced features such as pausing, resuming, fast forwarding, rewinding, and handling milestones that trigger custom actions.

### Features
- Basic Timer Operations: Start, stop, and reset the timer.
- Pause and Resume: Pause the timer and resume from the last stopped point without resetting.
- Fast Forward and Rewind: Jump forward or backward in time.
- **Milestones:** Set up callbacks to execute when the timer reaches certain points.
- **Serialization:** Serialize and deserialize the timer's state, useful for game saves.
- **Extensible:** Easy to extend with additional features and integrate into larger systems.

## Installation ##
To install the Timers package in your Unity project, follow these steps:

1. Open Unity and navigate to the Package Manager.
   - Click on the + button and select Add package from git URL...
   - Enter the following URL: https://github.com/PaulNonatomic/Timer and press Add.
2. Add the Timer component onto any active GameObject in your scene, or create a new GameObject dedicated to the timer.

## Usage ## 

## API Reference ##
* StartTimer(): Start or restart the timer.
* StopTimer(): Pause the timer.
* ResetTimer(): Reset the timer to its initial state.
* ResumeTimer(): Resume the timer from the last paused state.
* FastForward(float seconds): Advance the timer by a specified number of seconds.
* Rewind(float seconds): Reverse the timer by a specified number of seconds.
* AddMilestone(TimerMilestone milestone): Add a milestone that triggers a callback when reached.
* RemoveMilestone(TimerMilestone milestone): Remove a milestone from the timer.
* ClearMilestones(): Clears all milestones from the timer, ceasing any pending triggers.
* RemoveMilestonesByCondition(Predicate<TimerMilestone> condition): Removes a specific milestone from the timer.

## Contributing ##
Contributions are welcome! Please refer to CONTRIBUTING.md for guidelines on how to contribute.

## License ##
The Timers package is licensed under the MIT license. See LICENSE for more details.
