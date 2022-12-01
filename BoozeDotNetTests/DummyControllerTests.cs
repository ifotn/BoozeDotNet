using BoozeDotNet.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BoozeDotNetTests
{
    [TestClass]
    public class DummyControllerTests
    {
        [TestMethod]
        public void IndexLoadsView()
        {
            // arrange
            var controller = new DummyController();

            // act
            var result = (ViewResult)controller.Index();

            // assert
            Assert.AreEqual("Index", result.ViewName);
        }
    }
}