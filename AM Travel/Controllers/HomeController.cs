using Microsoft.AspNetCore.Mvc;

namespace AM_Travel.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
