namespace DocManager.Exceptions;

/// <summary>
/// Custom exception for errors related to anti virus scan.
/// </summary>
/// <param name="message">Error message</param>
public class AntiVirusException(string message) : Exception(message) { }
