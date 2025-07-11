using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Serilog;
using GMAOAPI.Services.SeriLog;

public class SerilogService : ISerilogService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SerilogService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void LogAudit(string actionEffectuee, string information = "")
    {
        var userName = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.GivenName)?.Value
                    ?? "UnknownUser";

        Log.ForContext("UserName", userName)
           .ForContext("ActionEffectuee", actionEffectuee)
           .Information($"L’utilisateur {userName} a effectué l’action suivante : {actionEffectuee} {information}");
    }
}
