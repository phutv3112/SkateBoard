using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skinet.Core.Entities.OrderAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skinet.Infastructure.Config
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.OwnsOne(o => o.ShippingAddress, o => o.WithOwner());
            builder.OwnsOne(o => o.PaymentSummary, o => o.WithOwner());

            builder.Property(s => s.Status)
                .HasConversion(o => o.ToString(), 
                o => (OrderStatus)Enum.Parse(typeof(OrderStatus), o));
            builder.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
            builder.HasMany(x => x.OrderItems).WithOne().OnDelete(DeleteBehavior.Cascade);

            // config datetime local
            builder.Property(o => o.OrderDate).HasConversion(
                d => d.ToUniversalTime(),
                d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
        }
    }
}
