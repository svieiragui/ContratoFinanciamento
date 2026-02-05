using ContractsApi.Application.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ContractsApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly FixedUserSettings _userSettings;

    public AuthController(IJwtTokenGenerator jwtTokenGenerator, IOptions<FixedUserSettings> userSettings)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _userSettings = userSettings.Value;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username != _userSettings.Username || request.Password != _userSettings.Password)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = _jwtTokenGenerator.GenerateToken(request.Username);

        return Ok(new { token, expiresIn = 3600 });
    }
}

public record LoginRequest(string Username, string Password);