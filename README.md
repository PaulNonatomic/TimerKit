# Timers

## Overview
**Timers** is a versatile, easy-to-use timer component designed for Unity projects. Whether you’re building a countdown for a game level, managing cooldowns, or triggering events at specific intervals, this package provides a robust solution. It combines basic timing functionality with advanced features, all wrapped in an extensible and Unity-friendly design.

### Features
- **Basic Operations**: Start, stop, reset, and query the timer’s state.
- **Pause & Resume**: Pause the timer and pick up where you left off.
- **Fast Forward & Rewind**: Skip ahead or backtrack through time.
- **Milestones**: Trigger custom actions at specific time or progress points.
- **Serialization**: Save and load timer states for persistent gameplay.
- **Unity Integration**: Works seamlessly as a MonoBehaviour or standalone class.
- **Extensible**: Virtual methods and interfaces make customization a breeze.

## Installation
Add the Timers package to your Unity project via the Unity Package Manager:

1. Open the Package Manager (`Window > Package Manager`).
2. Click the **+** button and select **"Add package from git URL"**.
3. Enter: `https://github.com/PaulNonatomic/Timer.git`.
4. Click **Add**.

## Usage
Below are some practical examples to get you started with Timers in your Unity project.

### Example 1: Simple Countdown
Create a 30-second countdown that logs when it’s done.
```csharp
using Timers.Runtime;
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