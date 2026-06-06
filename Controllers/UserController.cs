using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Food_App.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public UserController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ✅ Redirect to dashboard based on role
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return RedirectToAction("AdminDashboard");
            else if (roles.Contains("Seller"))
                return RedirectToAction("SellerDashboard");
            else
                return RedirectToAction("CustomerDashboard");
        }

        // ✅ Admin Dashboard
        public IActionResult AdminDashboard()
        {
            return View();
        }

        // ✅ Seller Dashboard
        public IActionResult SellerDashboard()
        {
            return View();
        }

        // ✅ Customer Dashboard
        public IActionResult CustomerDashboard()
        {
            return View();
        }
    }
}
