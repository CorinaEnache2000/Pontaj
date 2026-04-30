using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pontaj.Controllers;

[Route("Account")]
public class AccountViewsController : Controller
{
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View("~/Views/Account/Login.cshtml");
    }
}
