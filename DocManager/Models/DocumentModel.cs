using System.ComponentModel.DataAnnotations;

namespace DocManager.Models;

/// <summary>
/// An entity that represents a digital document that can be managed by the system.
/// </summary>
public class DocumentModel
{
    /// <summary>
    /// Unique identifier of the document.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    [Key]
    public Guid DocumentId { get; set; }

    /// <summary>
    /// File name of the document
    /// </summary>
    /// <example>Sales Contract.pdf</example>
    [Required]
    [MaxLength(255)]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Mime type associated with the document.
    /// </summary>
    /// <example>application/pdf</example>
    [Required]
    [MaxLength(255)]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Size in bytes of the dlcument.
    /// </summary>
    /// <example>1337</example>
    [Required]
    public long FileSize { get; set; }

    /// <summary>
    /// List of tags to be associated with the document.
    /// </summary>
    /// <example>Contract,Finance Dept,Sales Dept</example>
    public ISet<string> Tags { get; set; } = default!;

    /// <summary>
    /// User Id of the person who uploaded this document.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    [Required]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Timestamp of when the Document was uploaded.
    /// </summary>
    /// <example>2023-01-02T22:12:00.000Z</example>
    [Required]
    public DateTimeOffset CreateTS { get; set; }

    /// <summary>
    /// User Id of the person who last modified this document.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Timestamp of when the document was last modified.
    /// </summary>
    /// <example>2023-01-02T22:12:00.000Z</example>
    public DateTimeOffset? UpdateTS { get; set; }
}
