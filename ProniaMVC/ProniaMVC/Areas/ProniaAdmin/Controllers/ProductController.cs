using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using ProniaMVC.Areas.ProniaAdmin.ViewModels;
using ProniaMVC.DAL;
using ProniaMVC.Models;
using ProniaMVC.Utilities.Enums;
using ProniaMVC.Utilities.Extension;
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
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    CategoryName = p.Category.Name,
                    ImageURL = p.ProductImages.FirstOrDefault().ImageURL
                })
                .ToListAsync();
            return View(productVMs);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            CreateProductVM productVM = new CreateProductVM
            {
                Categories = await _context.Categories.Where(c => c.IsDeleted == false).ToListAsync(),
                Tags = await _context.Tags.Where(t => t.IsDeleted == false).ToListAsync()
            };
            return View(productVM);
        }

        [HttpPost]

        public async Task<IActionResult> Create(CreateProductVM productVM)
        {
            productVM.Categories = await _context.Categories.Where(c => c.IsDeleted == false).ToListAsync();
            productVM.Tags = await _context.Tags.Where(t => t.IsDeleted == false).ToListAsync();

            if (!ModelState.IsValid) return View(productVM);


            if (!productVM.MainPhoto.ValidateType("image/"))
            {
                ModelState.AddModelError("MainPhoto", "File type is not correct");
                return View(productVM);

            }
            if (!productVM.MainPhoto.ValidateSize(FileSize.MB, 1))
            {
                ModelState.AddModelError("MainPhoto", "File size must be less than 1MB");
                return View(productVM);

            }

            if (!productVM.HoverPhoto.ValidateType("image/"))
            {
                ModelState.AddModelError("HoverPhoto", "File type is not correct");
                return View(productVM);

            }
            if (!productVM.HoverPhoto.ValidateSize(FileSize.MB, 1))
            {
                ModelState.AddModelError("HoverPhoto", "File size must be less than 1MB");
                return View(productVM);

            }


            bool result = await _context.Categories.AnyAsync(c => c.Id == productVM.CategoryId && c.IsDeleted == false);
            if (!result)
            {
                ModelState.AddModelError("CategoryId", "Category does not exist");
                return View(productVM);

            }

            if (productVM.TagIds is not null)
            {
                bool tagResult = productVM.TagIds.Any(tId => !productVM.Tags.Exists(t => t.Id == tId));
                if (tagResult)
                {
                    ModelState.AddModelError("TagIds", "Tag does not exist");
                    return View(productVM);
                }
            }

            ProductImage mainImage = new ProductImage
            {
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                IsPrimary = true,
                ImageURL = await productVM.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images")

            };

            ProductImage hoverImage = new ProductImage
            {
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                IsPrimary = false,
                ImageURL = await productVM.HoverPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images")

            };

            Product product = new Product
            {
                CategoryId = productVM.CategoryId.Value,
                SKU = productVM.SKU,
                Description = productVM.Description,
                Name = productVM.Name,
                Price = productVM.Price.Value,
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                ProductImages = new List<ProductImage> { mainImage, hoverImage },

            };
            if (productVM.TagIds is not null)
            {
                product.ProductTags = productVM.TagIds.Select(tId => new ProductTag
                {
                    TagId = tId

                }).ToList();

            }

            if (productVM.Photos is not null)
            {
                string text = string.Empty;
                foreach (IFormFile file in productVM.Photos)
                {
                    if (!file.ValidateType("image/"))
                    {
                        text += $"{file.FileName} named file's type is not correct";
                        continue;
                    }
                    if (!file.ValidateSize(FileSize.MB, 1))
                    {
                        text += $"{file.FileName} named file's size must be less than 1 Mb";
                        continue;
                    }
                    ProductImage image = new ProductImage
                    {
                        CreatedAt = DateTime.Now,
                        IsDeleted = false,
                        IsPrimary = null,
                        ImageURL = await file.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images")
                    };
                    product.ProductImages.Add(image);
                }
                TempData["ErrorMessage"] = text;


            }

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null || id <= 0) return BadRequest();
            Product product = await _context.Products.Include(p => p.ProductImages).Include(p => p.ProductTags).FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted == false);
            if (product == null) return NotFound();

            UpdateProductVM productVM = new UpdateProductVM
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SKU = product.SKU,
                CategoryId = product.CategoryId,
                Categories = await _context.Categories.Where(c => c.IsDeleted == false).ToListAsync(),
                TagIds = product.ProductTags.Select(p => p.TagId).ToList(),
                Tags = await _context.Tags.Where(t => t.IsDeleted == false).ToListAsync(),
                Images = product.ProductImages.ToList()
            };
            return View(productVM);

        }

        [HttpPost]

        public async Task<IActionResult> Update(int? id, UpdateProductVM productVM)
        {
            if (id == null || id <= 0) return BadRequest();
            Product existed = await _context.Products.Include(p => p.ProductImages).Include(p => p.ProductTags).FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted == false);
            if (existed == null) return NotFound();

            productVM.Categories = await _context.Categories.Where(c => c.IsDeleted == false).ToListAsync();
            productVM.Tags = await _context.Tags.Where(t => t.IsDeleted == false).ToListAsync();
            if (!ModelState.IsValid) return View(productVM);


            if (productVM.MainPhoto != null)
            {
                if (!productVM.MainPhoto.ValidateType("image/"))
                {
                    ModelState.AddModelError("MainPhoto", "File type is not correct");
                    return View(productVM);
                }
                if (!productVM.MainPhoto.ValidateSize(FileSize.MB, 1))
                {
                    ModelState.AddModelError("MainPhoto", "File size must be less than 1MB");
                    return View(productVM);
                }

            }
            if (productVM.HoverPhoto != null)
            {
                if (!productVM.HoverPhoto.ValidateType("image/"))
                {
                    ModelState.AddModelError("HoverPhoto", "File type is not correct");
                    return View(productVM);

                }
                if (!productVM.HoverPhoto.ValidateSize(FileSize.MB, 1))
                {
                    ModelState.AddModelError("HoverPhoto", "File size must be less than 1MB");
                    return View(productVM);

                }

            }

            if (existed.CategoryId != productVM.CategoryId)
            {
                bool result = productVM.Categories.Any(c => c.Id == productVM.CategoryId && c.IsDeleted == false);
                if (!result)
                {
                    ModelState.AddModelError("CategoryId", "Category does not exist");
                    return View(productVM);
                }

            }

            if (productVM.MainPhoto != null)
            {
                ProductImage main = new ProductImage
                {
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsPrimary = true,
                    ImageURL = await productVM.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images")
                };
                ProductImage existedMain = existed.ProductImages.FirstOrDefault(pi => pi.IsPrimary == true);
                existedMain.ImageURL.DeleteFile(_env.WebRootPath, "assets", "images", "website-images");
                existed.ProductImages.Remove(existedMain);
                existed.ProductImages.Add(main);
            }

            if (productVM.HoverPhoto != null)
            {
                ProductImage hover = new ProductImage
                {
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsPrimary = true,
                    ImageURL = await productVM.HoverPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images")
                };
                ProductImage existedHover = existed.ProductImages.FirstOrDefault(pi => pi.IsPrimary == false);
                existedHover.ImageURL.DeleteFile(_env.WebRootPath, "assets", "images", "website-images");
                existed.ProductImages.Remove(existedHover);
                existed.ProductImages.Add(hover);
            }


            if (productVM.ImageIds == null)
            {
                productVM.ImageIds = new List<int>();
            }
            var deletedImages = existed.ProductImages.Where(pi => !productVM.ImageIds.Exists(imgId => pi.Id == imgId) && pi.IsPrimary == null).ToList();
            foreach (var img in deletedImages)
            {
                img.ImageURL.DeleteFile(_env.WebRootPath, "assets", "images", "website-images");
            }
            if (productVM.Photos != null)
            {
                string text = string.Empty;
                foreach (IFormFile? file in productVM.Photos)
                {
                    if (!file.ValidateType("image/"))
                    {
                        text += $"{file.FileName} named file's type is not correct";
                        continue;
                    }
                    if (!file.ValidateSize(FileSize.MB, 1))
                    {
                        text += $"{file.FileName} named file's size must be less than 1 Mb";
                        continue;
                    }
                    ProductImage image = new ProductImage
                    {
                        CreatedAt = DateTime.Now,
                        IsDeleted = false,
                        IsPrimary = null,
                        ImageURL = await file.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images")
                    };
                    existed.ProductImages.Add(image);
                }
                TempData["ErrorMessage"] = text;

            }
            _context.ProductImages.RemoveRange(deletedImages);

            if (productVM.TagIds == null)
            {
                productVM.TagIds = new List<int>();
            }
            var deletedTags = existed.ProductTags.Where(pt => !productVM.TagIds.Exists(tid => tid == pt.TagId)).ToList();
            _context.ProductTags.RemoveRange(deletedTags);

            var addedTags = productVM.TagIds.Where(tId => !existed.ProductTags.Any(pt => pt.TagId == tId)).Select(tId => new ProductTag { TagId = tId, });
            existed.ProductTags.AddRange(addedTags);


            existed.Name = productVM.Name;
            existed.Description = productVM.Description;
            existed.SKU = productVM.SKU;
            existed.Price = productVM.Price.Value;
            existed.CategoryId = productVM.CategoryId.Value;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");

        }



    }
}
