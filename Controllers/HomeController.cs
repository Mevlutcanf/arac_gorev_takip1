using Microsoft.AspNetCore.Mvc;

namespace AracGorevFormu.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Yeni", "Form");
        }
    }
}
