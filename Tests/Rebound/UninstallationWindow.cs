using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rebound;
using System.Threading.Tasks;

namespace Rebound.Tests.Rebound
{
    [TestClass]
    public class UninstallationWindowTests
    {
        private UninstallationWindow _uninstallationWindow;

        [TestInitialize]
        public void Setup()
        {
            _uninstallationWindow = new UninstallationWindow();
        }

        [TestMethod]
        public async Task Test_UninstallAppPackage()
        {
            await _uninstallationWindow.UninstallAppPackage("packageFamilyName", "displayAppName", "lnkFile", "lnkDestination", "lnkDisplayName");
            Assert.AreEqual(3, _uninstallationWindow.currentStep, "currentStep should be 3 after UninstallAppPackage is called.");
        }

        [TestMethod]
        public async Task Test_DeleteShortcut()
        {
            await _uninstallationWindow.DeleteShortcut("displayAppName", "lnkDestination", "lnkDisplayName");
            Assert.AreEqual(1, _uninstallationWindow.currentStep, "currentStep should be 1 after DeleteShortcut is called.");
        }

        [TestMethod]
        public async Task Test_ReplaceShortcut()
        {
            await _uninstallationWindow.ReplaceShortcut("displayAppName", "lnkFile", "lnkDestination", "lnkDisplayName");
            Assert.AreEqual(1, _uninstallationWindow.currentStep, "currentStep should be 1 after ReplaceShortcut is called.");
        }
    }
}