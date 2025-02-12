using Microsoft.AspNetCore.Http;
using Skinet.Infastructure.Data.Models.VNPay;

namespace Skinet.Infastructure.Services.VNPay
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        RefundModel CreateRefundModel(RefundRequestModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);

    }
}
