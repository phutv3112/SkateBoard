namespace Skinet.Core.Entities
{
    public class CartItem
    {
        public required string ProductId { get; set; }
        public string ProductName { get; set; } 
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public required string PictureUrl { get; set; }
        public string Brand { get; set; }
        public string Type { get; set; }
    }
}
