using BoozeDotNet.Data;
using BoozeDotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // GET: /products/AddToCart/401 => id param represents selected ProductId
        public IActionResult AddToCart(int id)
        {
            var product = _context.Product.Find(id);

            // check if this cart already contains this product
            var cartItem = _context.CartItems.SingleOrDefault(c => c.ProductId == id && c.CustomerId == GetCustomerId());

            if (cartItem == null)
            {
                // create new CartItem and populate the fields
                cartItem = new CartItem
                {
                    ProductId = id,
                    Quantity = 1,
                    Price = (decimal)product.Price,
                    CustomerId = GetCustomerId()
                };

                _context.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += 1;
                _context.Update(cartItem);
            }
            
            _context.SaveChanges();

            return RedirectToAction("Cart");
        }

        private string GetCustomerId()
        {
            // check if we already have a session var for CustomerId
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                // create new session var of type string using a Guid
                HttpContext.Session.SetString("CustomerId", Guid.NewGuid().ToString());
            }

            return HttpContext.Session.GetString("CustomerId");
        }

        // GET: /Shop/Cart => display current user's shopping cart
        public IActionResult Cart()
        {
            // identify which cart to display
            var customerId = GetCustomerId();

            // join to parent object so we can also show the Product details
            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToList();

            // calc cart total for display
            var total = (from c in cartItems
                         select c.Quantity * c.Price).Sum();
            ViewData["Total"] = total;

            // calc and store cart quantity total in a session var for display in navbar
            var itemCount = (from c in cartItems
                             select c.Quantity).Sum();
            HttpContext.Session.SetInt32("ItemCount", itemCount);

            return View(cartItems);
        }
    }
}
