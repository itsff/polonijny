using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SlownikPolonijny.Web.Models;
using SlownikPolonijny.Dal;

namespace SlownikPolonijny.Web.Controllers
{
    public class HomeController : Controller
    {
        readonly ILogger<HomeController> _logger;
        readonly IRepository _repo;

        public HomeController(ILogger<HomeController> logger, IRepository repository)
        {
            _logger = logger;
            _repo = repository;
        }

        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("haslo/{name}")]
        public IActionResult Entry(string name)
        {
            ViewData["Link"] = $"haslo/{name}";
            var entries = _repo.GetEntriesByName(name);
            if (entries != null && entries.Count > 0)
            {
                var vm = new EntryViewModel()
                {
                    Name = entries[0].Name,
                    Entries = entries
                };
                ViewData["Title"] = vm.Name;
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
                    Entries = entries
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
                    Entries = entries
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
        public IActionResult Add([FromForm]AddEntryModel model)
        {
            var r = new AddEntryResultModel();
            r.Problems = model.Validate();
            r.Name = model.Name;
            r.Success = false;

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

                    _repo.AddEntry(e);
                    r.Success = true;
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
