using Skinet.Core.Entities;

namespace Skinet.Core.Interfaces
{
    public interface ICouponService
    {
        Task<AppCoupon?> GetCouponFromPromoCode(string code);
    }
}
