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

namespace SlownikPolonijny.Web.Controllers
{
    public class HomeController : Controller
    {
        readonly ILogger<HomeController> _logger;
        readonly UserManager<WebUser> _userManager;
        readonly IRepository _repo;
        readonly IEntryAuditor _auditor;
        readonly Random _random = new Random();

        public HomeController(ILogger<HomeController> logger,
                              UserManager<WebUser> userManager,
                              IRepository repository,
                              IEntryAuditor auditor)
        {
            _logger = logger;
            _userManager = userManager;
            _repo = repository;
            _auditor = auditor;
        }

        bool IsAdminUser => this.User.IsInRole("@admin");

        private Task<WebUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        [Route("")]
        public IActionResult Index()
        {
            var vm = new IndexViewModel();
            vm.Entry = _repo.GetRandomExtryWithExample();

            if (vm.Entry.Examples.Count == 1)
            {
                vm.Example = vm.Entry.Examples[0];
            }
            else
            {
                vm.Example = vm.Entry.Examples[_random.Next(vm.Entry.Examples.Count)];
            }

            return View(vm);
        }

        string FindExample(IReadOnlyList<Entry> entries)
        {
            string example = null;
            var entry = entries.Where(e => e.Examples.Count > 0).FirstOrDefault();
            if (entry != null)
            {
                if (entry.Examples.Count == 1)
                {
                    example = entry.Examples[0];
                }
                else
                {
                    example = entry.Examples[_random.Next(entry.Examples.Count)];
                }
            }
            return example;
        }


        [Route("haslo/{name:dashed}")]
        public IActionResult Entry(string name)
        {
            name = DashedParameterTransformer.FromDashed(name);

            ViewData["Link"] = Url.Action("Entry", "Home", new { name = name });
            var entries = _repo.GetEntriesByName(name);
            if (entries != null && entries.Count > 0)
            {
                var vm = new EntryViewModel()
                {
                    Name = entries[0].Name,
                    ShowEdit = IsAdminUser,
                    Entries = entries
                };
                ViewData["Title"] = vm.Name;
                ViewData["Description"] = FindExample(vm.Entries);
                return View(vm);
            }
            else
            {
                ViewData["Title"] = name;
                ViewData["Entry"] = name;
                return View("EntryNotFound");
            }
        }

        [Route("losuj")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Random()
        {
            var entries = _repo.GetRandomEntries();
            if (entries != null && entries.Count > 0)
            {
                return RedirectToAction("Entry", new { name = entries[0].Name });
            }
            else
            {
                return Content("Nie ma w ogóle haseł");
            }
        }

        [Route("litera/{letter}")]
        public IActionResult Letter(char letter)
        {
            var bigLetter = char.ToUpper(letter, SlownikPolonijny.Dal.Entry.Culture).ToString();
            ViewData["Title"] = bigLetter;

            var entries = _repo.GetEntriesForLetter(letter);
            if (entries != null && entries.Count > 0)
            {              
                var vm = new LetterViewModel()
                {
                    Letter = bigLetter,
                    Entries = entries,
                    ShowEdit = IsAdminUser
                };
                return View(vm);
            }
            else
            {
                ViewData["Letter"] = bigLetter;
                return View("LetterNotFound");
            }
        }

        [Route("nowe")]
        public IActionResult Latest()
        {
            ViewData["Title"] = "Najnowsze hasła";

            var entries = _repo.GetLatestEntries();
            if (entries != null && entries.Count > 0)
            {              
                var vm = new LatestViewModel()
                {
                    Entries = entries,
                    ShowEdit = IsAdminUser
                };
                return View(vm);
            }
            else
            {
                return Content("Hmmm... wcale nie ma haseł... :-(");
            }
        }

        [Route("szukaj/{prefix}")]
        public IActionResult Search(string prefix)
        {
            var entries = _repo.Search(prefix);
            return Json(entries.Select(e => e.NameLowerCase).Distinct().ToArray());
        }

        [Route("dodaj")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm]AddEntryModel model)
        {
            var r = new AddEntryResultModel();
            r.Problems = model.Validate();
            r.Name = model.Name;
            r.Url = Url.Action("Entry", "Home", new { name = model.Name });

            if (r.Problems.Count == 0)
            {
                try
                {
                    var e = new Entry();
                    e.Name = model.Name;
                    e.Meanings = model.Meanings;
                    e.EnglishMeanings = model.EnglishMeanings;
                    e.SeeAlso = model.SeeAlso;
                    e.Examples = model.Examples;
                    e.TimeAdded = DateTimeOffset.UtcNow;
                    e.FromInternet = true;
                    e.IPAddress = null;

                    // TODO: Clean up this hack
                    r.Problems = _auditor
                            .PerformEntryAudit(e)
                            .Where(txt => !txt.Contains("link"))
                            .ToList();

                    if (r.Problems.Count == 0)
                    {
                        var user = await this.GetCurrentUserAsync();
                        e.ApprovedBy = user?.UserName;

                        _repo.AddEntry(e);
                    }
                }
                catch (System.Exception)
                {
                    r.Problems.Add("Baza danych się popsuła");
                }
            }

            return Json(r);
        }

        [Route("dodaj")]
        [HttpGet]
        public IActionResult Add()
        {
            var vm = new AddEntryViewModel();
            return View(vm);
        }

        [Route("o-slowniku")]
        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            ViewData["ErrorCode"] = $"Błąd!";
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("/Home/HandleError/{code:int}")]
        public IActionResult HandleError(int code)
        {
            ViewData["ErrorCode"] = $"Błąd {code}";
            ViewData["ErrorMessage"] = $"Coś poczło nie tak. Wystąpił błąd {code}.";
            if (code == 404)
            {
                ViewData["ErrorMessage"] = "Nie ma takiej strony.... :-(";
            }
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
