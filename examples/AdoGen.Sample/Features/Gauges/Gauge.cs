namespace AdoGen.Sample.Features.Gauges;

public enum GaugeState
{
    Idle,
    Running,
    Faulted
}

// Exercises FEATURES.md section B: nested DTOs (F17), unsigned/char defaults (F15),
// enum AsString (F14), Ignore (F11) and struct mapper DTOs (F20).
public static partial class Telemetry
{
    public sealed partial record Gauge(
        Guid Id,
        sbyte Delta,
        ushort Small,
        uint Reads,
        ulong Total,
        ulong? MaybeTotal,
        char Code,
        GaugeState State,
        GaugeState? PreviousState) : ISqlBulkModel, INpgsqlBulkModel
    {
        public string? Cache { get; init; }
    }

    public sealed class GaugeProfile : SqlProfile<Gauge>
    {
        public GaugeProfile()
        {
            RuleFor(x => x.State).AsString(20);
            RuleFor(x => x.PreviousState).AsString(20);
            RuleFor(x => x.Cache).Ignore();
        }
    }

    public sealed class GaugeNpgsqlProfile : NpgsqlProfile<Gauge>
    {
        public GaugeNpgsqlProfile()
        {
            RuleFor(x => x.State).AsString(20);
            RuleFor(x => x.PreviousState).AsString(20);
            RuleFor(x => x.Cache).Ignore();
        }
    }

    public readonly partial record struct GaugeTotal(uint Reads, ulong Total) : ISqlMapper, INpgsqlMapper;
}
