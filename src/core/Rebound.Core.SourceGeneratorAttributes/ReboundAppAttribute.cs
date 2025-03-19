using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Core.SourceGeneratorAttributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReboundAppAttribute : Attribute
{
    public string SingleProcessTaskName { get; }
    public string LegacyLaunchCommandTitle { get; }

    public ReboundAppAttribute(string singleProcessTaskName, string legacyLaunchCommandTitle)
    {
        SingleProcessTaskName = singleProcessTaskName;
        LegacyLaunchCommandTitle = legacyLaunchCommandTitle;
    }
}

