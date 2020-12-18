using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuaHangDienThoai.Common;
using CuaHangDienThoai.Data;
using CuaHangDienThoai.Extensions;
using CuaHangDienThoai.Models.View;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangDienThoai.Areas.Customer.Identity
{
    [Area("Customer")]
    public class ResetPasswordController : Controller
    {
        private readonly MobileContext _mb;
        [BindProperty]
        public ResetPasswordViewModel ResetPasswordVM { get; set; }
        public ResetPasswordController(MobileContext mb)
        {
            _mb = mb;
            ResetPasswordVM = new ResetPasswordViewModel();
        }
        public IActionResult ResetPass(string Email)
        {
            TempData["Reset"] = "Mã xác nhận đã dược gửi vào Gmail của bạn";
            TempData.Keep();
            ResetPasswordVM.Email = Email;
            return View(ResetPasswordVM);
        }
        [HttpPost, ActionName("ResetPass")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            if (ModelState.IsValid)
            {
                var Code = HttpContext.Session.GetString(CommonCustomer.Code);
                if (ResetPasswordVM.Code.Equals(Code))
                {
                    HttpContext.Session.Clear();
                    var taiKhoan = _mb.TaiKhoan.Include(tk => tk.KhachHang).Where(tk => tk.KhachHang.Email == ResetPasswordVM.Email).FirstOrDefault();
                    taiKhoan.MatKhau = MD5.GetMD5(ResetPasswordVM.Password);
                    await _mb.SaveChangesAsync();
                    return RedirectToAction("Success");

                }
                else
                {
                    ModelState.AddModelError("", "Mã xác nhận không đúng!");
                    return View("ResetPass");
                }
            }
            return View("ResetPass");

        }
        public IActionResult Success()
        {
            return View();
        }
    }
}