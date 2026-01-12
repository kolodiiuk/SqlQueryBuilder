using Microsoft.AspNetCore.Mvc;
using SQB.DdlDml.Api.Requests;
using SQB.Shared;

namespace SQB.DdlDml.Api.Controllers;

public class DdlController : BaseController<DdlController>
{
    public DdlController(ILogger<DdlController> logger) : base(logger)
    {
    }

    [HttpPost]
    public async Task<IActionResult> AddUserTable(CreateTableRequest req)
    {
        return StatusCode(418);
    }

    // [HttpPost("image/{productId:int}")]
    // public async Task<IActionResult> UploadImage(int productId, IFormFile file)
    // {
    //     Log(LogLevel.Information, new EventId(2000, "AddingProductImage"), "Adding image for product {ProductId}",
    //         productId);
    //     if (file == null || file.Length == 0)
    //     {
    //         return StatusCode(StatusCodes.Status400BadRequest, "No file uploaded.");
    //     }
    //
    //     var uploadParams = new ImageUploadParams()
    //     {
    //         File = new FileDescription(file.FileName, file.OpenReadStream()),
    //         PublicId = Guid.NewGuid().ToString(),
    //         Overwrite = true,
    //         Folder = "uploads/"
    //     };
    //
    //     var uploadResult = await _cloudinary.UploadAsync(uploadParams);
    //
    //     if (uploadResult.StatusCode != HttpStatusCode.OK)
    //     {
    //         return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails() { Title = "Upload failed." });
    //     }
    //
    //     var result = await _productsService.AddImageAsync(
    //         productId, uploadResult.Url.AbsoluteUri);
    //     result.OnFailure(() => Log(LogLevel.Error, AdminProductControllerEventIds.FailedToAddProductImage,
    //             "Failed to add image for product {ProductId}. Error: {error}",
    //             productId, result.Error))
    //         .OnSuccess(() => Log(LogLevel.Information, AdminProductControllerEventIds.ProductImageAdded,
    //             "Added image for product {ProductId}", productId));
    //
    //     return StatusCode(StatusCodes.Status200OK);
    // }
}
