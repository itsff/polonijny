using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
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
        readonly IEntryAuditor _auditor;
        readonly IMemoryCache _cache;


        public AdminController(ILogger<AdminController> logger,
                               UserManager<WebUser> userManager,
                               IRepository repository,
                               IEntryAuditor auditor,
                               IMemoryCache cache)
        {
            _logger = logger;
            _userManager = userManager;
            _repo = repository;
            _auditor = auditor;
            _cache = cache;
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
            r.Url = Url.Action("Entry", "Home", new { name = model.Name });

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
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    r.Problems.Add("Baza danych się popsuła");
                }
            }

            return Json(r);
        }

        [Route("/admin/zatwierdz/{id}")]
        [HttpPost]
        public async Task<IActionResult> Approve(string id)
        {
            var r = new AddEntryResultModel();
            
            try
            {
                Entry entry = _repo.GetEntryById(id);
                if (entry != null)
                {
                    var user = await this.GetCurrentUserAsync();

                    entry.ApprovedBy = user.UserName;
                    _repo.UpdateEntry(entry);
                }
                else
                {
                    r.Problems.Add("Nie ma takiego hasła");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.ToString());
                r.Problems.Add("Błąd bazy danych");
            }
            
            return Json(r);
        }

        [Route("/admin/audyt/{id}")]
        [HttpGet]
        public IActionResult Audit(string id)
        {
            var r = new AddEntryResultModel();
            r.Name = id;

            try
            {
                r.Problems = _auditor.PerformEntryAudit(id);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.ToString());
                r.Problems.Add("Baza danych się popsuła");
            }

            return Json(r);
        }

        List<EntryAuditResultModel> AuditAllEntries()
        {
            var auditedEntries = new List<EntryAuditResultModel>();

            _logger.LogInformation("Starting mega audit");
            foreach (var entry in _repo.GetAllEntries())
            {
                var problems = _auditor.PerformEntryAudit(entry);
                if (problems.Count > 0)
                {
                    var m = new EntryAuditResultModel()
                    {
                        Id = entry.Id.ToString(),
                        Name = entry.Name,
                        IsApproved = string.IsNullOrEmpty(entry.ApprovedBy),
                        Problems = problems
                    };
                    auditedEntries.Add(m);
                }
            }
            _logger.LogInformation($"Finished mega audit. Number of entries: {auditedEntries.Count}");

            return auditedEntries;
        }

        [Route("/admin/mega-audyt")]
        public async Task<IActionResult> MegaAudit()
        {
            IReadOnlyList<EntryAuditResultModel> auditResults = await _cache.GetOrCreateAsync(
                "mega-audyt",
                cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    return Task.Run(AuditAllEntries);
                });
           
            var vm = new MegaAuditViewModel()
            {
                AuditResults = auditResults
            };

            return View(vm);
        }

        [Route("/admin/wszystkie")]
        public IActionResult All()
        {
            var vm = new EntryListViewModel()
            {
                ShowEdit = true,
                Entries = _repo.GetAllEntries().OrderBy(e => e.NameLowerCase).ToList()
            };

            return View(vm);
        }
    }
}
