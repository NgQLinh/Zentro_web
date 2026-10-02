using Zentro.Models;

namespace Zentro.Services
{
    public class CamBienServices
    {
        private readonly BangTaiModel trangThai;

        public CamBienServices(BangTaiModel trangThai)
        {
            this.trangThai = trangThai;
        }

        public void BatCamBien()
        {
            trangThai.CamBien = true;
        }

        public void TatCamBien()
        {
            trangThai.CamBien = false;
        }

        public void BatXuLy()
        {
            trangThai.DangXuLy = true;
            trangThai.DangChay = false;
        }

        public void TatXuLy()
        {
            trangThai.DangXuLy = false;

            // Xử lý xong thì chạy lại motor (trừ khi dừng khẩn cấp hoặc lỗi máy)
            if (!trangThai.DungKhanCap && !trangThai.LoiMay)
            {
                trangThai.DangChay = true;
            }
        }
    }
}