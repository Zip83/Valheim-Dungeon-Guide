using System;
using System.Diagnostics;

namespace DungeonGuide;

/// <summary>
/// Limits cooperative main-thread work by both elapsed time and a maximum number of indivisible steps.
/// </summary>
internal sealed class FrameWorkBudget
{
    private readonly int _maximumSteps;
    private readonly double _maximumMilliseconds;
    private readonly Func<double> _clock;
    private double _started;
    private int _steps;

    /// <summary>Number of admitted operations across all frames; denied reservations do not advance it.</summary>
    public long TotalSteps
    {
        get; private set;
    }

    /// <summary>
    /// Creates a per-frame budget with an optional monotonic millisecond clock for deterministic verification.
    /// </summary>
    /// <param name="maximumSteps">Maximum indivisible operations before another frame is required.</param>
    /// <param name="maximumMilliseconds">Positive finite soft elapsed-time limit for one frame.</param>
    /// <param name="clock">Optional monotonic clock measured in milliseconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">A work or time limit is invalid.</exception>
    public FrameWorkBudget(int maximumSteps, double maximumMilliseconds, Func<double> clock = null)
    {
        if (maximumSteps < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSteps));
        }

        if (!(maximumMilliseconds > 0) || double.IsInfinity(maximumMilliseconds))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMilliseconds));
        }

        _maximumSteps = maximumSteps;
        _maximumMilliseconds = maximumMilliseconds;
        _clock = clock ?? (() => Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency));
        BeginFrame();
    }

    /// <summary>
    /// Resets elapsed time and the step counter after the caller has yielded to a new frame.
    /// </summary>
    public void BeginFrame()
    {
        _started = _clock();
        _steps = 0;
    }

    /// <summary>
    /// Reserves one indivisible step, returning false when the caller must yield before doing more work.
    /// </summary>
    public bool TryStartWork()
    {
        if (_steps >= _maximumSteps || _clock() - _started >= _maximumMilliseconds)
        {
            return false;
        }

        _steps++;
        TotalSteps++;
        return true;
    }
}
