using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skinet.Core.Entities.OrderAggregate;

namespace Skinet.Infastructure.Config
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.OwnsOne(o => o.ItemOrdered, o => o.WithOwner());
            builder.Property(o => o.Price).HasColumnType("decimal(18,2)");
        }
    }
}
