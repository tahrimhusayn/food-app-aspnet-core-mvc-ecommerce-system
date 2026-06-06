using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Food_App.Data;
using Food_App.Models;
using Food_App.Attributes;

namespace Food_App.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Seller Dashboard
        [AuthorizeRole("Seller")]
        public async Task<IActionResult> Seller()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var viewModel = new SellerDashboardViewModel
            {
                User = user,
                TotalProducts = await _context.Products.CountAsync(p => p.SellerId == user.Id),
                ActiveProducts = await _context.Products.CountAsync(p => p.SellerId == user.Id && p.Status == "Active"),
                TotalOrders = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .CountAsync(oi => oi.Product.SellerId == user.Id),
                TotalRevenue = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Product.SellerId == user.Id)
                    .SumAsync(oi => oi.Price * oi.Quantity),
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.SellerId == user.Id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentOrders = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Product.SellerId == user.Id)
                    .OrderByDescending(oi => oi.Order.OrderDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // Admin Dashboard
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Admin()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalSellers = await _userManager.GetUsersInRoleAsync("Seller"),
                TotalCustomers = await _userManager.GetUsersInRoleAsync("Customer"),
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount),
                RecentOrders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync(),
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // Seller's products management
        [AuthorizeRole("Seller")]
        public async Task<IActionResult> MyProducts()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SellerId == user.Id)
                .ToListAsync();

            return View(products);
        }

        // Seller's orders management
        [AuthorizeRole("Seller")]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Get all order items for seller's products
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == user.Id)
                .ToListAsync();

            // Group by order ID
            var groupedOrders = orderItems
                .GroupBy(oi => oi.OrderId)
                .Select(g => new
                {
                    Order = g.First().Order,
                    Items = g.ToList(),
                    Total = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(x => x.Order.OrderDate)
                .ToList();

            return View(groupedOrders);
        }

        // Admin's orders management
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> AllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Admin's users management
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> AllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // Update order status (Admin)
        [AuthorizeRole("Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request");
            }

            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null) return NotFound();

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Edit user (Admin)
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = new List<string> { "Admin", "Seller", "Customer" };

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                City = user.City,
                State = user.State,
                ZipCode = user.ZipCode,
                CurrentRoles = userRoles,
                AvailableRoles = allRoles,
                SelectedRole = userRoles.FirstOrDefault() ?? "Customer"
            };

            return View(viewModel);
        }

        // Update user (Admin)
        [AuthorizeRole("Admin")]
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(viewModel.Id);
                    if (user == null) return NotFound();

                    // Update user properties
                    user.Email = viewModel.Email;
                    user.UserName = viewModel.Email;
                    user.FirstName = viewModel.FirstName;
                    user.LastName = viewModel.LastName;
                    user.Address = viewModel.Address;
                    user.City = viewModel.City;
                    user.State = viewModel.State;
                    user.ZipCode = viewModel.ZipCode;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        // Update roles
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, viewModel.SelectedRole);

                        return RedirectToAction(nameof(AllUsers));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }

            viewModel.AvailableRoles = new List<string> { "Admin", "Seller", "Customer" };
            return View(viewModel);
        }

        // Delete user (Admin)
        [AuthorizeRole("Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete user" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class SellerDashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Product> RecentProducts { get; set; } = new();
        public List<OrderItem> RecentOrders { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public IList<ApplicationUser> TotalSellers { get; set; } = new List<ApplicationUser>();
        public IList<ApplicationUser> TotalCustomers { get; set; } = new List<ApplicationUser>();
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> RecentProducts { get; set; } = new();
    }
}
