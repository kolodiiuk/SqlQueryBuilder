using Microsoft.AspNetCore.Mvc;
using SqlQueryBuilder.Shared;

namespace SqlQueryBuilder.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController<AuthController>
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger) : base(logger)
    {

    }
}
