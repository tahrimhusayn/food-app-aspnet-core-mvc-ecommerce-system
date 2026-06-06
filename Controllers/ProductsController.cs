using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Food_App.Data;
using Food_App.Models;
using Food_App.Attributes;

namespace Food_App.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Products.Include(p => p.Category);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        [AuthorizeRole("Seller", "Admin")]
        public async Task<IActionResult> Create()
        {
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");
            var viewModel = new CreateProductViewModel
            {
                CategoryList = new SelectList(_context.Categories, "CategoryId", "Name"),
                SellerList = new SelectList(sellers.Select(s => new { Id = s.NumericId, Name = $"{s.FirstName} {s.LastName} ({s.Email})" }), "Id", "Name")
            };
            return View(viewModel);
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Seller", "Admin")]
        public async Task<IActionResult> Create(CreateProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Unauthorized();
                    }

                    // Determine seller ID
                    string sellerId;
                    if (User.IsInRole("Admin") && viewModel.SellerNumericId.HasValue)
                    {
                        // Admin is creating product for a specific seller
                        var seller = await _userManager.Users.FirstOrDefaultAsync(u => u.NumericId == viewModel.SellerNumericId.Value);
                        if (seller == null)
                        {
                            ModelState.AddModelError("SellerNumericId", "Seller not found.");
                            var sellerList = await _userManager.GetUsersInRoleAsync("Seller");
                            viewModel.CategoryList = new SelectList(_context.Categories, "CategoryId", "Name", viewModel.CategoryId);
                            viewModel.SellerList = new SelectList(sellerList.Select(s => new { Id = s.NumericId, Name = $"{s.FirstName} {s.LastName} ({s.Email})" }), "Id", "Name");
                            return View(viewModel);
                        }
                        sellerId = seller.Id;
                    }
                    else
                    {
                        // Seller is creating their own product
                        sellerId = currentUser.Id;
                    }

                    var product = new Product
                    {
                        CategoryId = viewModel.CategoryId,
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        Price = viewModel.Price,
                        ImageUrl = viewModel.ImageUrl,
                        Stock = viewModel.Stock,
                        Status = viewModel.Status,
                        SellerId = sellerId,
                        CreatedAt = DateTime.Now
                    };

                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the product: " + ex.Message);
                }
            }
            
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");
            viewModel.CategoryList = new SelectList(_context.Categories, "CategoryId", "Name", viewModel.CategoryId);
            viewModel.SellerList = new SelectList(sellers.Select(s => new { Id = s.NumericId, Name = $"{s.FirstName} {s.LastName} ({s.Email})" }), "Id", "Name");
            return View(viewModel);
        }

        // GET: Products/Edit/5
        [AuthorizeRole("Seller", "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if user is the seller or admin
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains("Admin") && product.SellerId != user.Id)
            {
                return Forbid();
            }

            // Get seller's numeric ID for the dropdown
            var seller = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == product.SellerId);
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");

            var viewModel = new EditProductViewModel
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                Stock = product.Stock,
                Status = product.Status ?? "Active",
                SellerNumericId = seller?.NumericId,
                CategoryList = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId),
                SellerList = new SelectList(sellers.Select(s => new { Id = s.NumericId, Name = $"{s.FirstName} {s.LastName} ({s.Email})" }), "Id", "Name")
            };

            return View(viewModel);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Seller", "Admin")]
        public async Task<IActionResult> Edit(int id, EditProductViewModel viewModel)
        {
            if (id != viewModel.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Unauthorized();
                    }

                    // Get the existing product
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                    {
                        return NotFound();
                    }

                    // Determine seller ID
                    string sellerId;
                    if (User.IsInRole("Admin") && viewModel.SellerNumericId.HasValue)
                    {
                        // Admin is editing product for a specific seller
                        var seller = await _userManager.Users.FirstOrDefaultAsync(u => u.NumericId == viewModel.SellerNumericId.Value);
                        if (seller == null)
                        {
                            ModelState.AddModelError("SellerNumericId", "Seller not found.");
                            var sellers = await _userManager.GetUsersInRoleAsync("Seller");
                            viewModel.CategoryList = new SelectList(_context.Categories, "CategoryId", "Name", viewModel.CategoryId);
                            viewModel.SellerList = new SelectList(sellers.Select(s => new { Id = s.NumericId, Name = $"{s.FirstName} {s.LastName} ({s.Email})" }), "Id", "Name");
                            return View(viewModel);
                        }
                        sellerId = seller.Id;
                    }
                    else
                    {
                        // Keep existing seller ID or use current user
                        sellerId = product.SellerId;
                    }

                    // Update product properties
                    product.CategoryId = viewModel.CategoryId;
                    product.Name = viewModel.Name;
                    product.Description = viewModel.Description;
                    product.Price = viewModel.Price;
                    product.ImageUrl = viewModel.ImageUrl;
                    product.Stock = viewModel.Stock;
                    product.Status = viewModel.Status;
                    product.SellerId = sellerId;

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            var sellerList = await _userManager.GetUsersInRoleAsync("Seller");
            viewModel.CategoryList = new SelectList(_context.Categories, "CategoryId", "Name", viewModel.CategoryId);
            viewModel.SellerList = new SelectList(sellerList.Select(s => new { Id = s.NumericId, Name = $"{s.FirstName} {s.LastName} ({s.Email})" }), "Id", "Name");
            return View(viewModel);
        }

        // GET: Products/Delete/5
        [AuthorizeRole("Seller", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if user is the seller or admin
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains("Admin") && product.SellerId != user.Id)
            {
                return Forbid();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Seller", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
