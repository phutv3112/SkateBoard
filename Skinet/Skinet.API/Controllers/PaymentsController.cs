using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Skinet.API.Extentions;
using Skinet.API.SignalR;
using Skinet.Core.Entities;
using Skinet.Core.Entities.OrderAggregate;
using Skinet.Core.Interfaces;
using Skinet.Core.Specifications;
using Skinet.Infastructure.Data.Models.VNPay;
using Skinet.Infastructure.Services.VNPay;
using Stripe;

namespace Skinet.API.Controllers
{
    public class PaymentsController(IPaymentService paymentService,
        IUnitOfWork unit, ILogger<PaymentsController> logger, 
        IConfiguration config, IHubContext<NotificationHub> hubContext,
        IVnPayService vnPayService) : BaseApiController
    {
        private readonly string _whSecret = config["StripeSettings:WhSecret"]!;
        [Authorize]
        [HttpPost("{cartId}")]
        public async Task<ActionResult<ShoppingCart>> CreateOrUpdatePaymentIntent(string cartId)
        {
            var cart = await paymentService.CreateOrUpdatePaymentIntent(cartId);
            if (cart == null) return BadRequest("Problem with your cart!");
            return Ok(cart);
        }

        [HttpGet("delivery-methods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethods()
        {
            return Ok(await unit.Repository<DeliveryMethod>().ListAllAsync());
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = ConstructStripeEvent(json);

                if (stripeEvent.Data.Object is not PaymentIntent intent)
                {
                    return BadRequest("Invalid event data");
                }

                await HandlePaymentIntentSucceeded(intent);

                return Ok();
            }
            catch (StripeException ex)
            {
                logger.LogError(ex, "Stripe webhook error");
                return StatusCode(StatusCodes.Status500InternalServerError, "Webhook error");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
            }
        }

        private async Task HandlePaymentIntentSucceeded(PaymentIntent intent)
        {
            if (intent.Status == "succeeded")
            {
                var spec = new OrderSpecification(intent.Id, true);
                var order = await unit.Repository<Order>().GetEntityWithSpec(spec)
                    ?? throw new Exception("Order not found");
                var orderTotalInCents = (long)Math.Round(order.GetTotal() * 100,
                    MidpointRounding.AwayFromZero);
                if (orderTotalInCents != intent.Amount)
                {
                    order.Status = OrderStatus.PaymentMismatch;
                }
                else
                {
                    order.Status = OrderStatus.PaymentReceived;
                }
                await unit.Complete();
                var connectionId = NotificationHub.GetConnectionIdByEmail(order.BuyerEmail);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await hubContext.Clients.Client(connectionId)
                        .SendAsync("OrderCompleteNotification", order.ToDto());
                }
            }
        }

        private Event ConstructStripeEvent(string json)
        {
            try
            {
                return EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"],
                    _whSecret);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to construct stripe event");
                throw new StripeException("Invalid signature");
            }
        }

        [HttpPost("stripe/refund/{paymentIntentId}")]
        public async Task<IActionResult> RefundPayment(string paymentIntentId)
        {
            try
            {
                var service = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                };
                var refund = await service.CreateAsync(refundOptions);

                if (refund.Status == "succeeded")
                {
                    var spec = new OrderSpecification(paymentIntentId, true);
                    var order = await unit.Repository<Order>().GetEntityWithSpec(spec)
                        ?? throw new Exception("Order not found");

                    order.Status = OrderStatus.Refunded;
                    await unit.Complete();

                    var connectionId = NotificationHub.GetConnectionIdByEmail(order.BuyerEmail);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await hubContext.Clients.Client(connectionId)
                            .SendAsync("OrderRefundedNotification", order.ToDto());
                    }
                }

                return Ok(new { Message = "Refund processed successfully", RefundId = refund.Id });
            }
            catch (StripeException ex)
            {
                logger.LogError(ex, "Stripe refund error");
                return StatusCode(StatusCodes.Status500InternalServerError, "Refund error");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
            }
        }


        [HttpPost("vnpay/refund-model")]
        public async Task<ActionResult<RefundRequestModel>> GetVnPayRefundModel(RefundRequestModel model)
        {
            try
            {
                var refundModel = vnPayService.CreateRefundModel(model, HttpContext);
                return Ok(refundModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
            }
        }

        [HttpPost("vnpay-url")]
        public async Task<ActionResult<string>> GetVnPayUrl(PaymentInformationModel model)
        {
            try
            {
                model.Amount = model.Amount * 100;
                var url = vnPayService.CreatePaymentUrl(model, HttpContext);
                return Ok(url);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
            }
        }
    }
}
