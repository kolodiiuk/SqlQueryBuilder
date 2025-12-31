using Microsoft.AspNetCore.Mvc;
using SQB.QueryExecution.Requests;
using SQB.Shared;

namespace SQB.QueryExecution.Controllers;

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
}
