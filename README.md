# Timers

## Overview
**Timers** is a versatile, easy-to-use timer component designed for Unity projects. Whether you're building a countdown for a game level, managing cooldowns, or triggering events at specific intervals, this package provides a robust solution. It combines basic timing functionality with advanced features, all wrapped in an extensible and Unity-friendly design.

### Features
- **Basic Operations**: Start, stop, reset, and query the timer's state.
- **Pause & Resume**: Pause the timer and pick up where you left off.
- **Fast Forward & Rewind**: Skip ahead or backtrack through time.
- **Milestones**: Trigger custom actions at specific time or progress points.
- **Range Milestones**: Trigger events at regular intervals within a time range.
- **Serialization**: Save and load timer states for persistent gameplay.
- **Unity Integration**: Works seamlessly as a MonoBehaviour or standalone class.
- **Extensible Architecture**: Multiple timer classes for different complexity needs.
- **Service Locator Support**: Optional integration with dependency injection patterns.

## Installation
Add the Timers package to your Unity project via the Unity Package Manager:

1. Open the Package Manager (`Window > Package Manager`).
2. Click the **+** button and select **"Add package from git URL"**.
3. Enter: `https://github.com/PaulNonatomic/Timer.git`.
4. Click **Add**.

## Timer Architecture

The package provides a flexible hierarchy of timer classes to suit different needs:

- **`BasicTimer`**: Pure timer functionality without milestone support (~150 lines)
- **`MilestoneTimer`**: Extends BasicTimer with milestone support
- **`StandardTimer`**: Full-featured timer with all capabilities (recommended for new projects)
- **`Timer`**: Unity MonoBehaviour wrapper for Unity integration
- **`SimpleTimer`**: [DEPRECATED] Alias for StandardTimer (maintained for backward compatibility)

### Interfaces

- **`IReadOnlyTimer`**: Read-only timer properties (TimeRemaining, TimeElapsed, Progress, etc.)
- **`IBasicTimer`**: Basic timer operations extending IReadOnlyTimer
- **`ITimer`**: Full timer functionality with milestone management

## Usage

### Quick Start

#### Unity MonoBehaviour Timer
For Unity integration with Inspector support:

```csharp
using Nonatomic.Timers;
using UnityEngine;

public class CountdownExample : MonoBehaviour
{
    private Timer _timer;

    void Start()
    {
        _timer = gameObject.AddComponent<Timer>();
        _timer.Duration = 30f; // 30 seconds
        _timer.OnComplete += () => Debug.Log("Countdown finished!");
        _timer.StartTimer();
    }
}
```

#### Standalone Timer
For pure C# usage without Unity dependencies:

```csharp
using Nonatomic.Timers;

public class StandaloneExample
{
    private StandardTimer _timer;

    public void StartCountdown()
    {
        _timer = new StandardTimer(30f); // 30 seconds
        _timer.OnComplete += () => Console.WriteLine("Countdown finished!");
        _timer.StartTimer();
        
        // In your update loop:
        // _timer.Update(deltaTime);
    }
}
```

### Basic Operations

```csharp
// Create a timer
var timer = new StandardTimer(10f); // 10 second duration

// Control the timer
timer.StartTimer();    // Start from full duration
timer.ResumeTimer();   // Resume from current position
timer.StopTimer();     // Pause the timer
timer.ResetTimer();    // Reset to full duration

// Time manipulation
timer.FastForward(2f); // Skip ahead 2 seconds
timer.Rewind(1f);      // Go back 1 second

// Query timer state
bool isRunning = timer.IsRunning;
float timeLeft = timer.TimeRemaining;
float elapsed = timer.TimeElapsed;
float progress = timer.ProgressElapsed; // 0.0 to 1.0
```

### Events

```csharp
timer.OnStart += () => Debug.Log("Timer started");
timer.OnResume += () => Debug.Log("Timer resumed");
timer.OnStop += () => Debug.Log("Timer stopped");
timer.OnComplete += () => Debug.Log("Timer completed");
timer.OnTick += (IReadOnlyTimer t) => Debug.Log($"Time: {t.TimeRemaining}");
```

### Milestones

Milestones trigger callbacks when the timer reaches specific points:

```csharp
// Basic milestone - trigger at 5 seconds remaining
var milestone = new TimerMilestone(TimeType.TimeRemaining, 5f, () => {
    Debug.Log("5 seconds left!");
});
timer.AddMilestone(milestone);

// Progress-based milestone - trigger at 75% completion
var progressMilestone = new TimerMilestone(TimeType.ProgressElapsed, 0.75f, () => {
    Debug.Log("75% complete!");
});
timer.AddMilestone(progressMilestone);

// Remove milestones
timer.RemoveMilestone(milestone);
timer.RemoveAllMilestones();
timer.RemoveMilestonesByCondition(m => m.TriggerValue < 3f);
```

### Range Milestones

Range milestones trigger at regular intervals within a specified range:

```csharp
// Trigger every second for the last 10 seconds
var rangeMilestone = timer.AddRangeMilestone(
    TimeType.TimeRemaining, // Type of time to track
    10f,                    // Range start (10 seconds remaining)
    0f,                     // Range end (0 seconds remaining)
    1f,                     // Interval (every 1 second)
    () => Debug.Log("Countdown warning!") // Callback
);

// Trigger every 0.5 seconds from 2-5 seconds elapsed
timer.AddRangeMilestone(
    TimeType.TimeElapsed,
    2f,                     // Start at 2 seconds elapsed
    5f,                     // End at 5 seconds elapsed
    0.5f,                   // Every 0.5 seconds
    () => PlayTickSound()   // Callback
);
```

### TimeType Options

- **`TimeRemaining`**: Time left on the timer (countdown)
- **`TimeElapsed`**: Time passed since timer started
- **`ProgressElapsed`**: Completion progress (0.0 to 1.0)
- **`ProgressRemaining`**: Remaining progress (1.0 to 0.0)

### Advanced Examples

#### Game Level Timer with Warnings

```csharp
public class LevelTimer : MonoBehaviour
{
    private Timer _timer;
    
    void Start()
    {
        _timer = gameObject.AddComponent<Timer>();
        _timer.Duration = 300f; // 5 minutes
        
        // Add warning milestones
        _timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 60f, () => 
            ShowWarning("1 minute remaining!")));
        _timer.AddMilestone(new TimerMilestone(TimeType.TimeRemaining, 30f, () => 
            ShowWarning("30 seconds remaining!")));
            
        // Add countdown for last 10 seconds
        _timer.AddRangeMilestone(TimeType.TimeRemaining, 10f, 0f, 1f, () => 
            PlayCountdownBeep());
            
        _timer.OnComplete += () => EndLevel();
        _timer.StartTimer();
    }
}
```

#### Ability Cooldown System

```csharp
public class AbilityCooldown : MonoBehaviour
{
    [SerializeField] private float _cooldownDuration = 5f;
    private Timer _cooldownTimer;
    
    void Start()
    {
        _cooldownTimer = gameObject.AddComponent<Timer>();
        _cooldownTimer.Duration = _cooldownDuration;
        _cooldownTimer.OnComplete += () => OnCooldownComplete();
    }
    
    public void UseAbility()
    {
        if (_cooldownTimer.IsRunning) return; // Still on cooldown
        
        // Execute ability logic here
        Debug.Log("Ability used!");
        
        // Start cooldown
        _cooldownTimer.StartTimer();
    }
    
    private void OnCooldownComplete()
    {
        Debug.Log("Ability ready!");
    }
    
    public float GetCooldownProgress() => _cooldownTimer.ProgressElapsed;
}
```

### Service Locator Integration

When the `SERVICE_LOCATOR` preprocessor directive is defined, you can use dependency injection:

```csharp
// Custom timer service
public class GameTimerService : BaseTimerService<IGameTimerService>
{
    // Your custom timer logic here
}

// Register and use
ServiceLocator.Register<IGameTimerService>(gameTimerService);
var timerService = ServiceLocator.Get<IGameTimerService>();
```

## Migration Guide

### From SimpleTimer to StandardTimer

If you're using the deprecated `SimpleTimer` class:

**Before:**
```csharp
var timer = new SimpleTimer(10f);
```

**After:**
```csharp
var timer = new StandardTimer(10f);
```

The API is identical, but `StandardTimer` provides better clarity about the class's capabilities.

### Choosing the Right Timer Class

- Use **`BasicTimer`** when you only need start/stop/reset functionality
- Use **`MilestoneTimer`** when you need milestone support but want a lighter class
- Use **`StandardTimer`** for full functionality (recommended for most use cases)
- Use **`Timer`** (MonoBehaviour) for Unity Inspector integration

## Breaking Changes & Migration

### Version 0.4.0 Changes

**⚠️ Deprecation Notice**: `SimpleTimer` has been deprecated in favor of `StandardTimer`. This is **not a breaking change** - all existing code continues to work unchanged, but you'll see compiler warnings.

#### What's Deprecated
- **`SimpleTimer`** class - Use `StandardTimer` instead

#### What's Not Breaking
- **All existing APIs remain identical** - no code changes required
- **Full backward compatibility** maintained
- **All existing functionality** preserved

#### Recommended Updates (Optional)
```csharp
// Old (still works, but shows deprecation warning)
var timer = new SimpleTimer(10f);

// New (recommended for new code)
var timer = new StandardTimer(10f);
```

#### New Architecture Benefits
The new class hierarchy provides better separation of concerns:
- **Smaller classes** for specific needs (BasicTimer for simple cases)
- **Clearer intent** with descriptive names (StandardTimer vs SimpleTimer)
- **Better extensibility** with proper inheritance chain

#### No Action Required
Existing projects can continue using `SimpleTimer` without any changes. The deprecation warning can be suppressed if needed:

```csharp
#pragma warning disable CS0618 // Type or member is obsolete
var timer = new SimpleTimer(10f);
#pragma warning restore CS0618
```