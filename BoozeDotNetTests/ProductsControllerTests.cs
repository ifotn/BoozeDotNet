using BoozeDotNet.Controllers;
using BoozeDotNet.Data;
using BoozeDotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoozeDotNetTests
{
    [TestClass]
    public class ProductsControllerTests
    {
        // db var at class level for use in all tests
        private ApplicationDbContext context;
        ProductsController controller;

        // set up code that runs automatically before each unit test
        [TestInitialize]
        public void TestInitialize()
        {
            // must instantiate in memory db to pass as a dependency when creating an instance of ProductsController
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            context = new ApplicationDbContext(options);

            // seed the db before passing it to controller
            var category = new Category { CategoryId = 389, Name = "Some Category " };
            context.Add(category);

            for (var i = 100; i < 111; i++)
            {
                var product = new Product { ProductId = i, Name = "Product " + i.ToString(), CategoryId = 389, Category = category, Price = i + 10 };
                context.Add(product);
            }

            var extraProduct = new Product { ProductId = 300, Name = "ABC Product", CategoryId = 389, Category = category, Price = 38 };
            context.Add(extraProduct);
            context.SaveChanges();

            controller = new ProductsController(context);
        }

        #region "Index"
        [TestMethod]
        public void IndexLoadsView()
        {
            // arrange
            // now all done in TestInitialize

            // act
            var result = (ViewResult)controller.Index().Result;

            // assert
            Assert.AreEqual("Index", result.ViewName);
        }

        [TestMethod]
        public void IndexLoadsProducts()
        {
            // act
            var result = (ViewResult)controller.Index().Result;
            List<Product> model = (List<Product>)result.Model;

            // assert
            CollectionAssert.AreEqual(context.Product.OrderBy(p => p.Name).ToList(), model);
        }
        #endregion

        #region "Details"
        [TestMethod]
        public void DetailsNoIdLoads404()
        {
            // act
            var result = (ViewResult)controller.Details(null).Result;

            // assert 
            Assert.AreEqual("404", result.ViewName);
        }

        [TestMethod]
        public void DetailsNoProductsTableLoads404()
        {
            // arrange
            context.Product = null;

            // act
            var result = (ViewResult)controller.Details(null).Result;

            // assert 
            Assert.AreEqual("404", result.ViewName);
        }

        [TestMethod]
        public void DetailsInvalidIdLoads404()
        {
            // act
            var result = (ViewResult)controller.Details(23).Result;

            // assert 
            Assert.AreEqual("404", result.ViewName);
        }

        [TestMethod]
        public void DetailsValidIdLoadsView()
        {
            // act
            var result = (ViewResult)controller.Details(104).Result;

            // assert 
            Assert.AreEqual("Details", result.ViewName);
        }

        [TestMethod]
        public void DetailsValidIdLoadsProduct()
        {
            // act
            var result = (ViewResult)controller.Details(104).Result;

            // assert 
            Assert.AreEqual(context.Product.Find(104), result.Model);
        }
        #endregion
    }
}
