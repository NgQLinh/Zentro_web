using Zentro.Models;

namespace Zentro.Services
{
    public class BangTaiLine2Services : BangTaiServices
    {
        public BangTaiLine2Services(
            BangTaiLine2Model trangThai,
            PhanLoaiLine2Services phanLoaiServices,
            ProductionDatabaseService database,
            NhaMayDataService nhaMayData)
            : base(trangThai, phanLoaiServices, database, nhaMayData, "BT02")
        {
        }
    }
}
