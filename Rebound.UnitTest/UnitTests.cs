using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Rebound.Helpers.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // directly create an instance of the generator
            Rebound.Helpers.Generators.ReboundAppAttribute generator = new ReboundAppAttribute("Rebound.Run", "Legacy run");

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            // Assert there are no errors in the diagnostics
            Debug.Assert(diagnostics.IsEmpty);
            Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2); // two syntax trees: original + generated

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

namespace Rebound.Helpers
{
    partial class App
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
            Debug.Assert(generatedSource.Trim() == expectedGeneratedCode.Trim(), "Generated code does not match expected result.");
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}