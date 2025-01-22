namespace Skinet.Core.Entities.OrderAggregate
{
    public class PaymentSummary
    {
        public int Last4 { get; set; }
        public required string Brand { get; set; }
        public int ExMonth { get; set; }
        public int ExYear { get; set; }
    }
}
