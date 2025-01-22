namespace Skinet.Core.Entities.OrderAggregate
{
    public class ProductItemOrdered
    {
        public Guid ProductId { get; set; }
        public required string ProductName {  get; set; }
        public required string PictureUrl { get; set; }
    }
}
