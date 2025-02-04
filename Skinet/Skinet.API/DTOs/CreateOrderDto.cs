using Skinet.Core.Entities.OrderAggregate;
using Skinet.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace Skinet.API.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public string CartId { get; set; } = default!;
        public ShippingAddress ShippingAddress { get; set; } = default!;
        public Guid DeliveryMethodId { get; set; } = default!;
        public PaymentSummary PaymentSummary { get; set; } = default!;
        public decimal Discount { get; set; }
    }
}
