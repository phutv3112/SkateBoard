using Microsoft.AspNetCore.Identity;
using Skinet.Core.Entities;
using System.Text.Json;

namespace Skinet.Infastructure.Data
{
    public class StoreSeedContext
    {
        public static async Task SeedAsync(StoreContext context, UserManager<AppUser> userManager)
        {
            if (!userManager.Users.Any(x => x.UserName == "admin@test.com"))
            {
                var user = new AppUser
                {
                    UserName = "admin@test.com",
                    Email = "admin@test.com",
                };

                await userManager.CreateAsync(user, "Pa$$w0rd");
                await userManager.AddToRoleAsync(user, "Admin");
            }

            if (!context.Products.Any())
            {
                var productsDataJson = await File.ReadAllTextAsync("../Skinet.Infastructure/Data/SeedData/products.json");
                var products = JsonSerializer.Deserialize<List<Product>>(productsDataJson);
                if(products == null) return;
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
            if (!context.DeliveryMethods.Any())
            {
                var deliveryDataJson = await File.ReadAllTextAsync("../Skinet.Infastructure/Data/SeedData/delivery.json");
                var deliveryMethods = JsonSerializer.Deserialize<List<DeliveryMethod>>(deliveryDataJson);
                if (deliveryMethods == null) return;
                context.DeliveryMethods.AddRange(deliveryMethods);
                await context.SaveChangesAsync();
            }
        }
    }
}
