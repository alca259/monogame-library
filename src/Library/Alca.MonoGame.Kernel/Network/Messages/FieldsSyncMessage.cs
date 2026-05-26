using Alca.MonoGame.Kernel.Network.NetFields;

namespace Alca.MonoGame.Kernel.Network.Messages;

/// <summary>
/// Delta-sync message that carries only the <see cref="NetField"/> values that have changed
/// since the last flush. Payload format:
/// <list type="bullet">
///   <item>NetworkId (uint)</item>
///   <item>FieldCount (byte) — number of dirty fields following</item>
///   <item>For each field: FieldIndex (byte) + serialized value bytes</item>
/// </list>
/// After <see cref="Deserialize"/>, call <see cref="ApplyTo"/> to push received values into local fields.
/// </summary>
public sealed class FieldsSyncMessage : INetworkMessage
{
    /// <summary>Maximum number of <see cref="NetField"/> entries that can be serialized per message.</summary>
    public const int MaxFieldsPerMessage = 64;

    // Raw field-payload bytes captured during Deserialize, replayed by ApplyTo.
    private readonly byte[] _rawPayload = new byte[4096];
    private int _rawPayloadLength;

    /// <inheritdoc/>
    public ushort MessageId => SystemMessageId.FieldsSync;

    /// <summary>Gets or sets the network ID of the entity whose fields are being synced.</summary>
    public uint NetworkId { get; set; }

    // Outbound state — set by PrepareForSend before Serialize is called.
    private NetField[]? _outFields;
    private int _dirtyCount;
    private readonly byte[] _dirtyIndices = new byte[MaxFieldsPerMessage];

    /// <summary>
    /// Prepares the message for sending by scanning <paramref name="fields"/> for dirty entries.
    /// Call immediately before <see cref="Serialize"/>.
    /// </summary>
    /// <param name="networkId">Network ID of the owning entity.</param>
    /// <param name="fields">All registered fields on the entity.</param>
    /// <param name="fieldCount">Number of valid entries in <paramref name="fields"/>.</param>
    public void PrepareForSend(uint networkId, NetField[] fields, int fieldCount)
    {
        NetworkId = networkId;
        _outFields = fields;
        _dirtyCount = 0;

        for (int i = 0; i < fieldCount && i < MaxFieldsPerMessage; i++)
        {
            if (fields[i].IsDirty)
                _dirtyIndices[_dirtyCount++] = (byte)i;
        }
    }

    /// <summary>
    /// Applies the field values captured by the last <see cref="Deserialize"/> call to
    /// <paramref name="fields"/>. Field lookup is by the stored index; out-of-range indices
    /// stop processing to avoid reader corruption.
    /// </summary>
    /// <param name="fields">Registered fields on the receiving entity.</param>
    /// <param name="fieldCount">Number of valid entries in <paramref name="fields"/>.</param>
    public void ApplyTo(NetField[] fields, int fieldCount)
    {
        if (_rawPayloadLength == 0) return;

        var reader = new NetworkReader(new ReadOnlySpan<byte>(_rawPayload, 0, _rawPayloadLength));
        int count = reader.ReadByte();
        for (int i = 0; i < count; i++)
        {
            int idx = reader.ReadByte();
            if (idx < fieldCount)
                fields[idx].Deserialize(ref reader);
            else
                break; // Unknown index: cannot skip without field metadata — stop.
        }
    }

    /// <inheritdoc/>
    public void Serialize(ref NetworkWriter writer)
    {
        writer.Write(NetworkId);
        writer.Write((byte)_dirtyCount);
        for (int i = 0; i < _dirtyCount; i++)
        {
            byte idx = _dirtyIndices[i];
            writer.Write(idx);
            _outFields![idx].Serialize(ref writer);
        }
    }

    /// <summary>
    /// Reads the NetworkId and snapshots the remaining raw field payload bytes so that
    /// <see cref="ApplyTo"/> can apply them once field context is available.
    /// </summary>
    public void Deserialize(ref NetworkReader reader)
    {
        NetworkId = reader.ReadUInt();

        // Snapshot the remaining bytes (FieldCount + [Index + Value] tuples) verbatim.
        _rawPayloadLength = 0;
        while (reader.Remaining > 0 && _rawPayloadLength < _rawPayload.Length)
            _rawPayload[_rawPayloadLength++] = reader.ReadByte();
    }
}
