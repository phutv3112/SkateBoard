namespace Skinet.Core.Entities.OrderAggregate
{
    public class Order : BaseEntity
    {
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public required string BuyerEmail { get; set; }
        public ShippingAddress ShippingAddress { get; set; } = default!;
        public DeliveryMethod DeliveryMethod { get; set; } = default!;
        public PaymentSummary PaymentSummary { get; set; } = default!;
        public List<OrderItem> OrderItems { get; set; } = [];
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public required string PaymentIntentId { get; set; }
        public decimal GetTotal() => Subtotal - Discount + DeliveryMethod.Price;
    }
}
