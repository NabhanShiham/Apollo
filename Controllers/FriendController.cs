using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apollo.Controllers;

[Authorize]
public class FriendController : Controller
{
    private readonly ILogger<FriendController> _logger;
    public FriendController(ILogger<FriendController> logger)
    {
        _logger = logger;
    }
    public IActionResult Index()
    {
        return View();
    }
}