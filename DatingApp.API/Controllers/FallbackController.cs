namespace DatingApp.API.Controllers
{
    using System.IO;
    using Microsoft.AspNetCore.Mvc;
    
    public class FallbackController : Controller
    {
        public IActionResult Index()
        {
            return this.PhysicalFile(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html"),
                "text/HTML");
        }
    }
}