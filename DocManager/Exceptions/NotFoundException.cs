namespace DocManager.Exceptions;

/// <summary>
/// Custom exception for when an item is not found.
/// </summary>
/// <param name="message">Error message</param>
public class NotFoundException(string message) : Exception(message) { }
