using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class PhanLoaiLine2Services : PhanLoaiServices
    {
        public PhanLoaiLine2Services(BangTaiLine2Model trangThai, ProductionDatabaseService database)
            : base(trangThai, database, "BT02")
        {
        }
    }
}
