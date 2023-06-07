using Microsoft.AspNetCore.Mvc;
using PhoneBookBusinessLayer.InterfacesOfManagers;
using PhoneBookEntityLayer.ViewModels;
using PhoneBookUI.Areas.Admin.Models;
using System.Drawing.Printing;

namespace PhoneBookUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Route("a/[Controller]/[Action]/{id?}")] bu route verildiğinde [Action] yazan yere action'ın tam adını yazmadan sayfsa açılmaz
    /* [Route("a/h")] */// bu route verildiğinde controller'a nasıl ulaşıldığı berlitilir ve action'a ulaşılma konusundaki kuralı action üzerine yazılan kural belirler.
    [Route("admin")]
    public class HomeController : Controller
    {
        private readonly IMemberManager _memberManager;
        private readonly IPhoneTypeManager _phoneTypeManager;
        private readonly IMemberPhoneManager _memberPhoneManager;
        private readonly IWebHostEnvironment _environment;

        public HomeController(IMemberManager memberManager, IPhoneTypeManager phoneTypeManager, IMemberPhoneManager memberPhoneManager, IWebHostEnvironment environment)
        {
            _memberManager = memberManager;
            _phoneTypeManager = phoneTypeManager;
            _memberPhoneManager = memberPhoneManager;
            _environment = environment;
        }

        [HttpGet]
        //[Route("d")]
        [Route("dsh")] // Action'un ismi çok uzun olabilir url'e action'ın isminin hepsini yazmak istemezsek action'a Route verebiliriz.
        public IActionResult Dashboard()

        {
            //bu ay sisteme kayıt olan üye sayısı
            DateTime thisMonth = new DateTime(DateTime.Now.Year,
                DateTime.Now.Month, 1);

            ViewBag.MontlyMemberCount = _memberManager.GetAll(x =>
            x.CreatedDate > thisMonth.AddDays(-1)).Data.Count();

            //bu ay sisteme eklenen numara sayısı

            ViewBag.MontlyContactCount = _memberPhoneManager.GetAll(x =>
            x.CreatedDate > thisMonth.AddDays(-1)).Data.Count();

            var members = _memberManager.GetAll().Data.OrderBy(x => x.CreatedDate);
            //En son eklenen üyenin adı soyadı
            ViewBag.LastMember = $"{members.LastOrDefault()?.Name} {members.LastOrDefault()?.Surname}";

            // Rehbere en son eklenen kişinin adı soyadı

            var contacts = _memberPhoneManager.GetAll().Data.OrderBy(x => x.CreatedDate);

            ViewBag.LastContact = contacts.LastOrDefault()?.FriendNameSurname;

            return View();
        }

        [Route("/admin/GetPhoneTypePieData")] //buradaki admin controler'ın route'u
        public JsonResult GetPhoneTypePieData()
        {
            try
            {
                Dictionary<string, int> model = new Dictionary<string, int>();

                var data = _memberPhoneManager.GetAll().Data;
                foreach (var item in data)
                {
                    if (model.ContainsKey(item.PhoneType.Name)) // wissen kurs tipinden var mı?
                    {
                        //sayıyı 1 arttırsın
                        model[item.PhoneType.Name] += 1;
                    }
                    else
                    {
                        model.Add(item.PhoneType.Name, 1);
                    }
                } // foreach bitti

                return Json(new
                {
                    isSuccess = true,
                    message = "Veriler geldi",
                    types = model.Keys.ToArray(),
                    points = model.Values.ToArray()
                });

            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = "Veriler getirilemedi!" });

            }
        }


        [HttpGet]
        [Route("uye")]
        public IActionResult MemberIndex()
        {
            try
            {
                var data = _memberManager.GetAll().Data;

                return View(data);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik hata oldu! " + ex.Message);
                return View();
            }
        }


        [HttpGet]
        [Route("duzenle")]
        public IActionResult MemberEdit(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    ModelState.AddModelError("", "id gelmediği için kullanıcı bulunmadı!");
                    return View(new MemberViewModel());
                }
                var member = _memberManager.GetById(id).Data;
                if (member == null)
                {
                    ModelState.AddModelError("", "Kullanıcı bulunmadı!");
                    return View(new MemberViewModel());
                }
                return View(member);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!" + ex.Message);
                return View(new MemberViewModel());
            }
        }

        [HttpPost]
        [Route("duzenle")]
        public IActionResult MemberEdit(MemberViewModel model)
        {
            try
            {
                var member = _memberManager.GetById(model.Email).Data;
                if (member == null)
                {
                    ModelState.AddModelError("", "Kullanıcı bulunamadı!");
                    return View(model);
                }
                member.Name = model.Name;
                member.Surname = model.Surname;
                member.BirthDate = model.BirthDate;
                member.Gender = model.Gender;

                //1) Upload pic null değilse RESİM Yüklemesi yapılmalı
                //2) Upload pic yüklenen resim mi?
                //3) Upload pic yüklenen dosya botuyu > 0 mı?
                if (model.UploadPicture != null &&
                    model.UploadPicture.ContentType.Contains("image") &&
                    model.UploadPicture.Length > 0)
                {
                    //wwwroot klasörünün içinde MemberPictures isimli bir klasör oluşturup o klasörün içine resmi kaydetmeliyim
                    //Resmi kaydederken isimlendirmesini ben burada yeniden yapmalıyım

                    string uploadPath = Path.Combine(_environment.WebRootPath, "MemberPictures");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath); // MemberPictures isimli klasörden wwwroot içinde yoksa klasörü OLUŞTURACAK
                    }

                    //betulaksan-gmail-com
                    string memberPictureName = model.Email.Replace("@", "-")
                        .Replace(".", "-");

                    //dipnot: Resim ismi olarak Guid kullanılabilir
                    // product name gibi alanlarda guid vb eklenebilir

                    //uzantı
                    string extentionName = Path.GetExtension(model.UploadPicture.FileName);


                    string filePath = Path.Combine(uploadPath, 
                        $"{memberPictureName}{extentionName}");
              
                    using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                         model.UploadPicture.CopyTo(fileStream); // CopyTo halini de deneyelim
                    }

                     member.Picture = $"/MemberPictures/{memberPictureName}{extentionName}";
                    
                } // if bitti

                if (_memberManager.Update(member).IsSuccess)
                {
                    TempData["MemberEditSuccessMsg"] = $"{model.Name} {model.Surname} isimli üyenin bilgileri güncellenmiştir!";

                    return RedirectToAction("MemberIndex");
                }
                else
                {
                    ModelState.AddModelError("", "Güncelleme başarısız! Tekrar deneyiniz!");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir sorun oluştu!");
                return View(model);
            }
        }
    }
}
