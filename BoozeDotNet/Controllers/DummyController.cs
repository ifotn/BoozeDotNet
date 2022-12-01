using Microsoft.AspNetCore.Mvc;

namespace BoozeDotNet.Controllers
{
    public class DummyController : Controller
    {
        public IActionResult Index()
        {
            return View("Index");
        }
    }
}
