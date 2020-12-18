using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CuaHangDienThoai.Data;
using Microsoft.EntityFrameworkCore;
using CuaHangDienThoai.Models;
using System.Diagnostics;
using CuaHangDienThoai.Models.View;
using Microsoft.AspNetCore.Http;
using CuaHangDienThoai.Extensions;
using CuaHangDienThoai.Areas.Customer.Identity;
using System.Text;

namespace CuaHangDienThoai.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly MobileContext _db;
        [BindProperty]
        public ChiTietViewModel ChiTietVM { get; set; }
        private int PageSize = 12;

        public HomeController(MobileContext db)
        {
            _db = db;
            ChiTietVM = new ChiTietViewModel();
        }

        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Index(int pageIndex = 1, string searchTen = null, int? searchHang = null, int? searchBatDau = null, int? searchKetThuc = null, string searchSapXep = null)
        {
            var TrangChuVM = new TrangChuViewModel()
            {
                DanhSachModel = _db.ModelDienThoai.Where(md => md.DienThoais.Count != 0 && md.TrangThai == true).Include(md => md.DienThoais).ToList(),
                DanhSachHang = _db.Hang.Where(h => h.TrangThai == true).ToList()
            };
            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Home/Index?pageIndex=:");
            param.Append("&searchTen=");
            if (searchTen != null)
            {
                param.Append(searchTen);
            }
            param.Append("&searchHang=");
            if (searchHang != null)
            {
                param.Append(searchHang);
            }
            param.Append("&searchBatDau=");
            if (searchBatDau != null)
            {
                param.Append(searchBatDau);
            }
            param.Append("&searchKetThuc=");
            if (searchKetThuc != null)
            {
                param.Append(searchKetThuc);
            }
            param.Append("&searchSapXep=");
            if (searchSapXep != null)
            {
                param.Append(searchSapXep);
            }




            if (searchHang != null)
            {
                TrangChuVM.DanhSachModel = TrangChuVM.DanhSachModel.Where(md => md.MaHang == searchHang).ToList();
            }
            if (searchBatDau != null && searchKetThuc != null)
            {
                TrangChuVM.DanhSachModel = TrangChuVM.DanhSachModel.Where(md => md.DienThoais.Min(dt => dt.Gia - dt.GiamGia) >= searchBatDau
                    && md.DienThoais.Min(dt => dt.Gia - dt.GiamGia) <= searchKetThuc).ToList();
            }
            if (searchSapXep != null)
            {
                if (searchSapXep == "CaoDenThap")
                    TrangChuVM.DanhSachModel = TrangChuVM.DanhSachModel.OrderByDescending(md => md.DienThoais.Min(dt => dt.Gia - dt.GiamGia)).ToList();
                else if (searchSapXep == "ThapDenCao")
                    TrangChuVM.DanhSachModel = TrangChuVM.DanhSachModel.OrderBy(md => md.DienThoais.Min(dt => dt.Gia - dt.GiamGia)).ToList();
            }
            if (searchTen != null)
            {
                TrangChuVM.DanhSachModel = TrangChuVM.DanhSachModel.Where(md => md.TenModel.ToLower().Contains(searchTen.ToLower())).ToList();
            }


            var count = TrangChuVM.DanhSachModel.Count;
            if (searchSapXep == null)
            {
                TrangChuVM.DanhSachModel = TrangChuVM.DanhSachModel.OrderByDescending(md => md.DienThoais.Min(dt => dt.Gia - dt.GiamGia)).ToList();
            }
            TrangChuVM.DanhSachModel = TrangChuVM.DanhSachModel.Skip((pageIndex - 1) * PageSize).Take(PageSize).ToList();



            TrangChuVM.PagingInfo = new PagingInfo
            {
                CurrentPage = pageIndex,
                ItemsPerPage = PageSize,
                TotalItems = count,
                urlParam = param.ToString()
            };

            return View(TrangChuVM);
        }

        public IActionResult LocTrangChu(int pageIndex = 1, string searchTen = null, int? searchHang = null, int? searchBatDau = null, int? searchKetThuc = null, string searchSapXep = null)
        {
            if (searchTen != null || searchHang != null || searchBatDau != null || searchKetThuc != null || searchSapXep != null)
                pageIndex = 1;
            return RedirectToAction("Index");
        }
        [HttpGet]
        public JsonResult SoLuongGioHang()
        {
            if (HttpContext.Session.GetObject<DangNhap>("DangNhap") != null)
                return new JsonResult(_db.GioHang.Where(gh => gh.MaKH == HttpContext.Session.GetObject<DangNhap>("DangNhap").MaKH).Sum(gh => gh.SoLuong).ToString());
            return new JsonResult("Chua dang nhap");
        }

        public async Task<IActionResult> ChiTiet(int? MaModel)
        {
            if (MaModel == null)
                return NotFound();
            else
            {
                ChiTietVM.DanhSachDT = await _db.DienThoai.Where(d => d.MaModel == MaModel).ToListAsync();
                ChiTietVM.ModelDT = await _db.ModelDienThoai.FindAsync(MaModel);
                ChiTietVM.ModelDT.Hang = await _db.Hang.FindAsync(ChiTietVM.ModelDT.MaHang);
                ChiTietVM.SoLuong = 1;
                ChiTietVM.MaDT = 0;
                return View(ChiTietVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChiTiet()
        {
            if (ModelState.IsValid)
            {
                if (HttpContext.Session.GetObject<DangNhap>("DangNhap") != null)
                {
                    var obj = HttpContext.Session.GetObject<DangNhap>("DangNhap");
                    GioHang gh = new GioHang()
                    {
                        MaDT = ChiTietVM.MaDT,
                        MaKH = obj.MaKH,
                        SoLuong = ChiTietVM.SoLuong,
                        KhachHang = _db.KhachHang.Where(kh => kh.MaKH == obj.MaKH).FirstOrDefault(),
                        DienThoai = _db.DienThoai.Where(dt => dt.MaDT == ChiTietVM.MaDT).FirstOrDefault()
                    };
                    if (_db.GioHang.Where(g => (g.MaKH == gh.MaKH && g.MaDT == gh.MaDT)).FirstOrDefault() == null)
                    {
                        _db.GioHang.Add(gh);
                    }
                    else
                    {
                        _db.GioHang.Where(g => (g.MaKH == gh.MaKH && g.MaDT == gh.MaDT)).FirstOrDefault().SoLuong += gh.SoLuong;
                    }

                    await _db.SaveChangesAsync();
                    return RedirectToAction("GioHang", "MuaHang");
                }
                else
                {
                    TempData["MaModel"] = _db.DienThoai.Find(ChiTietVM.MaDT).MaModel.ToString();
                    return RedirectToAction("Index", "DangNhap");
                }
            }
            return View(ChiTietVM);
        }


    }
}
