namespace Skinet.Infastructure.Data.Models.VNPay
{
    public class RefundRequestModel
    {
        public string vnpTxnRef { get; set; }
        public double vnpAmount { get; set; }
        public string vnpTransactionNo { get; set; }
        public string vnpTransactionDate { get; set; }
        public string vnpOrderInfo { get; set; }
        public string vnpCreateBy { get; set; }
    }
}
