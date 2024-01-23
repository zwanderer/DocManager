using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using DocManager.DTOs;
using DocManager.Interfaces;
using DocManager.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocManager.Controllers;

/// <summary>
/// Controller responsible for CRUD operations for model <see cref="DocumentModel"/>.
/// </summary>
/// <param name="documentService">Injected Document Service instance.</param>
/// <remarks>Requires the USER role.</remarks>
/// <response code="400">If a validation error occurs, it returns an object containing the error details.</response>
/// <response code="401">If user is not properly authenticated.</response>
/// <response code="403">If user does not have permission to execute the operation.</response>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
[Authorize("User")]

public class DocumentController(IDocumentService documentService) : ControllerBase
{
    private readonly IDocumentService _documentService = documentService;

    /// <summary>
    /// Uploads a document.
    /// </summary>
    /// <param name="file">File to be uploaded.</param>
    /// <param name="tags" example="Contract,Sales,Pending">Comma separated list of tags to be associated with the file.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>An instance of <see cref="DocumentViewDTO"/>.</returns>
    /// <response code="200">If the operation is successful, it returns an object with the new document details.</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentViewDTO), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> UploadDocument(IFormFile file, [FromHeader(Name = "x-tags")] string? tags, CancellationToken ct)
    {
        string loggedUserId = HttpContext.User.FindFirstValue("id") ?? string.Empty;
        if (!Guid.TryParse(loggedUserId, out var creator))
            throw new Exception("Invalid logged user id.");

        var stream = file.OpenReadStream();
        var doc = await _documentService.UploadDocument(file.FileName, file.ContentType, file.Length, tags, creator, stream, ct);

        return Ok(doc);
    }

    /// <summary>
    /// Returns document information.
    /// </summary>
    /// <param name="id" example="1f15eaaa-fc2f-4b3c-9dde-0ffe6ca9b709">Id of the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentViewDTO"/>.</returns>
    /// <response code="200">If the operation is successful, it returns an object with the document details.</response>
    /// <response code="204">If the document is NOT found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentViewDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async ValueTask<IActionResult> GetDocument([Required][FromRoute] Guid id, CancellationToken ct)
    {
        var doc = await _documentService.GetDocument(id, ct);
        return doc is null ? NoContent() : Ok(doc);
    }

    /// <summary>
    /// Searches for documents based on name or tag.
    /// </summary>
    /// <param name="name" example="Contract.pdf">Name of the document to look for.</param>
    /// <param name="tag" example="Sales">A tag to look for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="DocumentViewDTO"/> instances.</returns>
    /// <response code="200">If the operation is successful, it returns a list with details of matched documents.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentViewDTO>), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> SearchDocuments([FromQuery] string? name, [FromQuery] string? tag, CancellationToken ct)
    {
        var docs = await _documentService.SearchDocuments(name, tag, ct);
        return Ok(docs);
    }

    /// <summary>
    /// Downloads a document.
    /// </summary>
    /// <param name="id" example="1f15eaaa-fc2f-4b3c-9dde-0ffe6ca9b709">Id of the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The binary content of the document.</returns>
    /// <response code="200">If the operation is successful, it returns the binary content of document.</response>
    /// <response code="204">If the document is NOT found.</response>
    [HttpGet("download/{id}")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async ValueTask<IActionResult> DownloadDocument([Required][FromRoute] Guid id, CancellationToken ct)
    {
        var (filename, stream) = await _documentService.DownloadDocument(id, ct);
        return File(stream, "application/octet-stream", filename);
    }

    /// <summary>
    /// Updates the list of tags of a document.
    /// </summary>
    /// <param name="id" example="1f15eaaa-fc2f-4b3c-9dde-0ffe6ca9b709">Id of the document.</param>
    /// <param name="tags" example="[&quot;Contract&quot;,&quot;Pending&quot;]">A list of tags to be associated with the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentViewDTO"/>.</returns>
    /// <response code="200">If the operation is successful, it returns an object with the updated document details.</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(DocumentViewDTO), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> UpdateTags([Required][FromRoute] Guid id, [FromBody] ISet<string> tags, CancellationToken ct)
    {
        string loggedUserId = HttpContext.User.FindFirstValue("id") ?? string.Empty;
        if (!Guid.TryParse(loggedUserId, out var updater))
            throw new Exception("Invalid logged user id.");

        var doc = await _documentService.UpdateTags(id, tags, updater, ct);
        return Ok(doc);
    }
}
