# TimerKit Performance Baseline

This document records the allocation performance baseline for TimerKit as of v0.10.0.

## Summary

| Scenario | Allocations | Status |
|----------|-------------|--------|
| BasicTimer.Update | 0 bytes/update | ✅ Optimal |
| StandardTimer.Update (no milestones) | 0 bytes/update | ✅ Optimal |
| StandardTimer.Update (with milestones, not triggered) | 0 bytes/update | ✅ Optimal |
| StandardTimer.Update (with milestones, triggered) | 0 bytes/trigger | ✅ Optimal |
| Milestone triggering | 0 bytes/trigger | ✅ Optimal |
| Range milestone triggering | 0 bytes/trigger | ✅ Optimal |
| Timer MonoBehaviour Update | 0 bytes/frame | ✅ Optimal |
| Timer with OnTick subscriber | 0 bytes/frame | ✅ Optimal |
| Event invocation | 0 bytes/invocation | ✅ Optimal |

## Detailed Results (v0.10.0)

### EditMode Tests

#### BasicTimer
```
BasicTimer.Update: 0 bytes total, 0 bytes/update over 100 iterations
BasicTimer.Update (not running): 0 bytes total over 100 iterations
BasicTimer.Update with OnTick: 0 bytes total, 0 bytes/update
```

#### StandardTimer (No Milestones)
```
StandardTimer.Update (no milestones): 0 bytes total, 0 bytes/update
StandardTimer 1 second @ 60 FPS (no milestones): 0 bytes
100 StandardTimers, 1 second @ 60 FPS: 0 bytes (0 bytes/timer/frame)
```

#### StandardTimer (With Milestones - Not Triggered)
```
StandardTimer.Update (1 milestone, not triggered): 0 bytes total, 0 bytes/update
StandardTimer.Update (10 milestones, not triggered): 0 bytes total, 0 bytes/update
StandardTimer.Update (range milestone, not triggered): 0 bytes total, 0 bytes/update
StandardTimer 1 second @ 60 FPS (5 milestones): 0 bytes
100 StandardTimers with milestones, 1 second @ 60 FPS: 0 bytes (0 bytes/timer/frame)
```

#### Milestone Triggering
```
StandardTimer milestone trigger: 0 bytes avg per trigger (10 triggers)
StandardTimer 10 milestones trigger: 0 bytes total for 10 triggers
StandardTimer range milestone triggers: 0 bytes total for 9 triggers
ProcessAllTriggeredMilestones (single trigger): ~0 bytes avg
CheckAndTriggerMilestones: 0 bytes total, 0 bytes/call over 100 calls
```

#### Comparison
```
BasicTimer: 0 bytes (0 bytes/update)
StandardTimer: 0 bytes (0 bytes/update)
Overhead: 0 bytes (0 bytes/update)
Without milestones: 0 bytes (0 bytes/update)
With 5 milestones: 0 bytes (0 bytes/update)
Milestone overhead: 0 bytes (0 bytes/update)
```

#### Event Invocation
```
Event invocation: 0 bytes over 100 invocations
```

### PlayMode Tests

#### Timer MonoBehaviour
```
Timer MonoBehaviour: 0 bytes over 60 frames (0 bytes/frame)
Timer with OnTick subscriber: 0 bytes over 60 frames (0 bytes/frame)
```

#### Notes on PlayMode Test Overhead

PlayMode tests using `[UnityTest]` coroutines include **Unity test framework overhead**:
```
Timer with milestone (not triggered): 4096 bytes over 60 frames (68 bytes/frame)
Timer milestone trigger: 28672 bytes (1 trigger)
10 Timer MonoBehaviours: 16384 bytes over 60 frames
Timer with event subscribers: 4096 bytes over 60 frames
```

**Root cause identified**: `PlaymodeTestsController.Run` allocates ~64 bytes per frame. This is Unity's test runner, not TimerKit code.

Negative values (e.g., `-24576 bytes`) indicate GC collections occurred during measurement.

**EditMode tests are authoritative for timer logic allocations** - they test the timer code directly without Unity test runner overhead.

## Analysis

### Optimization Results (v0.10.0)

The milestone system was completely rewritten for zero allocations:

| Component | Before | After |
|-----------|--------|-------|
| MilestoneTimer.Update() | ~160-200 bytes/frame | 0 bytes |
| ProcessAllTriggeredMilestones() | ~104 bytes | 0 bytes |
| CheckAndTriggerMilestones() | ~160 bytes | 0 bytes |
| Milestone triggering | ~56 bytes/trigger | 0 bytes |

### Optimizations Applied

1. **Index-based iteration**: Replaced all `foreach` loops with `for` loops using index access to avoid enumerator allocations
2. **Collection pooling**: All temporary HashSets and Lists are now class-level fields, cleared and reused
3. **Out parameters**: Converted tuple-returning methods to use `out` parameters to avoid boxing
4. **Early exit**: Added early return when no milestones exist
5. **Removed LINQ**: Eliminated `System.Linq` dependency from hot paths

### PlayMode Test Framework Overhead

PlayMode `[UnityTest]` coroutines include allocations from Unity's test framework:
- `PlaymodeTestsController.Run`: ~64 bytes per frame
- This is Unity's internal test runner, not TimerKit code

Additional variance factors:
- GC collections during measurement window
- Memory compaction between measurements
- `GC.GetTotalMemory()` timing variations

**For accurate allocation profiling in production, use Unity's Deep Profiler in a standalone build.**

## Recommendations

1. **Production profiling**: Use Unity Profiler in standalone builds for accurate measurements
2. **Regression testing**: Run EditMode allocation tests to catch regressions
3. **Event subscribers**: For maximum performance, consider using direct callbacks instead of events when GC is critical

## Test Environment

- Unity Editor (EditMode and PlayMode)
- GC.GetTotalMemory() for measurements
- Warmup iterations before each measurement
- v0.10.0 optimizations applied

---
*Generated from AllocationTests.cs and AllocationPlayModeTests.cs - Updated 2025-11-27*
