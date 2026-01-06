using Microsoft.AspNetCore.Mvc;

namespace FingerprintManagementSystem.Web.Controllers;

public class TerminalsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
