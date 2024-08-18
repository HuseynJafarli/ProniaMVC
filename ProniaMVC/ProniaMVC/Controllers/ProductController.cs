using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProniaMVC.DAL;
using ProniaMVC.Models;
using ProniaMVC.ViewModels;

namespace ProniaMVC.Controllers
{
    public class ProductController : Controller
    {
        public readonly AppDbContext _context;
        public ProductController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Detail(int? id)
        {
            if(id == null || id <= 0)
            {
                return BadRequest();
            }

            Product product = _context.Products
                .Include(p => p.Category)
                .Include(p=>p.ProductImages.OrderByDescending(pi => pi.IsPrimary))
                .FirstOrDefault(p => p.Id == id);

            if (product == null) return NotFound();

            DetailVM vm = new DetailVM
            {
                Product = product,
                Products = _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Include(p => p.ProductImages.Where(pi => pi.IsPrimary != null))
                .ToList(),
                
            };

            return View(vm);
        }
    }
}
