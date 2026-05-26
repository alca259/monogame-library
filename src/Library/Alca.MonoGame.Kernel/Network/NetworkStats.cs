namespace Alca.MonoGame.Kernel.Network;

/// <summary>Snapshot of network statistics for a connected peer.</summary>
public readonly struct NetworkStats
{
    /// <summary>Gets the total number of packets sent since the connection was established.</summary>
    public int PacketsSentTotal { get; init; }

    /// <summary>Gets the total number of packets received since the connection was established.</summary>
    public int PacketsReceivedTotal { get; init; }

    /// <summary>Gets the total bytes sent since the connection was established.</summary>
    public long BytesSentTotal { get; init; }

    /// <summary>Gets the total bytes received since the connection was established.</summary>
    public long BytesReceivedTotal { get; init; }

    /// <summary>Gets the estimated packet loss as a percentage (0–100).</summary>
    public int PacketLossPercent { get; init; }

    /// <summary>Gets the round-trip latency in milliseconds.</summary>
    public int Ping { get; init; }
}
