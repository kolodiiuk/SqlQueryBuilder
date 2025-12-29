using Microsoft.Extensions.Logging;

namespace SqlQueryBuilder.Shared;

public abstract class BaseController<TController> where TController : class
{
    protected BaseController(ILogger<TController> logger)
    {

    }
}
