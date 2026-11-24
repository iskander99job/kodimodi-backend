using GameServer.Models;
using MemoryPack;

namespace GameServer.Services;

public static class WsProtocol
{
    public static byte[] Pack<T>(WsMessageType type, T model)
    {
        var envelope = new WsEnvelopeModel
        {
            Type = type,
            Payload = MemoryPackSerializer.Serialize(model)
        };
        return MemoryPackSerializer.Serialize(envelope);
    }

    public static WsEnvelopeModel Unpack(byte[] data)
        => MemoryPackSerializer.Deserialize<WsEnvelopeModel>(data)!;

    public static T UnpackPayload<T>(WsEnvelopeModel envelopeModel)
        => MemoryPackSerializer.Deserialize<T>(envelopeModel.Payload)!;
}