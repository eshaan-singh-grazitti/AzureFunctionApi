using Microsoft.AspNetCore.Mvc;

namespace AzureFunctionApi.Controllers
{
    public class AController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
