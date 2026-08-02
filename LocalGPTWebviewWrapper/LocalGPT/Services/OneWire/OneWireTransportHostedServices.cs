using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

public sealed class OneWireTcpHostedService(
    IOptions<OneWireOptions> options,
    IOneWireEnvelopeCodec codec,
    IOneWireRuntimeSecurityService security,
    IOneWireMessageDispatcher dispatcher,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireTransportSecurityPolicy transportSecurityPolicy,
    IOneWireDispatchContextFactory dispatchContextFactory,
    IOneWireListenAddressResolver listenAddressResolver,
    ISupervisedTaskRunner taskRunner,
    ILogger<OneWireTcpHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;
        TcpListener? listener = null;
        try
        {
            var listenAddress = listenAddressResolver.Resolve(options.Value);
            listener = new TcpListener(listenAddress, Program.OneWirePort);
            listener.Start();
            logger.LogInformation("LocalGPT 1-Wire service listening on {Address}:{Port}. LAN transport is {LanState}.",
                listenAddress, Program.OneWirePort, options.Value.EnableLanTransport ? "enabled" : "disabled");
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                taskRunner.Run(
                    nameof(OneWireTcpHostedService),
                    "HandleClient",
                    token => HandleClientAsync(client, token),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (SocketException ex)
        {
            logger.LogError(ex, "The optional 1-Wire TCP listener could not bind to port {Port}. LocalGPT web/installer bootstrap remains active.", Program.OneWirePort);
        }
        finally { listener?.Stop(); }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        string peerId = string.Empty;
        var registrationId = Guid.Empty;
        var transportConnectionId = Guid.NewGuid();
        var remoteAddress = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;
        var isLoopback = transportSecurityPolicy.IsLoopback(remoteAddress);
        var writeGate = new SemaphoreSlim(1, 1);
        try
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 8192, leaveOpen: true) { AutoFlush = true })
            {
                async Task Sender(OneWireEnvelope message, CancellationToken token)
                {
                    await security.ProtectOutgoingAsync(message, token).ConfigureAwait(false);
                    if (!isLoopback && transportSecurityPolicy.RequiresProtectedTransport(message.MessageType) &&
                        !transportSecurityPolicy.IsProtected(message))
                    {
                        throw new System.Security.Cryptography.CryptographicException(
                            "A non-loopback 1-Wire connection requires MFA-verified message protection before application data can be sent.");
                    }
                    await writeGate.WaitAsync(token).ConfigureAwait(false);
                    try { await writer.WriteLineAsync(codec.Serialize(message).AsMemory(), token).ConfigureAwait(false); }
                    finally { writeGate.Release(); }
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (line is null)
                        break;
                    if (Encoding.UTF8.GetByteCount(line) > Math.Clamp(options.Value.MaximumMessageBytes, 4096, OneWireProtocol.MaximumMessageBytes))
                        throw new InvalidDataException("The 1-Wire message is too large.");
                    var envelope = codec.DeserializeAndValidate(line);
                    await security.UnprotectIncomingAsync(envelope, cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(peerId))
                    {
                        if (envelope.MessageType != OneWireMessageType.Hello || string.IsNullOrWhiteSpace(envelope.SourcePeerId))
                            throw new InvalidDataException("A TCP 1-Wire connection must establish its peer identity with Hello before sending other messages.");
                        if (string.Equals(envelope.SourcePeerId, "localgpt", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("An external 1-Wire connection cannot claim the LocalGPT internal peer identity.");
                        peerId = envelope.SourcePeerId;
                        registrationId = connections.RegisterOwned(peerId, Sender);
                    }
                    else if (!string.Equals(envelope.SourcePeerId, peerId, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("The 1-Wire SourcePeerId changed after the transport identity was established.");
                    }

                    var context = dispatchContextFactory.CreateExternal(peerId, transportConnectionId, isLoopback, "tcp");
                    var response = await dispatcher.DispatchAsync(envelope, context, cancellationToken).ConfigureAwait(false);
                    if (response is not null)
                        await Sender(response, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or SocketException or System.Security.Cryptography.CryptographicException)
        {
            logger.LogWarning(ex, "A 1-Wire peer connection ended after a protocol, security, or transport error.");
        }
        finally
        {
            writeGate.Dispose();
            if (!string.IsNullOrWhiteSpace(peerId) && registrationId != Guid.Empty && connections.Unregister(peerId, registrationId))
                peers.SetConnected(peerId, false);
        }
    }


}

public sealed class OneWireDiscoveryHostedService(
    IOptions<OneWireOptions> options,
    IOneWireRuntimeSecurityService security,
    IOneWireEnvelopeCodec codec,
    ILogger<OneWireDiscoveryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || !options.Value.EnableDiscovery)
            return;
        using var udp = new UdpClient { EnableBroadcast = true };
        var interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.BroadcastIntervalSeconds, 2, 60));
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                var address = options.Value.EnableLanTransport
                    ? (IPAddress.TryParse(options.Value.BroadcastAddress, out var parsed) ? parsed : IPAddress.Broadcast)
                    : IPAddress.Loopback;
                var publicSecurity = await security.GetPublicDescriptorAsync(stoppingToken).ConfigureAwait(false);
                var advertisement = new OneWirePeerAdvertisement
                {
                    PeerId = "localgpt",
                    DisplayName = "LocalGPT",
                    Application = "LocalGPT",
                    ApplicationVersion = "2.1.21-organic-wire",
                    HostName = Environment.MachineName,
                    Address = options.Value.EnableLanTransport ? "0.0.0.0" : "127.0.0.1",
                    ServicePort = Program.OneWirePort,
                    DiscoveryPort = Program.OneWireDiscoveryPort,
                    WebBaseUrl = Program.BaseUrl,
                    IsConnected = true,
                    TransportKind = OneWireTransportKind.Tcp,
                    SupportedTransports = ["tcp", "http-json"],
                    Security = publicSecurity
                };
                // UDP is only the small discovery beacon. The full DXFunction/skill/hardware directory is
                // requested over the established TCP link after both frontends approve the connection.
                var bytes = JsonSerializer.SerializeToUtf8Bytes(advertisement, codec.JsonOptions);
                if (bytes.Length > OneWireProtocol.MaximumDiscoveryBytes)
                    throw new InvalidDataException($"The compact 1-Wire discovery advertisement is unexpectedly large ({bytes.Length} bytes).");
                await udp.SendAsync(bytes, bytes.Length, new IPEndPoint(address, Program.OneWireDiscoveryPort)).ConfigureAwait(false);
                if (options.Value.EnableLanTransport && !IPAddress.IsLoopback(address))
                {
                    // Preserve same-machine discovery even when LAN transport is explicitly enabled.
                    await udp.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, Program.OneWireDiscoveryPort)).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) when (ex is SocketException or InvalidOperationException or InvalidDataException)
            {
                logger.LogWarning(ex, "Optional LocalGPT 1-Wire discovery broadcast failed; web/installer bootstrap is unaffected.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
