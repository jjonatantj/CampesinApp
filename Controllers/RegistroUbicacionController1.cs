using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    public class RegistroUbicacionController1 : Controller
    {
        // GET: RegistroUbicacionController1
        public ActionResult IndexRegistroUbi()
        {
            return View();
        }

        // GET: RegistroUbicacionController1/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: RegistroUbicacionController1/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RegistroUbicacionController1/Create
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

        // GET: RegistroUbicacionController1/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: RegistroUbicacionController1/Edit/5
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

        // GET: RegistroUbicacionController1/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: RegistroUbicacionController1/Delete/5
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
