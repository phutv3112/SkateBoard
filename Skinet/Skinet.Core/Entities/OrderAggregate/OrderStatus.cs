namespace Skinet.Core.Entities.OrderAggregate
{
    public enum OrderStatus
    {
        Pending,
        PaymentReceived,
        PaymentFailed,
        PaymentMismatch
    }
}
