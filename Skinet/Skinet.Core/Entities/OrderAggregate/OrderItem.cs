namespace Skinet.Core.Entities.OrderAggregate
{
    public class OrderItem : BaseEntity
    {
        public ProductItemOrdered ItemOrdered { get; set; } = default!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
