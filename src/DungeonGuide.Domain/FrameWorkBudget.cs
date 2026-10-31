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
    private readonly Func<int> _frame;
    private readonly FrameWorkBudget _parent;
    private int _lastFrame = -1;
    private double _started;
    private double _elapsed;
    private bool _running = true;
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
    /// <param name="frame">Optional frame identity preventing multiple consumers from resetting the same frame.</param>
    /// <exception cref="ArgumentOutOfRangeException">A work or time limit is invalid.</exception>
    public FrameWorkBudget(int maximumSteps, double maximumMilliseconds, Func<double> clock = null, Func<int> frame = null)
        : this(maximumSteps, maximumMilliseconds, clock, frame, null)
    {
    }

    private FrameWorkBudget(int maximumSteps, double maximumMilliseconds, Func<double> clock, Func<int> frame,
        FrameWorkBudget parent)
    {
        if (maximumSteps < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSteps));
        }

        if (!(maximumMilliseconds > 0) || double.IsInfinity(maximumMilliseconds))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMilliseconds));
        }

        _frame = frame;
        _parent = parent;
        _maximumSteps = maximumSteps;
        _maximumMilliseconds = maximumMilliseconds;
        _clock = clock ?? (() => Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency));
        BeginFrame();
    }

    /// <summary>Limits a consumer to a fraction of both limits while charging its admitted steps to this parent.</summary>
    /// <param name="fraction">Positive share no greater than one; the parent still bounds total frame work.</param>
    /// <returns>A temporary view; creating or resetting it never replenishes the parent's allowance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The fraction is outside the interval (0, 1].</exception>
    public FrameWorkBudget CreateSlice(double fraction)
    {
        if (!(fraction > 0 && fraction <= 1))
        {
            throw new ArgumentOutOfRangeException(nameof(fraction));
        }

        return new FrameWorkBudget(Math.Max(1, (int)(_maximumSteps * fraction)),
            _maximumMilliseconds * fraction, _clock, null, this);
    }

    /// <summary>
    /// Resets elapsed time and the step counter after the caller has yielded to a new frame.
    /// </summary>
    public void BeginFrame()
    {
        if (_frame != null)
        {
            var current = _frame();
            if (_lastFrame == current)
            {
                return;
            }

            _lastFrame = current;
        }

        _started = _clock();
        _elapsed = 0;
        _steps = 0;
    }

    /// <summary>
    /// Stops charging the budget while control belongs to unrelated game processing.
    /// </summary>
    public void Pause()
    {
        if (_running)
        {
            _elapsed += _clock() - _started;
            _running = false;
        }
    }

    /// <summary>Resumes charging active work after unrelated game processing or a coroutine suspension.</summary>
    public void Resume()
    {
        if (!_running)
        {
            _started = _clock();
            _running = true;
        }
    }

    /// <summary>Reserves one cooperative work step within the current frame.</summary>
    public bool TryStartWork()
    {
        if (_steps >= _maximumSteps || _elapsed + (_running ? _clock() - _started : 0) >= _maximumMilliseconds)
        {
            return false;
        }

        if (_parent != null && !_parent.TryStartWork())
        {
            return false;
        }

        _steps++;
        TotalSteps++;
        return true;
    }
}
