using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SqlQueryBuilder.Shared;

public abstract class BaseController<TController> : ControllerBase where TController : class
{
    protected BaseController(ILogger<TController> logger)
    {

    }
}
