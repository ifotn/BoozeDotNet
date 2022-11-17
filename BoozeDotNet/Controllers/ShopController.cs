using BoozeDotNet.Data;
using BoozeDotNet.Models;
using Microsoft.AspNetCore.Mvc;

namespace BoozeDotNet.Controllers
{
    public class ShopController : Controller
    {
        // manually add db connection dependency (auto-generated in scaffolded controllers but this is custom)
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(categories);
        }

        public IActionResult Category(string CategoryName)
        {
            if (CategoryName == null)
            {
                return RedirectToAction("Index");
            }
            
            // pass input param val to ViewData for display in the view
            ViewData["CategoryName"] = CategoryName;

            // fetch products for display
            var products = _context.Product
                .Where(p => p.Category.Name == CategoryName)
                .OrderBy(p => p.Name)
                .ToList();

            return View(products);
        }

        public IActionResult AddToCart(int id)
        {

        }
    }
}
