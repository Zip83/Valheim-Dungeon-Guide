using UnityEngine;

namespace DungeonGuide;

/// <summary>Waits for one native door animation to settle before deciding whether floor geometry changed.</summary>
internal sealed class DoorGeometryObservation
{
    private readonly DoorGeometrySnapshot _before;
    private DoorGeometrySnapshot _latest;
    private readonly Animator _animator;
    private float _stableSince;

    /// <summary>Captures the pose immediately after the state request, before the animator advances.</summary>
    public DoorGeometryObservation(Door door, float now)
    {
        Door = door;
        _before = _latest = new DoorGeometrySnapshot(door);
        _animator = door.GetComponentInChildren<Animator>();
        _stableSince = now;
    }

    /// <summary>Native object whose motion is being observed.</summary>
    public Door Door
    {
        get;
    }

    /// <summary>Whether settled motion added, removed or moved floor-like solid coverage.</summary>
    public bool FloorChanged => _before.ChangesFloor(_latest);

    /// <summary>Restarts settling on a repeated state change while preserving the original floor coverage.</summary>
    public void Restart(float now) => _stableSince = now;

    /// <summary>Samples collider coverage until it remains stable and the native animation has finished.</summary>
    public bool Settled(float now)
    {
        var current = new DoorGeometrySnapshot(Door);
        if (!_latest.Matches(current))
        {
            _latest = current;
            _stableSince = now;
        }

        return now - _stableSince >= 0.5f && !Animating();
    }

    private bool Animating()
    {
        if (!_animator || !_animator.isActiveAndEnabled || !_animator.runtimeAnimatorController)
        {
            return false;
        }

        var state = _animator.GetCurrentAnimatorStateInfo(0);
        return _animator.IsInTransition(0) || (!state.loop && state.normalizedTime < 1);
    }
}
