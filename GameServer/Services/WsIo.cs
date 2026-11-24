using System.Net.WebSockets;

namespace GameServer.Services;

public static class WsIo
{
    // null, если клиент закрыл соединение или прислал слишком большое сообщение
    public static async Task<byte[]?> ReceiveAsync(
        WebSocket ws,
        CancellationToken ct = default)
    {
        var buffer = new byte[1024];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (ws.State == WebSocketState.CloseReceived)
                    await ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Bye",
                        ct);
                return null;
            }

            ms.Write(buffer, 0, result.Count);
            if (ms.Length > 64 * 1024) return null;
        } while (!result.EndOfMessage);

        return ms.ToArray();
    }
}