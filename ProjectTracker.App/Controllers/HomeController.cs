using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Project Tracker";

            if (User.Identity?.IsAuthenticated == true)
            {
                var firstName = User.FindFirstValue("FirstName") ?? "User";
                ViewBag.WelcomeMessage = $"Welcome back, {firstName}!";
            }

            return View();
        }

        public IActionResult AccessDenied()
        {
            return View("Error401");
        }

        [Route("Home/Error")]
        public IActionResult Error(int? statusCode)
        {
            return statusCode switch
            {
                401 => View("Error401"),
                404 => View("Error404"),
                500 => View("Error500"),
                _ => View("Error404")
            };
        }

        public IActionResult ServerError()
        {
            return View("Error500");
        }
    }
}