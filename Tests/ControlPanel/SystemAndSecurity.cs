using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rebound.Rebound.Pages.ControlPanel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml.Controls;
using System.Management;

namespace Rebound.Tests.ControlPanel
{
    [TestClass]
    public class SystemAndSecurityTests
    {
        private SystemAndSecurity _systemAndSecurity;

        [TestInitialize]
        public void Setup()
        {
            _systemAndSecurity = new SystemAndSecurity();
        }

        [TestMethod]
        public void Test_UACStatus()
        {
            int status = SystemAndSecurity.UACStatus();
            Assert.IsTrue(status >= -1000 && status <= 3, "UACStatus should return a value between -1000 and 3.");
        }

        [TestMethod]
        public void Test_DecodeProductState()
        {
            string state = _systemAndSecurity.DecodeProductState(0x10000);
            Assert.AreEqual("B", state, "DecodeProductState should correctly decode the product state.");
        }
    }
}