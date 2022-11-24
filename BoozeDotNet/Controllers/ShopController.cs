using BoozeDotNet.Data;
using BoozeDotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Configuration;

namespace BoozeDotNet.Controllers
{
    public class ShopController : Controller
    {
        // manually add db connection dependency (auto-generated in scaffolded controllers but this is custom)
        private readonly ApplicationDbContext _context;

        // add Configuration dependency so we can read the Stripe API key from appsettings or the Azure Config section
        private readonly IConfiguration _configuration;

        public ShopController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

        // GET: /Shop/Checkout => show empty checkout page to capture customer info
        [Authorize]
        public IActionResult Checkout()
        {
            return View();
        }

        // POST: /Shop/Checkout => create Order object and store as session var before payment
        [HttpPost]
        [Authorize]
        public IActionResult Checkout([Bind("FirstName,LastName,Address,City,Province,PostalCode,Phone")] Order order)
        {
            // 7 fields bound from form inputs in method header
            // now auto-fill 3 of the fields we removed from the form
            order.OrderDate = DateTime.Now;
            order.CustomerId = User.Identity.Name;

            order.OrderTotal = (from c in _context.CartItems
                                where c.CustomerId == HttpContext.Session.GetString("CustomerId")
                                select c.Quantity * c.Price).Sum();

            // store the order as session var so we can proceed to payment attempt
            HttpContext.Session.SetObject("Order", order);

            // redirect to payment
            return RedirectToAction("Payment");
        }

        // GET: /Shop/Payment => invoke Stripe payment session which displays their payment form
        [Authorize]
        public IActionResult Payment()
        {
            // get the order from the session var
            var order = HttpContext.Session.GetObject<Order>("Order");

            // get the api key from the site config
            StripeConfiguration.ApiKey = _configuration.GetValue<string>("Stripe:SecretKey");

            // stripe invocation from https://stripe.com/docs/checkout/quickstart?client=html
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long?)(order.OrderTotal * 100), // total must be in cents, not dollars and cents
                        Currency = "cad",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "BoozeDotNet Purchase"
                        },
                    },
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = "https://" + Request.Host + "/Shop/SaveOrder",
                CancelUrl = "https://" + Request.Host + "/Shop/Cart"
            };
            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // GET: /Shop/SaveOrder => create Order in DB, add OrderDetails, clear cart
        [Authorize]
        public IActionResult SaveOrder()
        {
            // get the order from session var
            var order = HttpContext.Session.GetObject<Order>("Order");

            // fill required PaymentCode temporarily
            order.PaymentCode = HttpContext.Session.GetString("CustomerId");

            // save new order to db
            _context.Add(order);
            _context.SaveChanges();

            // save each CartItem as a new OrderDetails record for this order
            var cartItems = _context.CartItems.Where(c => c.CustomerId == HttpContext.Session.GetString("CustomerId"));

            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    Quantity = item.Quantity,
                    ProductId = item.ProductId,
                    Price = item.Price,
                    OrderId = order.OrderId
                };
                _context.Add(orderDetail);
            }
            _context.SaveChanges();

            // empty cart
            foreach (var item in cartItems)
            {
                _context.Remove(item);
            }
            _context.SaveChanges();

            // clear session vars
            HttpContext.Session.SetInt32("ItemCount", 0);
            HttpContext.Session.SetString("CustomerId", "");
            HttpContext.Session.SetObject("Order", null);

            // redirect to Order Confirmation
            return RedirectToAction("Details", "Orders", new { @id = order.OrderId });
        }
    }
}
