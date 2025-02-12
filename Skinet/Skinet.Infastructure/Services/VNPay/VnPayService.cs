using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Skinet.Infastructure.Data.Libraries;
using Skinet.Infastructure.Data.Models.VNPay;

namespace Skinet.Infastructure.Services.VNPay
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]!);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var pay = new VNPayLibrary();
            var urlCallBack = _configuration["Vnpay:PaymentBackReturnUrl"]!;

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]!);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]!);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]!);
            pay.AddRequestData("vnp_Amount", ((int)model.Amount * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]!);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]!);
            pay.AddRequestData("vnp_OrderInfo", $"{model.Name} {model.OrderDescription} {model.Amount}");
            pay.AddRequestData("vnp_OrderType", model.OrderType);
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            pay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl =
                pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"]!, _configuration["Vnpay:HashSecret"]!);

            return paymentUrl;
        }

        public RefundModel CreateRefundModel(RefundRequestModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]!);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var pay = new VNPayLibrary();
            List<string> secureHashList = new List<string>();
            secureHashList.Add(tick);
            secureHashList.Add(_configuration["Vnpay:Version"]!);
            secureHashList.Add("refund");
            secureHashList.Add(_configuration["Vnpay:TmnCode"]!);
            secureHashList.Add("02");
            secureHashList.Add(model.vnpTxnRef);
            secureHashList.Add(((int)model.vnpAmount).ToString());
            secureHashList.Add(model.vnpTransactionNo);
            secureHashList.Add(model.vnpTransactionDate);
            secureHashList.Add(model.vnpCreateBy);
            secureHashList.Add(timeNow.ToString("yyyyMMddHHmmss"));
            secureHashList.Add(pay.GetIpAddress(context));
            secureHashList.Add(model.vnpOrderInfo);
            //pay.AddRequestData("vnp_RequestId", tick);
            //pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]!);
            //pay.AddRequestData("vnp_Command", "refund");
            //pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]!);
            //pay.AddRequestData("vnp_TransactionType", "02");
            //pay.AddRequestData("vnp_TxnRef", model.vnpTxnRef);
            //pay.AddRequestData("vnp_Amount", ((int)model.vnpAmount).ToString());
            //pay.AddRequestData("vnp_OrderInfo", model.vnpOrderInfo);
            //pay.AddRequestData("vnp_TransactionNo", model.vnpTransactionNo);
            //pay.AddRequestData("vnp_TransactionDate", model.vnpTransactionDate);
            //pay.AddRequestData("vnp_CreateBy", model.vnpCreateBy);
            //pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            //pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));

            var secureHash = pay.CreateRefundSecureHash(secureHashList, _configuration["Vnpay:HashSecret"]!);

            var refundModel = new RefundModel
            {
                vnp_RequestId = tick,
                vnp_Version = _configuration["Vnpay:Version"]!,
                vnp_Command = "refund",
                vnp_TmnCode = _configuration["Vnpay:TmnCode"]!,
                vnp_TransactionType = "02",
                vnp_TxnRef = model.vnpTxnRef,
                vnp_Amount = (decimal)model.vnpAmount,
                vnp_OrderInfo = model.vnpOrderInfo,
                vnp_TransactionNo = model.vnpTransactionNo,
                vnp_TransactionDate = model.vnpTransactionDate,
                vnp_CreateBy = model.vnpCreateBy,
                vnp_CreateDate = timeNow.ToString("yyyyMMddHHmmss"),
                vnp_IpAddr = pay.GetIpAddress(context),
                vnp_SecureHash = secureHash
            };

            return refundModel;
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VNPayLibrary();
            var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);

            return response;
        }

    }
}
