using Skinet.Core.Entities.OrderAggregate;

namespace Skinet.Core.Specifications
{
    public class OrderSpecification : BaseSpecification<Order>
    {
        public OrderSpecification(string email) : base(o => o.BuyerEmail == email)
        {
            AddInclude(o => o.DeliveryMethod);
            AddInclude(o => o.OrderItems);
            AddOrderByDescending(o => o.OrderDate);
        }

        public OrderSpecification(string email, Guid id) : base(o => o.BuyerEmail == email && o.Id == id)
        {
            AddInclude("OrderItems");
            AddInclude("DeliveryMethod");
        }
        public OrderSpecification(string paymentIntentId, bool isPaymentIntent) 
            : base(o => o.PaymentIntentId == paymentIntentId)
        {
            AddInclude("OrderItems");
            AddInclude("DeliveryMethod");
        }
    }
}
