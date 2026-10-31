# Map preview before navigation (Valheim 1.0.21)

Initial sampling is ordered from the player and bounded to the existing local preview region.
A conservative local elevation layer is published without passage linking or architectural analysis.
The completed snapshot is immutable. Full sampling, pure worker-thread endpoint preselection,
main-thread passage physics and worker-thread floor analysis continue without blocking this map.
Navigation requests remain pending until the complete graph is ready. Session cancellation cancels
unpublished workers without waiting. Both reveal modes receive the early preview.

Preview samples more than 1.2 m above/below the local floor remain unclassified. This is a local
neighborhood, not a guarantee that an entire architectural floor has been captured.
Endpoint preselection applies the existing slope/jump envelopes only; physics remains authoritative.
The shared scan/exploration scheduler counts active work, not unrelated game processing.
Gameplay timing and first-image latency require testing in the same dungeon/save/settings.
