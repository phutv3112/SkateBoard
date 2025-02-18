﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Skinet.Core.Entities;
using Skinet.Core.Interfaces;
using Stripe;

namespace Skinet.Infastructure.Services
{
    public class PaymentService(IConfiguration config, ICartService cartService,
        IGenericRepository<Core.Entities.Product> productRepo,
        IGenericRepository<DeliveryMethod> deliveryMethodRepo) : IPaymentService
    {
        public async Task<ShoppingCart?> CreateOrUpdatePaymentIntent(string cartId)
        {
            StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
            var cart = await cartService.GetCartAsync(cartId);

            if (cart == null) return null;
            var shippingPrice = 0m;

            if (cart.DeliveryMethodId.HasValue)
            {
                var deliveryMethod = await deliveryMethodRepo.GetByIdAsync(cart.DeliveryMethodId.Value);
                if(deliveryMethod != null)
                {
                    shippingPrice = deliveryMethod.Price;
                }
                else
                {
                    return null;
                }
            }
            foreach (var item in cart.Items)
            {
                var productItem = await productRepo.GetByIdAsync(Guid.Parse(item.ProductId));
                if (productItem == null) return null;

                if (productItem.Price != item.Price)
                {
                    item.Price = productItem.Price;
                }
            }

            var service = new PaymentIntentService();
            PaymentIntent? intent = null;

            if (string.IsNullOrEmpty(cart.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)cart.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100,
                    Currency = "usd",
                    PaymentMethodTypes = ["card"]
                };
                intent = await service.CreateAsync(options);

                cart.PaymentIntentId = intent.Id;
                cart.ClientSecret = intent.ClientSecret;
            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = (long)cart.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100,
                };
                intent = await service.UpdateAsync(cart.PaymentIntentId, options);
            }
            await cartService.SetCartAsync(cart);

            return cart;
        }
    }
}
