using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CuaHangDienThoai.Models;
using CuaHangDienThoai.Models.View;
using CuaHangDienThoai.Data;
using CuaHangDienThoai.Extensions;
using CuaHangDienThoai.Areas.Customer.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CuaHangDienThoai.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class MuaHangController : Controller
    {
        private readonly MobileContext _db;
        [BindProperty]
        public GioHangViewModel GioHangVM { get; set; }
        private int PageSize = 6;

        public MuaHangController(MobileContext db)
        {
            _db = db;
            GioHangVM = new GioHangViewModel()
            {
                DanhSachGH = new List<GioHang>(),
                DanhSachPost = new List<DanhSachPost>()
            };
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GioHang()
        {
            if (HttpContext.Session.GetObject<DangNhap>("DangNhap") == null)
            {
                return RedirectToAction("Index", "DangNhap");
            }
            GioHangVM.KhachHang = _db.KhachHang.Find(HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH);
            List<GioHang> listGioHang = _db.GioHang.Where(gh => gh.MaKH == HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH)
                .Include(gh => gh.DienThoai).ThenInclude(dt => dt.ModelDienThoai).ToList();
            foreach (GioHang gh in listGioHang)
            {
                GioHangVM.DanhSachGH.Add(gh);
                var dsPost = new DanhSachPost()
                {
                    MaDT = gh.MaDT,
                    SoLuong = gh.SoLuong,
                    TrangThai = true
                };
                GioHangVM.DanhSachPost.Add(dsPost);
            }
            return View(GioHangVM);
        }

        [HttpPost, ActionName("GioHang")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GioHangPost()
        {
            bool duSoLuong = true;
            bool coSanPham = false;
            if (GioHangVM.DanhSachPost != null)
            {
                foreach (var item in GioHangVM.DanhSachPost)
                {
                    if (item.TrangThai == true)
                    {
                        coSanPham = true;
                        if (item.SoLuong > _db.DienThoai.FindAsync(item.MaDT).Result.SoLuong)
                        {
                            duSoLuong = false;
                            var dienthoai = _db.DienThoai.FindAsync(item.MaDT).Result;
                            var modelDT = _db.ModelDienThoai.FindAsync(dienthoai.MaModel).Result;
                            TempData["GioHang"] = "Điện thoại " + modelDT.TenModel + "("
                                + dienthoai.Mau + ") hiện chỉ còn lại " + dienthoai.SoLuong + " trong kho. Quý khách vui lòng giảm số lượng";
                        }

                    }
                }
            }

            if (!coSanPham)
                TempData["GioHang"] = "Vui lòng chọn một sản phẩm để đặt hàng";
            if (duSoLuong && coSanPham)
            {
                var donHang = new DonHang()
                {
                    MaKH = HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH,
                    NgayLapDH = DateTime.Now,
                    TrangThai = "Chưa duyệt",
                    TongGia = 0
                };
                _db.DonHang.Add(donHang);
                await _db.SaveChangesAsync();
                foreach (var item in GioHangVM.DanhSachPost)
                {
                    if (item.TrangThai == true && item.SoLuong > 0)
                    {
                        var gioHang = await _db.GioHang.Where(gh => gh.MaDT == item.MaDT && gh.MaKH == HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH).FirstOrDefaultAsync();
                        var dienThoai = await _db.DienThoai.FindAsync(item.MaDT);
                        var chiTietDH = new ChiTietDonHang()
                        {
                            MaDH = donHang.MaDH,
                            MaDT = gioHang.MaDT,
                            SoLuong = item.SoLuong,
                            DonGia = dienThoai.Gia,
                            GiamGia = dienThoai.GiamGia,
                            TongGia = (dienThoai.Gia - dienThoai.GiamGia) * item.SoLuong
                        };
                        dienThoai.SoLuong = dienThoai.SoLuong - item.SoLuong;
                        donHang.TongGia += chiTietDH.TongGia;
                        _db.DienThoai.Update(dienThoai);
                        _db.ChiTietDonHang.Add(chiTietDH);
                        _db.GioHang.Remove(gioHang);
                    }
                    await _db.SaveChangesAsync();
                }
                _db.DonHang.Update(donHang);
                await _db.SaveChangesAsync();
                TempData["GioHang"] = "Đặt hàng thành công";
                return RedirectToAction("DonHang");
            }
            return RedirectToAction("GioHang");
        }

        [HttpPost, ActionName("XoaSanPham")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaGioHang(int MaDT)
        {
            var giohang = _db.GioHang.Where(gh => gh.MaKH == HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH && gh.MaDT == MaDT).FirstOrDefault();
            _db.GioHang.Remove(giohang);
            await _db.SaveChangesAsync();
            return RedirectToAction("GioHang");

        }

        public IActionResult DonHang(int pageIndex = 1, int? searchMa = null, string searchTrangThai = null)
        {
            if (HttpContext.Session.GetObject<DangNhap>("DangNhap") == null)
            {
                return RedirectToAction("Index", "DangNhap");
            }
            var DonHangVM = new DonHangViewModel()
            {
                ListDonHang = _db.DonHang.Where(dh => dh.MaKH == HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH).ToList()
            };

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/MuaHang/DonHang?pageIndex=:");
            param.Append("&searchMa=");
            if (searchMa != null)
            {
                param.Append(searchMa);
            }
            param.Append("&searchTrangThai=");
            if (searchTrangThai != null)
            {
                param.Append(searchTrangThai);
            }



            if (searchMa != null)
            {
                DonHangVM.ListDonHang = DonHangVM.ListDonHang.Where(dh => dh.MaDH.ToString().Contains(searchMa.ToString())).ToList();
            }
            if (searchTrangThai != null)
            {
                DonHangVM.ListDonHang = DonHangVM.ListDonHang.Where(dh => dh.TrangThai == searchTrangThai).ToList();
            }
            var count = DonHangVM.ListDonHang.Count;

            DonHangVM.ListDonHang = DonHangVM.ListDonHang.OrderByDescending(dh => dh.NgayLapDH).Skip((pageIndex - 1) * PageSize).Take(PageSize).ToList();

            DonHangVM.PagingInfo = new PagingInfo
            {
                CurrentPage = pageIndex,
                ItemsPerPage = PageSize,
                TotalItems = count,
                urlParam = param.ToString()
            };

            return View(DonHangVM);
        }

        public IActionResult ChiTietDonHang(int MaDH)
        {
            if (HttpContext.Session.GetObject<DangNhap>("DangNhap") == null)
            {
                return RedirectToAction("Index", "DangNhap");
            }
            var ChiTietDHVM = new DonHangViewChiTiet()
            {
                DonHangs = _db.DonHang.Include(dh => dh.KhachHang).Where(dh => dh.MaDH == MaDH).FirstOrDefault(),
                ChiTietDonHangs = _db.ChiTietDonHang.Include(ct => ct.DienThoai).ThenInclude(dt => dt.ModelDienThoai).Where(ct => ct.MaDH == MaDH).ToList()
            };
            return View(ChiTietDHVM);
        }

        public async Task<IActionResult> HuyDonHang(int MaDH)
        {
            if (HttpContext.Session.GetObject<DangNhap>("DangNhap") == null)
            {
                return RedirectToAction("Index", "DangNhap");
            }
            var donHang = _db.DonHang.Find(MaDH);
            donHang.TrangThai = "Đã hủy";
            var listCT = await _db.ChiTietDonHang.Where(ct => ct.MaDH == MaDH).ToListAsync();
            var listDT = new List<DienThoai>();
            foreach (var item in listCT)
            {
                var dienThoai = await _db.DienThoai.FindAsync(item.MaDT);
                dienThoai.SoLuong += item.SoLuong;
                listDT.Add(dienThoai);
            }
            _db.DonHang.Update(donHang);
            _db.DienThoai.UpdateRange(listDT);
            await _db.SaveChangesAsync();
            return RedirectToAction("ChiTietDonHang", new { MaDH = MaDH });

        }

        public IActionResult HoaDon(int pageIndex = 1, int? searchMa = null)
        {
            if (HttpContext.Session.GetObject<DangNhap>("DangNhap") == null)
            {
                return RedirectToAction("Index", "DangNhap");
            }
            var HoaDonVM = new HoaDonViewModel()
            {
                ListHoaDon = _db.HoaDon.Where(hd => hd.MaKH == HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH).ToList()
            };

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/MuaHang/HoaDon?pageIndex=:");
            param.Append("&searchMa=");
            if (searchMa != null)
            {
                param.Append(searchMa);
            }


            if (searchMa != null)
            {
                HoaDonVM.ListHoaDon = HoaDonVM.ListHoaDon.Where(dh => dh.MaDH.ToString().Contains(searchMa.ToString())).ToList();
            }
            var count = HoaDonVM.ListHoaDon.Count;

            HoaDonVM.ListHoaDon = HoaDonVM.ListHoaDon.OrderByDescending(dh => dh.NgayLapHD).Skip((pageIndex - 1) * PageSize).Take(PageSize).ToList();

            HoaDonVM.PagingInfo = new PagingInfo
            {
                CurrentPage = pageIndex,
                ItemsPerPage = PageSize,
                TotalItems = count,
                urlParam = param.ToString()
            };
            return View(HoaDonVM);
        }

        public IActionResult ChiTietHoaDon(int MaHD)
        {
            if (HttpContext.Session.GetObject<DangNhap>("DangNhap") == null)
            {
                return RedirectToAction("Index", "DangNhap");
            }
            var ChiTietHDVM = new ChiTietHoaDonViewModel()
            {
                HoaDon = _db.HoaDon.Where(hd => hd.MaHD == MaHD).FirstOrDefault(),
                ChiTietHoaDons = _db.ChiTietHoaDon.Include(ct => ct.DienThoai).ThenInclude(dt => dt.ModelDienThoai).Include(ct => ct.IMEI_DienThoais).Where(ct => ct.MaHD == MaHD).ToList()
            };
            return View(ChiTietHDVM);
        }
    }
}