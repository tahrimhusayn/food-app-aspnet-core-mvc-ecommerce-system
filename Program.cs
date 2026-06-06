using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Food_App.Data;
using Food_App.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Database Context ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Identity ---
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- MVC + Session Support ---
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache(); // ✅ Needed for session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- Middleware pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Use session middleware
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// --- Routes ---
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// --- Role seeding ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    await RoleSeeder.SeedRolesAsync(roleManager);

           // Optional: seed admin user
           var adminEmail = "admin@foodapp.com";
           var adminUser = await userManager.FindByEmailAsync(adminEmail);
           if (adminUser == null)
           {
               adminUser = new ApplicationUser
               {
                   UserName = adminEmail,
                   Email = adminEmail,
                   EmailConfirmed = true,
                   FirstName = "Admin",
                   LastName = "User",
                   NumericId = 1
               };
               await userManager.CreateAsync(adminUser, "Admin@123");
               await userManager.AddToRoleAsync(adminUser, "Admin");
           }
           else if (adminUser.NumericId == 0)
           {
               adminUser.NumericId = 1;
               await userManager.UpdateAsync(adminUser);
           }

           // Seed sample seller
           var sellerEmail = "seller@foodapp.com";
           var sellerUser = await userManager.FindByEmailAsync(sellerEmail);
           if (sellerUser == null)
           {
               sellerUser = new ApplicationUser
               {
                   UserName = sellerEmail,
                   Email = sellerEmail,
                   EmailConfirmed = true,
                   FirstName = "John",
                   LastName = "Seller",
                   NumericId = 2
               };
               await userManager.CreateAsync(sellerUser, "Seller@123");
               await userManager.AddToRoleAsync(sellerUser, "Seller");
           }
           else if (sellerUser.NumericId == 0)
           {
               sellerUser.NumericId = 2;
               await userManager.UpdateAsync(sellerUser);
           }

    // Seed sample data
    await DataSeeder.SeedDataAsync(context, userManager);
}

app.Run();
