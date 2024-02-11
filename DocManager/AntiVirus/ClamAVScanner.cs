using DocManager.Exceptions;
using DocManager.Interfaces;
using DocManager.Utils;

using nClam;

namespace DocManager.AntiVirus;

/// <summary>
/// Implementation of <see cref="IAVScanner"/> using ClamAV (nClam).
/// </summary>
/// <param name="client">Injected instance of <see cref="IClamClient"/>.</param>
/// <param name="logger">Logger instance</param>
/// <param name="requestContext">Injected Request Context instance.</param>
public class ClamAVScanner(IClamClient client, ILogger<ClamAVScanner> logger, RequestContext requestContext) : IAVScanner
{
    private readonly IClamClient _client = client;
    private readonly ILogger<ClamAVScanner> _logger = logger;
    private readonly RequestContext _requestContext = requestContext;

    /// <inheritdoc/>
    public async ValueTask<(bool, string?)> ScanFile(Stream stream, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var requestId = _requestContext.GetRequestId();

        _logger.LogInformation("[{requestId}] Starting ClamAV server scan, stream size {size} bytes.", requestId, stream.Length);
        try
        {
            var result = await _client.SendAndScanFileAsync(stream, ct);
            switch (result.Result)
            {
                case ClamScanResults.Clean:
                    _logger.LogInformation("[{requestId}] Stream is clean!", requestId);
                    return (true, null);

                case ClamScanResults.VirusDetected:
                    string virus = result.InfectedFiles!.First().VirusName;
                    _logger.LogInformation("[{requestId}] Stream is infected with `{virus}`!\n{message}", requestId, virus, result.RawResult);
                    return (false, virus);

                case ClamScanResults.Error:
                    _logger.LogInformation("[{requestId}] An error occurred during the scan!\n{message}", requestId, result.RawResult);
                    return (false, "error");

                default:
                    throw new AntiVirusException($"Invalid ClamAV result: {result.Result}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{requestId}] Error while scanning stream.", requestId);
            throw;
        }
    }
}
