using Microsoft.AspNetCore.Mvc;
using ProniaMVC.DAL;
using ProniaMVC.Models;
using ProniaMVC.ViewModels;

namespace ProniaMVC.Controllers
{
    public class HomeController : Controller
    {
        public readonly AppDbContext _context;
        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            
            HomeVM homeVM = new HomeVM
            {
                Slides = _context.Slides.OrderBy(s => s.Order).ToList()
            };
            return View(homeVM);
        }
    }
}