namespace FireTower.Shared.Contracts;

/// <summary>
/// Marker payload for <see cref="OperationResult{T}"/> instances that carry no data,
/// used in place of a non-generic result type so every operation shares one shape.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = default;
}
