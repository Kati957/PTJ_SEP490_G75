using Microsoft.AspNetCore.Mvc;

namespace PTJ_API.Controllers
{
    public class Auth : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
