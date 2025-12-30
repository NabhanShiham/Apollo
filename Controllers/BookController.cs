using Apollo.Data;
using Apollo.Entities;
using Apollo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Apollo.Controllers
{
    [Authorize]
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BookController> _logger;

        public BookController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<BookController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
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

        // GET: Books/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == userId);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: /Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] BookViewModel model, IFormFile? photoFile, IFormFile? bookFile)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    throw new Exception("User not found!");
                }

                var book = new Book
                {
                    Name = model.Name,
                    Description = model.Description,
                    OwnerId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                if (photoFile != null && photoFile.Length > 0)
                {
                    book.PhotoPath = await SaveFile(photoFile, "thumbnails");
                }

                if (bookFile != null && bookFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".epub", ".pdf" };
                    var extension = Path.GetExtension(bookFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["ErrorMessage"] = "Only EPUB and PDF files are allowed.";
                        return RedirectToAction(nameof(Index));
                    }

                    book.FilePath = await SaveFileWithOriginalName(bookFile, "books");
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Book '{book.Name}' added successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Please correct the errors below.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Books/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Book book, IFormFile? PhotoFile, IFormFile? BookFile)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            var existingBook = await _context.Books.FindAsync(id);
            if (existingBook == null)
            {
                return NotFound();
            }

            existingBook.Name = book.Name;
            existingBook.Description = book.Description;
            existingBook.UpdatedAt = DateTime.UtcNow;

            // Update photo if provided
            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingBook.PhotoPath))
                {
                    DeleteFile(existingBook.PhotoPath);
                }
                existingBook.PhotoPath = await SaveFile(PhotoFile, "thumbnails");
            }

            // Update book file if provided
            if (BookFile != null && BookFile.Length > 0)
            {
                var allowedExtensions = new[] { ".epub", ".pdf" };
                var extension = Path.GetExtension(BookFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Only EPUB and PDF files are allowed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (!string.IsNullOrEmpty(existingBook.FilePath))
                {
                    DeleteFile(existingBook.FilePath);
                }
                existingBook.FilePath = await SaveFileWithOriginalName(BookFile, "books");
            }

            try
            {
                _context.Update(existingBook);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Book updated successfully!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(book.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Details), new { id });
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

            if (!string.IsNullOrEmpty(book.PhotoPath))
            {
                DeleteFile(book.PhotoPath);
            }

            if (!string.IsNullOrEmpty(book.FilePath))
            {
                DeleteFile(book.FilePath);
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

        // GET: /Books/Search
        [HttpGet]
        public async Task<IActionResult> Search(string query, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }

            var cleanedQuery = query.Trim();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pageSize = 10;

            ViewBag.CurrentFilter = cleanedQuery;

            var books = await _context.Books
                .Where(b => b.Name.Contains(cleanedQuery) && b.OwnerId == userId)
                .OrderBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalItems = await _context.Books
                .Where(b => b.Name.Contains(cleanedQuery) && b.OwnerId == userId)
                .CountAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View("Index", books);
        }

        // GET: Books/GetEpubUrl/{id} - Returns the direct static file URL for EPUB
        [HttpGet("Books/GetEpubUrl/{id}")]
        public async Task<IActionResult> GetEpubUrl(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == userId);

            if (book == null || string.IsNullOrEmpty(book.FilePath))
            {
                return NotFound();
            }

            var epubUrl = $"/{book.FilePath}";
            return Ok(new { url = epubUrl });
        }

        // GET: Books/Download/{id}
        [HttpGet("Book/Download/{id}")]
        public async Task<IActionResult> Download(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == userId);

            if (book == null || string.IsNullOrEmpty(book.FilePath))
            {
                return NotFound();
            }

            var filePath = Path.Combine(_environment.WebRootPath, book.FilePath);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileInfo = new FileInfo(filePath);
            var contentType = GetContentType(fileInfo.Extension) ?? "application/octet-stream";
            var fileName = $"{book.Name}{fileInfo.Extension}";

            return PhysicalFile(filePath, contentType, fileName);
        }

        // Helper Methods
        private string? GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".epub" => "application/epub+zip",
                _ => "application/octet-stream"
            };
        }

        private async Task<string> SaveFile(IFormFile file, string subfolder)
        {
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", subfolder);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine("uploads", subfolder, uniqueFileName).Replace("\\", "/");
        }

        private async Task<string> SaveFileWithOriginalName(IFormFile file, string subfolder)
        {
            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var safeName = $"{Guid.NewGuid().ToString().Substring(0, 8)}_{originalName}{extension}";

            safeName = safeName.Replace(" ", "_").Replace("(", "").Replace(")", "");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", subfolder);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fullPath = Path.Combine(uploadsFolder, safeName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine("uploads", subfolder, safeName).Replace("\\", "/");
        }

        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    _logger.LogInformation($"File deleted: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
            }
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}