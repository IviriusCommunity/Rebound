using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rebound.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SimpleGeneratorTest()
        {
            // Create the 'input' compilation that the generator will act on
            Compilation inputCompilation = CreateCompilation(@"
namespace Rebound.Run
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
    }
}
");

            // Directly create an instance of the source generator (not the attribute)
            Rebound.Generators.ReboundAppSourceGenerator generator = new Rebound.Generators.ReboundAppSourceGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            // Assert there are no errors in the diagnostics
            Assert.IsTrue(diagnostics.IsEmpty);

            // Assert the correct number of syntax trees (original + generated)
            Assert.AreEqual(2, outputCompilation.SyntaxTrees.Count());

            // Get the results of the generation
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // Get the generated tree from the run result
            var generatedSource = runResult.Results[0].GeneratedSources[0].SourceText.ToString();

            // Define the expected result string (what you expect the generated code to look like)
            string expectedGeneratedCode = @"
using Rebound.Helpers.Services;
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Windows.UI.StartScreen;
using Windows.Foundation;
using Windows.ApplicationModel;

namespace Rebound.Run
{
    partial class App : Application
    {
        public App()
        {
            SingleInstanceAppService.Launched += SingleInstanceApp_Launched;
            Current.UnhandledException += App_UnhandledException;

            // Get the app's jump list.
            var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();

            // Disable the system-managed jump list group.
            jumpList.SystemGroupKind = Windows.UI.StartScreen.JumpListSystemGroupKind.None;

            // Remove any previously added custom jump list items.
            jumpList.Items.Clear();

            var item = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(""legacy"", ""Legacy run"");
            item.Logo = new Uri(""ms-appx:///Assets/Computer disk.png"");

            jumpList.Items.Add(item);

            // Save the changes to the app's jump list.
            await jumpList.SaveAsync();
        }

        private SingleInstanceAppService SingleInstanceAppService { get; set; } = new SingleInstanceAppService(""Rebound.Run"");

        protected override void OnLaunched(LaunchActivatedEventArgs args) => _singleInstanceApp.Launch(args.Arguments);
    }
}
";

            // Compare the generated code with the expected result
            Assert.AreEqual(expectedGeneratedCode.Trim(), generatedSource.Trim());
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}