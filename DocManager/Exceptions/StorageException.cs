namespace DocManager.Exceptions;

/// <summary>
/// Custom exception for storage related errors.
/// </summary>
/// <param name="message">Error Message</param>
public class StorageException(string message) : Exception(message) { }
