using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Apollo.Models;
using Apollo.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apollo.Entities;

namespace Apollo.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IEnumerable<Book> books = await _context.Books
            .Where(b => b.OwnerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        int countOfAvailableBooks = books.Count(b => b.IsBorrowed != true);

        ViewBag.countOfAvailableBooks = countOfAvailableBooks;
        ViewBag.countOfLoanedBooks = books.Count(b => b.IsBorrowed == true);
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
