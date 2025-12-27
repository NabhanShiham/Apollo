using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Apollo.Data;
using Apollo.Entities;
using Apollo.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Apollo.Controllers
{
    [Authorize]
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BookController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Books
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var books = await _context.Books
                .Where(b => b.OwnerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            
            return View(books);
        }

        // POST: /Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] BookViewModel model, IFormFile? photoFile)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var book = new Book
                {
                    Name = model.Name,
                    Description = model.Description,
                    OwnerId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (photoFile != null && photoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "books");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + photoFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }
                    
                    book.PhotoPath = $"/uploads/books/{uniqueFileName}";
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Book '{book.Name}' added successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            TempData["ErrorMessage"] = "Please correct the errors below.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Books/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == userId);
            
            if (book == null)
            {
                return NotFound();
            }

            // Delete photo file if exists
            if (!string.IsNullOrEmpty(book.PhotoPath))
            {
                var filePath = Path.Combine(_environment.WebRootPath, book.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Book '{book.Name}' deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Books/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == userId);
            
            if (book == null)
            {
                return NotFound();
            }

            book.IsBorrowed = !book.IsBorrowed;
            book.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Book '{book.Name}' marked as {(book.IsBorrowed ? "borrowed" : "available")}!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }

            string cleanedQuery = query.Trim();

            ViewBag.CurrentFilter = cleanedQuery;

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            int pageSize = 10;

            List<Book>? results = await _context.Books
                .Where(e => e.Name.Contains(cleanedQuery))
                .Where(e => e.OwnerId == userId)
                .OrderBy(e => e.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Int32 totalItems = await _context.Books
                .Where(e =>
                e.Name.Contains(cleanedQuery))
                .Where(e =>
                e.OwnerId == userId)
                .CountAsync();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.Results = results;
            ViewBag.TotalPages = totalPages;

            return View("Index", results);
        }

    }
}