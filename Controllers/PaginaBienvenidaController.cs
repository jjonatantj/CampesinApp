using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    public class PaginaBienvenidaController : Controller
    {
        // GET: PaginaBienvenidaController
        public ActionResult Bienvenida()
        {
            return View();
        }

        // GET: PaginaBienvenidaController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PaginaBienvenidaController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PaginaBienvenidaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PaginaBienvenidaController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PaginaBienvenidaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PaginaBienvenidaController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PaginaBienvenidaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
