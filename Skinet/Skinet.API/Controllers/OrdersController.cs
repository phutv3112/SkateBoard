using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Skinet.API.DTOs;
using Skinet.API.Extentions;
using Skinet.Core.Entities;
using Skinet.Core.Entities.OrderAggregate;
using Skinet.Core.Interfaces;
using Skinet.Core.Specifications;

namespace Skinet.API.Controllers
{
    [Authorize]
    public class OrdersController(ICartService cartService, IUnitOfWork unit) : BaseApiController
    {
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto orderDto)
        {
            var email = User.GetEmail();

            var cart = await cartService.GetCartAsync(orderDto.CartId);
            if (cart == null) return BadRequest("Cart not found");

            if (cart.PaymentIntentId == null) return BadRequest("No payment intent for this order");

            var items = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                var productItem = await unit.Repository<Product>().GetByIdAsync(Guid.Parse(item.ProductId));
                if (productItem == null) return BadRequest("Problem with the order");
                var itemOrdered = new ProductItemOrdered
                {
                    ProductId = productItem.Id,
                    ProductName = productItem.Name,
                    PictureUrl = productItem.PictureUrl
                };

                var orderItem = new OrderItem
                {
                    ItemOrdered = itemOrdered,
                    Price = productItem.Price,
                    Quantity = item.Quantity
                };
                items.Add(orderItem);
            }

            var deliveryMethod = await unit.Repository<DeliveryMethod>().GetByIdAsync(orderDto.DeliveryMethodId);
            if (deliveryMethod == null) return BadRequest("No delivery method selected");

            var order = new Order
            {
                BuyerEmail = email,
                ShippingAddress = orderDto.ShippingAddress,
                DeliveryMethod = deliveryMethod,
                PaymentSummary = orderDto.PaymentSummary,
                OrderItems = items,
                PaymentIntentId = cart.PaymentIntentId,
                Subtotal = items.Sum(i => i.Price * i.Quantity),
            };

            unit.Repository<Order>().Add(order);
            if(await unit.Complete())
            {
                return order;
            }
            return BadRequest("Problem creating order");

        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersForUser()
        {
            var spec = new OrderSpecification(User.GetEmail());
            var orders = await unit.Repository<Order>().ListAsync(spec);
            var ordersToReturn = orders.Select(order => order.ToDto()).ToList();
            return Ok(ordersToReturn);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
        {
            var spec = new OrderSpecification(User.GetEmail(), id);
            var order = await unit.Repository<Order>().GetEntityWithSpec(spec);
            if (order == null) return NotFound("Order not found");
            return order.ToDto();
        }
    }
}
