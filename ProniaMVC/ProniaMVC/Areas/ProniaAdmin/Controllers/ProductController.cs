using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProniaMVC.Areas.ProniaAdmin.ViewModels;
using ProniaMVC.DAL;
using ProniaMVC.Models;
using System.Collections.Generic;

namespace ProniaMVC.Areas.ProniaAdmin.Controllers
{
    [Area("ProniaAdmin")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        public async Task<IActionResult> Index()
        {
            List<GetAdminProductVM> productVMs = await _context.Products
                .Where(p => !p.IsDeleted)
                .Include(p => p.Category)
                .Include(p => p.ProductImages.Where(pi => pi.IsPrimary == true))
                .Select(p => new GetAdminProductVM
                {
                    Name = p.Name,
                    Price = p.Price,
                    CategoryName = p.Category.Name,
                    ImageURL = p.ProductImages.FirstOrDefault().ImageURL
                })
                .ToListAsync();
            
            return View(productVMs);
        }

        public async Task<IActionResult> Create()
        {

            CreateProductVM createProductVM = new CreateProductVM
            {
                Categories = await _context.Categories.Where(c => !c.IsDeleted).ToListAsync(),
            };
            return View(createProductVM);
        }


        [HttpPost]
        public async Task<IActionResult> Create(CreateProductVM productVM)
        {
            productVM.Categories = await _context.Categories.Where(c => !c.IsDeleted).ToListAsync();
            if (!ModelState.IsValid)
            {
                return View(productVM);
            }

            bool result = await _context.Categories.AnyAsync(c => c.Id == productVM.CategoryId && c.IsDeleted == false);

            if (!result)
            {
                ModelState.AddModelError(nameof(productVM.CategoryId), "Category does not exist");
                return View(productVM);
            }

            Product product = new Product
            {
                CategoryId = productVM.CategoryId.Value,
                Name = productVM.Name,
                Price = productVM.Price.Value,
                SKU = productVM.SKU,
                Description = productVM.Description,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
    }
}
