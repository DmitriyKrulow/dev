using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace uchet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TagController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tag
        public IActionResult Index()
        {
            var tags = _context.Tags.ToList();
            return View(tags);
        }

        // GET: Tag/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tag/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Tag tag)
        {
            if (ModelState.IsValid)
            {
                _context.Tags.Add(tag);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tag/Edit/5
        public IActionResult Edit(int id)
        {
            var tag = _context.Tags.FirstOrDefault(t => t.Id == id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        // POST: Tag/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Tag tag)
        {
            if (id != tag.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(tag);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // POST: Tag/Delete/5
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var tag = _context.Tags.FirstOrDefault(t => t.Id == id);
            if (tag != null)
            {
                // Проверяем, используется ли бирка в печати
                // Пока просто удаляем, в будущем можно добавить проверку
                _context.Tags.Remove(tag);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Tag/GetTags
        [HttpGet]
        public IActionResult GetTags()
        {
            var tags = _context.Tags.Where(t => t.IsActive).ToList();
            return Json(tags);
        }
    }
}