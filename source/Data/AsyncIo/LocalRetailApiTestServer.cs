using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed class LocalRetailApiTestServer : IAsyncDisposable
{
    private readonly int _simulatedLatencyMilliseconds;
    private readonly System.Text.Json.JsonSerializerOptions _serializerOptions = new(System.Text.Json.JsonSerializerDefaults.Web);
    private readonly object _lifecycleLock = new();

    private IHost? _host;
    private Uri? _baseAddress;

    private int _totalRequests;
    private int _activeRequests;
    private int _maxObservedConcurrentRequests;
    private int _failedRequests;

    public LocalRetailApiTestServer(int simulatedLatencyMilliseconds)
    {
        if (simulatedLatencyMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(simulatedLatencyMilliseconds));
        }

        _simulatedLatencyMilliseconds = simulatedLatencyMilliseconds;
    }

    public Uri BaseAddress => _baseAddress ?? throw new InvalidOperationException("Server has not started.");

    public bool IsStarted => _host is not null;

    public int SimulatedLatencyMilliseconds => _simulatedLatencyMilliseconds;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (_host is not null)
            {
                throw new InvalidOperationException("Server already started.");
            }
        }

        int port = GetAvailablePort();
        string url = $"http://127.0.0.1:{port}";

        IHost host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                    options.ListenLocalhost(port, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                });
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/retail/orders/{storeId:int}/{orderId:int}", HandleRequestAsync);
                    });
                });
            })
            .Build();

        try
        {
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            host.Dispose();
            throw;
        }

        lock (_lifecycleLock)
        {
            _host = host;
            _baseAddress = new Uri(url, UriKind.Absolute);
        }
    }

    public void ResetMetrics()
    {
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _activeRequests, 0);
        Interlocked.Exchange(ref _maxObservedConcurrentRequests, 0);
        Interlocked.Exchange(ref _failedRequests, 0);
    }

    public AsyncIoServerSnapshot GetSnapshot() => new(
        TotalRequests: Volatile.Read(ref _totalRequests),
        ActiveRequests: Volatile.Read(ref _activeRequests),
        MaxObservedConcurrentRequests: Volatile.Read(ref _maxObservedConcurrentRequests),
        FailedRequests: Volatile.Read(ref _failedRequests));

    private async Task HandleRequestAsync(HttpContext context)
    {
        int currentActive = Interlocked.Increment(ref _activeRequests);
        Interlocked.Increment(ref _totalRequests);
        UpdateMaxConcurrency(currentActive);

        try
        {
            int storeId = int.Parse((string)context.Request.RouteValues["storeId"]!, System.Globalization.CultureInfo.InvariantCulture);
            int orderId = int.Parse((string)context.Request.RouteValues["orderId"]!, System.Globalization.CultureInfo.InvariantCulture);
            string requestId = context.Request.Query["requestId"].ToString();

            if (string.IsNullOrWhiteSpace(requestId))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Interlocked.Increment(ref _failedRequests);
                return;
            }

            if (_simulatedLatencyMilliseconds > 0)
            {
                await Task.Delay(_simulatedLatencyMilliseconds, context.RequestAborted).ConfigureAwait(false);
            }

            AsyncIoApiResponseDto response = BuildResponse(requestId, storeId, orderId);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, response, _serializerOptions, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _failedRequests);
            throw;
        }
        catch
        {
            Interlocked.Increment(ref _failedRequests);
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _activeRequests);
        }
    }

    private static AsyncIoApiResponseDto BuildResponse(string requestId, int storeId, int orderId)
    {
        int availableUnits = ((storeId * 31) + (orderId * 17)) % 200 + 1;
        bool isCompleted = ((orderId + storeId) % 3) != 0;
        string status = isCompleted ? "Completed" : "Pending";

        long checksum = ComputeChecksum(requestId, storeId, orderId, status, availableUnits);

        return new AsyncIoApiResponseDto(requestId, storeId, orderId, status, availableUnits, checksum);
    }

    private static long ComputeChecksum(string requestId, int storeId, int orderId, string status, int availableUnits)
    {
        string payload = string.Concat(requestId, "|", storeId.ToString(System.Globalization.CultureInfo.InvariantCulture), "|", orderId.ToString(System.Globalization.CultureInfo.InvariantCulture), "|", status, "|", availableUnits.ToString(System.Globalization.CultureInfo.InvariantCulture));
        byte[] hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToInt64(hash, 0);
    }

    private void UpdateMaxConcurrency(int currentActive)
    {
        int currentMax;
        do
        {
            currentMax = Volatile.Read(ref _maxObservedConcurrentRequests);
            if (currentActive <= currentMax)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref _maxObservedConcurrentRequests, currentActive, currentMax) != currentMax);
    }

    private static int GetAvailablePort()
    {
        using System.Net.Sockets.TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        IHost? host;

        lock (_lifecycleLock)
        {
            host = _host;
            _host = null;
            _baseAddress = null;
        }

        if (host is null)
        {
            return;
        }

        try
        {
            await host.StopAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        finally
        {
            host.Dispose();
        }
    }
}
