﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Skinet.Core.Entities;
using Skinet.Core.Interfaces;
using Stripe;

namespace Skinet.Infastructure.Services
{
    public class PaymentService(IConfiguration config, ICartService cartService,
       IUnitOfWork unit) : IPaymentService
    {
        public async Task<ShoppingCart?> CreateOrUpdatePaymentIntent(string cartId)
        {
            StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
            var cart = await cartService.GetCartAsync(cartId)
                ?? throw new Exception("Cart unavailable");
            var shippingPrice = await GetShippingPriceAsync(cart) ?? 0;
            await ValidateCartItemsInCartAsync(cart);
            var subtotal = CalculateSubtotal(cart);
            if (cart.Coupon != null)
            {
                subtotal = await ApplyDiscountAsync(cart.Coupon, subtotal);
            }
            var total = subtotal + shippingPrice;
            await CreateUpdatePaymentIntentAsync(cart, total);
            await cartService.SetCartAsync(cart);
            return cart;
        }

        private async Task CreateUpdatePaymentIntentAsync(ShoppingCart cart, long total)
        {
            var service = new PaymentIntentService();
            if (string.IsNullOrEmpty(cart.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = total,
                    Currency = "vnd",
                    PaymentMethodTypes = new List<string> { "card" }
                };
                var intent = await service.CreateAsync(options);
                cart.PaymentIntentId = intent.Id;
                cart.ClientSecret = intent.ClientSecret;
            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = total
                };
                await service.UpdateAsync(cart.PaymentIntentId, options);
            }
        }

        private async Task<long> ApplyDiscountAsync(AppCoupon appCoupon,long amount)
        {
            var couponService = new Stripe.CouponService();
            var coupon = await couponService.GetAsync(appCoupon.CouponId);
            if (coupon.AmountOff.HasValue)
            {
                amount -= (long)coupon.AmountOff * 100;
            }
            if (coupon.PercentOff.HasValue)
            {
                var discount = amount * (coupon.PercentOff.Value / 100);
                amount -= (long)discount;
            }
            return amount;
        }

        private long CalculateSubtotal(ShoppingCart cart)
        {
            var itemTotal = cart.Items.Sum(x => x.Quantity * x.Price * 100);
            return (long)itemTotal;
        }

        private async Task ValidateCartItemsInCartAsync(ShoppingCart cart)
        {
            foreach (var item in cart.Items)
            {
                var productItem = await unit.Repository<Core.Entities.Product>()
                    .GetByIdAsync(Guid.Parse(item.ProductId))
                    ?? throw new Exception("Problem getting product in cart");
                if (item.Price != productItem.Price)
                {
                    item.Price = productItem.Price;
                }
            }
        }

        private async Task<long?> GetShippingPriceAsync(ShoppingCart cart)
        {
            if (cart.DeliveryMethodId.HasValue)
            {
                var deliveryMethod = await unit.Repository<DeliveryMethod>()
                    .GetByIdAsync(cart.DeliveryMethodId.Value)
                        ?? throw new Exception("Problem with delivery method");
                return (long)deliveryMethod.Price * 100;
            }
            return null;
        }
    }
}
