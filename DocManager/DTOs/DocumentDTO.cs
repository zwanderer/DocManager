namespace DocManager.DTOs;

/// <summary>
/// A class that describes the document data that will be returned to consumers of Document API.
/// </summary>
public class DocumentViewDTO
{
    /// <summary>
    /// Unique identifier of the document.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// File name of the document
    /// </summary>
    /// <example>Sales Contract.pdf</example>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Mime type associated with the document.
    /// </summary>
    /// <example>application/pdf</example>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Size in bytes of the document.
    /// </summary>
    /// <example>1337</example>
    public long FileSize { get; set; }

    /// <summary>
    /// List of tags to be associated with the document.
    /// </summary>
    /// <example>["Contract","Finance Dept","Sales"]</example>
    public ISet<string> Tags { get; set; } = default!;

    /// <summary>
    /// Username of the person who uploaded this document.
    /// </summary>
    /// <example>user@email.com</example>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of when the Document was uploaded.
    /// </summary>
    /// <example>2023-01-02T22:12:00.000Z</example>
    public DateTimeOffset CreateTS { get; set; }

    /// <summary>
    /// Username of the person who last modified this document.
    /// </summary>
    /// <example>user@email.com</example>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Timestamp of when the document was last modified.
    /// </summary>
    /// <example>2023-01-02T22:12:00.000Z</example>
    public DateTimeOffset? UpdateTS { get; set; }
}
