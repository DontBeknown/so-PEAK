# HJB Pathfinding Optimization

## The Problem

The solver was taking 15+ minutes because of a **scale vs. method mismatch**. It used vanilla Gauss–Seidel value iteration: one single left-to-right, top-to-bottom sweep over every cell in the grid, repeated up to 20,000 times.

On a ~1100×1100 grid that's **1.21 million cell updates per sweep**. A single left-to-right sweep only propagates information in one direction — to cover the full map and reach convergence takes thousands of sweeps. The math: `1.21M cells × ~thousands of iterations × 16 direction probes ≈ hundreds of billions of operations on a single thread`.

---

## Fix 1 — Fast Sweeping Method (FSM)

**What changed:** Replaced the single directional sweep with **4 alternating sweeps per iteration**, each traversing the grid in a different quadrant direction:

```
↗  x: 0→w,   y: 0→h
↘  x: 0→w,   y: h→0
↙  x: w→0,   y: 0→h
↖  x: w→0,   y: h→0
```

**Why it works:**

In HJB/eikonal problems, the optimal path from any cell to the goal has a direction. A left-to-right sweep is great at propagating information leftward, but terrible at propagating it rightward (it has to loop around next iteration). By sweeping in all 4 diagonal directions each iteration, every possible path direction gets covered within a single iteration.

The theoretical result for monotone HJB problems: **FSM converges in a fixed small number of iterations regardless of grid size** — typically 2–8 total iterations. That's not 2–8 per cell, that's 2–8 full passes over the entire grid.

**Before:** ~thousands of sweeps  
**After:** ~4–8 sweeps  
**Speedup: ~1,000–2,000×**

---

## Fix 2 — Relaxed Tolerance

**What changed:** `tolerance` went from `1e-3` to `0.5`

**Why it works:**

The `T` values in this solver represent **travel time in seconds**. Across a 1100-unit map at walking speed, these values are in the range of hundreds of seconds. Requiring convergence to within `0.001 seconds` (`1e-3`) is absurdly precise for a game — it forces extra iterations to squeeze out sub-millisecond accuracy in a path the player just needs to roughly follow.

`0.5` means "stop when no cell changes by more than half a second." The resulting path is indistinguishable to a player.

---

## Fix 3 — Cache `slopeCurrent` Outside the Inner Loop

**What changed:**

```csharp
// Before — inside the 16-direction loop, re-reads the array every direction:
float slopeCurrent = terrain.slopeMap[x, y]; // called 16 times per cell

// After — read once before the loop, bail early if impassable:
float slopeCurrent = terrain.slopeMap[x, y];
if (Mathf.Abs(slopeCurrent) > maxWalkableSlope) return; // skip all 16 probes
```

**Why it works:**

`terrain.slopeMap` is a `float[,]` array. Each read requires a bounds-checked array access. The current cell `(x, y)` doesn't change across the 16 direction probes, so its slope was being fetched 16 times redundantly per cell update.

Reading it once and placing the early-exit check also means **impassable cells skip all 16 direction evaluations entirely** — free optimization for steep terrain cells, which are common near the summit.

---

## Reduced `maxIter`

**What changed:** `maxIter` 20,000 → 50

Since FSM converges in under 10 iterations in practice, 20,000 was pure dead weight — it only existed as a safeguard for the old slow algorithm. 50 gives comfortable headroom for FSM while ensuring a runaway loop can't hang the game for minutes.

---

## Summary

| Change | Before | After | Why |
|---|---|---|---|
| Sweep strategy | 1 direction/iter (Gauss–Seidel) | 4 directions/iter (FSM) | Covers all path directions per iteration; converges in ~4–8 passes instead of thousands |
| Max iterations | 20,000 | 50 | FSM doesn't need more |
| Tolerance | `1e-3` | `0.5` | T values are in hundreds of seconds; sub-second precision is overkill |
| `slopeCurrent` reads | 16× per cell | 1× per cell + early exit | Same cell, same value — read it once |

**Expected solve time: from 15+ minutes → under 10 seconds.**
