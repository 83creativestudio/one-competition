using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Storage;
using OneCompetitions.Contracts.Assets;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class AssetsController(IAssetService assets) : ControllerBase
{
    [HttpPost("uploads")]
    public async Task<ActionResult<AssetUploadResponse>> CreateUpload(CreateAssetUploadRequest request, CancellationToken cancellationToken) =>
        Ok(await assets.CreateUploadAsync(User.GetUserId(), request, cancellationToken));

    [HttpPost("{assetId:guid}/complete")]
    public async Task<ActionResult<AssetResponse>> Complete(Guid assetId, CancellationToken cancellationToken) =>
        Ok(await assets.CompleteUploadAsync(User.GetUserId(), assetId, cancellationToken));

    [HttpGet("{assetId:guid}/download")]
    public async Task<IActionResult> Download(Guid assetId, CancellationToken cancellationToken) =>
        Redirect(await assets.CreateDownloadUrlAsync(User.GetUserId(), assetId, cancellationToken));

    [HttpPut("{assetId:guid}/local-content")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> LocalUpload(Guid assetId, CancellationToken cancellationToken)
    {
        await assets.StoreLocalUploadAsync(User.GetUserId(), assetId, Request.Body, cancellationToken);
        return NoContent();
    }

    [HttpGet("{assetId:guid}/local-content")]
    public async Task<IActionResult> LocalDownload(Guid assetId, CancellationToken cancellationToken)
    {
        var result = await assets.OpenLocalDownloadAsync(User.GetUserId(), assetId, cancellationToken);
        return File(result.Content, result.ContentType, result.Filename);
    }
}
