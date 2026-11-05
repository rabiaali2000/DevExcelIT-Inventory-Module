namespace Inventory.UnitTests.Application;

// A clock that always returns the same moment, so timestamps in tests are predictable.
internal sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(utcNow);
}
