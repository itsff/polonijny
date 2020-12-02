using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SlownikPolonijny.Web.Models;
using SlownikPolonijny.Dal;
using AspNetCore.Identity.Mongo;
using AspNetCore.Identity.Mongo.Model;

namespace SlownikPolonijny.Web.Controllers
{
    
    [Authorize(Policy="RequireAdmin")]
    public class AdminController : Controller
    {
        readonly ILogger<AdminController> _logger;
        readonly UserManager<WebUser> _userManager;
        readonly IRepository _repo;


        public AdminController(ILogger<AdminController> logger, UserManager<WebUser> userManager, IRepository repository)
        {
            _logger = logger;
            _userManager = userManager;
            _repo = repository;
        }

        private bool IsAdminUser => this.User.IsInRole("@admin");

        private Task<WebUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        [Route("/admin/edytuj/{id}")]
        [HttpGet]
        public IActionResult Edit(string id)
        {
            Entry entry = _repo.GetEntryById(id);
            if (entry != null)
            {
                var vm = new EditEntryViewModel();
                vm.Entry = entry;
                return View(vm);
            }
            else
            {
                return Content("Nada");
            }
        }

        [Route("/admin/edytuj")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm]EditEntryModel model)
        {
            var r = new AddEntryResultModel();
            r.Problems = model.Validate();
            r.Name = model.Name;
            r.Success = false;

            if (r.Problems.Count == 0)
            {
                try
                {
                    var user = await this.GetCurrentUserAsync();

                    var e = _repo.GetEntryById(model.Id);
                    e.Name = model.Name;
                    e.Meanings = model.Meanings;
                    e.EnglishMeanings = model.EnglishMeanings;
                    e.SeeAlso = model.SeeAlso;
                    e.Examples = model.Examples;
                    e.TimeAdded = DateTimeOffset.UtcNow;
                    e.ApprovedBy = user.UserName;

                    _repo.UpdateEntry(e);
                    r.Success = true;
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    r.Problems.Add("Baza danych się popsuła");
                }
            }

            return Json(r);
        }
    }
}
