using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Food_App.Data;
using Food_App.Models;

public static class DataSeeder
{
    public static async Task SeedDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        // Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Pizza", Description = "Delicious pizzas from around the world" },
                new Category { Name = "Burgers", Description = "Juicy burgers and sandwiches" },
                new Category { Name = "Asian Food", Description = "Authentic Asian cuisine" },
                new Category { Name = "Desserts", Description = "Sweet treats and desserts" },
                new Category { Name = "Beverages", Description = "Refreshing drinks and beverages" },
                new Category { Name = "Salads", Description = "Fresh and healthy salads" }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // Seed Sample Products
        if (!await context.Products.AnyAsync())
        {
            var categories = await context.Categories.ToListAsync();
            var sellers = await userManager.GetUsersInRoleAsync("Seller");

            if (sellers.Any())
            {
                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Margherita Pizza",
                        Description = "Classic pizza with tomato sauce, mozzarella, and fresh basil",
                        Price = 12.99m,
                        Stock = 50,
                        CategoryId = categories.First(c => c.Name == "Pizza").CategoryId,
                        SellerId = sellers.First().Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Cheeseburger",
                        Description = "Juicy beef patty with cheese, lettuce, tomato, and special sauce",
                        Price = 8.99m,
                        Stock = 30,
                        CategoryId = categories.First(c => c.Name == "Burgers").CategoryId,
                        SellerId = sellers.First().Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Chicken Fried Rice",
                        Description = "Authentic Chinese fried rice with tender chicken and vegetables",
                        Price = 10.99m,
                        Stock = 25,
                        CategoryId = categories.First(c => c.Name == "Asian Food").CategoryId,
                        SellerId = sellers.First().Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Chocolate Cake",
                        Description = "Rich and moist chocolate cake with chocolate frosting",
                        Price = 6.99m,
                        Stock = 15,
                        CategoryId = categories.First(c => c.Name == "Desserts").CategoryId,
                        SellerId = sellers.First().Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Fresh Orange Juice",
                        Description = "Freshly squeezed orange juice, no preservatives",
                        Price = 3.99m,
                        Stock = 40,
                        CategoryId = categories.First(c => c.Name == "Beverages").CategoryId,
                        SellerId = sellers.First().Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Caesar Salad",
                        Description = "Crisp romaine lettuce with Caesar dressing, croutons, and parmesan",
                        Price = 7.99m,
                        Stock = 20,
                        CategoryId = categories.First(c => c.Name == "Salads").CategoryId,
                        SellerId = sellers.First().Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
