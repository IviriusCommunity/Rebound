using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Pages.ControlPanel;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WindowsTools : Page
{
    public class ProgramItem()
    {
        public string Name
        {
            get; set;
        }
        public Visibility AdminVisibility
        {
            get; set;
        }
        public Visibility TagVisibility
        {
            get; set;
        }
        public string SpecialTag
        {
            get; set;
        }
        public string Path
        {
            get; set;
        }
        public string Icon
        {
            get; set;
        }
        public bool IsEnabled
        {
            get; set;
        }
    }

    public List<ProgramItem> items = new()
    {
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/imageres_15.png",
            Name = "Calculator",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "PRODUCTIVITY",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/imageres_15.png",
            Name = "Character Map",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "PRODUCTIVITY",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/imageres_15.png",
            Name = "Clock",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "PRODUCTIVITY",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/imageres_5323.ico",
            Name = "Command Prompt",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "COMMAND LINE",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/imageres_15.png",
            Name = "Component Services",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/CompManagement.png",
            Name = "Computer Management",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/rcontrol.ico",
            Name = "Control Panel",
            Path = @"C:\Rebound11\rcontrol.exe",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/rdfrgui.ico",
            Name = "Defragment and Optimize Drives",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "MAINTENANCE",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/imageres_15.png",
            Name = "Dev Home (Preview)",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "DEVELOPMENT",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/cleanmgr.ico",
            Name = "Disk Cleanup",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "MAINTENANCE",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/EventViewer.png",
            Name = "Event Viewer",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/HyperV.png",
            Name = "Hyper-V Manager",
            AdminVisibility = Visibility.Visible,
            SpecialTag = "VIRTUALIZATION",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/HyperVQC.png",
            Name = "Hyper-V Quick Create",
            AdminVisibility = Visibility.Visible,
            SpecialTag = "VIRTUALIZATION",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/imageres_25.ico",
            Name = "iSCSI Initiator",
            AdminVisibility = Visibility.Visible,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "/Assets/AppIcons/LocalSecPolicy.png",
            Name = "Local Security Policy",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
    };

    public WindowsTools()
    {
        this.InitializeComponent();
        ItemsGrid.ItemsSource = items;
    }
}
