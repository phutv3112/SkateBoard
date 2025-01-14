using Skinet.Core.Entities;
using System.Text.Json;

namespace Skinet.Infastructure.Data
{
    public class StoreSeedContext
    {
        public static async Task SeedAsync(StoreContext context)
        {
            if(!context.Products.Any())
            {
                var productsDataJson = await File.ReadAllTextAsync("../Skinet.Infastructure/Data/SeedData/products.json");
                var products = JsonSerializer.Deserialize<List<Product>>(productsDataJson);
                if(products == null) return;
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
