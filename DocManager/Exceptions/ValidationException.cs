namespace DocManager.Exceptions;

/// <summary>
/// Custom exception for validation errors.
/// </summary>
/// <param name="message">Error Message</param>
public class ValidationException(string message) : Exception(message) { }
