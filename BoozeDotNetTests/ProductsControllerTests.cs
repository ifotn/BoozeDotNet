using BoozeDotNet.Controllers;
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
        [TestMethod]
        public void IndexLoadsView()
        {
            // arrange
            // must instantiate in memory db to pass as a dependency when creating an instance of ProductsController
            var controller = new ProductsController();

            // act

            // assert
        }
    }
}
