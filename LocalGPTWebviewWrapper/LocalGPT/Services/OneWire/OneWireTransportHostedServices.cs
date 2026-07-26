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
    IOneWireMessageDispatcher dispatcher,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
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
            listener = new TcpListener(IPAddress.Any, Program.OneWirePort);
            listener.Start();
            logger.LogInformation("LocalGPT 1-Wire service listening on TCP {Port}.", Program.OneWirePort);
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
                    if (!string.IsNullOrWhiteSpace(envelope.SourcePeerId) && !string.Equals(envelope.SourcePeerId, "localgpt", StringComparison.OrdinalIgnoreCase))
                    {
                        peerId = envelope.SourcePeerId;
                        connections.Register(peerId, Sender);
                    }
                    var response = await dispatcher.DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
                    if (response is not null)
                        await Sender(response, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or SocketException)
        {
            logger.LogWarning(ex, "A 1-Wire peer connection ended after a protocol or transport error.");
        }
        finally
        {
            writeGate.Dispose();
            if (!string.IsNullOrWhiteSpace(peerId))
            {
                connections.Unregister(peerId);
                peers.SetConnected(peerId, false);
            }
        }
    }
}

public sealed class OneWireDiscoveryHostedService(
    IOptions<OneWireOptions> options,
    IOneWireCapabilityCatalog capabilities,
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
                var address = IPAddress.TryParse(options.Value.BroadcastAddress, out var parsed) ? parsed : IPAddress.Broadcast;
                var advertisement = new OneWirePeerAdvertisement
                {
                    PeerId = "localgpt",
                    DisplayName = "LocalGPT",
                    Application = "LocalGPT",
                    ApplicationVersion = "2.0.1-organic-wire",
                    HostName = Environment.MachineName,
                    Address = "0.0.0.0",
                    ServicePort = Program.OneWirePort,
                    DiscoveryPort = Program.OneWireDiscoveryPort,
                    WebBaseUrl = Program.BaseUrl,
                    IsConnected = true,
                    Capabilities = (await capabilities.GetLocalCapabilitiesAsync(stoppingToken).ConfigureAwait(false)).ToList()
                };
                var bytes = JsonSerializer.SerializeToUtf8Bytes(advertisement, OneWireEnvelopeCodec.CreateOptions());
                await udp.SendAsync(bytes, bytes.Length, new IPEndPoint(address, Program.OneWireDiscoveryPort)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) when (ex is SocketException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Optional LocalGPT 1-Wire discovery broadcast failed; web/installer bootstrap is unaffected.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
