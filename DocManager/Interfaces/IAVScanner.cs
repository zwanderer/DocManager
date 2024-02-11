namespace DocManager.Interfaces;

/// <summary>
/// Abstracts Virus Scanner service.
/// </summary>
public interface IAVScanner
{
    /// <summary>
    /// Scans the contents of <paramref name="stream"/> for viruses.
    /// </summary>
    /// <param name="stream">The stream with the file contents to be scanned for viruses.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A <see cref="bool"/> indicating if the file is safe and a <see cref="string"/> containing the virus name (null of no virus found).</returns>
    public ValueTask<(bool, string?)> ScanFile(Stream stream, CancellationToken ct);
}
