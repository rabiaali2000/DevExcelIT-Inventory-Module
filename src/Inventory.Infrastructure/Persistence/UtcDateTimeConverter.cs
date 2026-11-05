using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Inventory.Infrastructure.Persistence;

// SQLite stores dates as text without a time zone, so values come back as DateTimeKind.Unspecified.
// Every DateTime we store is UTC, so mark values read back from the database as UTC.
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
