using Microsoft.VisualStudio.TestTools.UnitTesting;
using LinuxCore.Controllers;
using System.Linq;

namespace LinuxCoreTests
{
    [TestClass]
    public class ValuesControllerTests
    {
        private ValuesController mTarget;

        [TestInitialize]
        public void Initialise()
        {
            mTarget = new ValuesController();
        }

        [TestMethod]
        public void TestGetReturnsList()
        {
            var result = mTarget.Get().Value.ToArray();

            Assert.AreEqual(result.Length, 2);
            Assert.AreEqual(result[0], "wsvalues1");
            Assert.AreEqual(result[1], "wsvalues1");
        }
    }
}
