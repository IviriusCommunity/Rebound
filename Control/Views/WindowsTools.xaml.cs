using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Control.Views;
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
            Icon = "ms-appx:///AppRT/Exported/imageres_15.png",
            Name = "Calculator",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "PRODUCTIVITY",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/imageres_15.png",
            Name = "Character Map",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "PRODUCTIVITY",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/imageres_15.png",
            Name = "Clock",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "PRODUCTIVITY",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/imageres_5323.ico",
            Name = "Command Prompt",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "COMMAND LINE",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/imageres_15.png",
            Name = "Component Services",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/CompManagement.png",
            Name = "Computer Management",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/rcontrol.ico",
            Name = "Control Panel",
            Path = @"C:\Rebound11\rcontrol.exe",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/rdfrgui.ico",
            Name = "Defragment and Optimize Drives",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "MAINTENANCE",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/imageres_15.png",
            Name = "Dev Home (Preview)",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "DEVELOPMENT",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/cleanmgr.ico",
            Name = "Disk Cleanup",
            IsEnabled = true,
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "MAINTENANCE",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/EventViewer.png",
            Name = "Event Viewer",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/HyperV.png",
            Name = "Hyper-V Manager",
            AdminVisibility = Visibility.Visible,
            SpecialTag = "VIRTUALIZATION",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/HyperVQC.png",
            Name = "Hyper-V Quick Create",
            AdminVisibility = Visibility.Visible,
            SpecialTag = "VIRTUALIZATION",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/imageres_25.ico",
            Name = "iSCSI Initiator",
            AdminVisibility = Visibility.Visible,
            SpecialTag = "SYSTEM",
        },
        new ProgramItem()
        {
            Icon = "ms-appx:///AppRT/Exported/LocalSecPolicy.png",
            Name = "Local Security Policy",
            AdminVisibility = Visibility.Collapsed,
            SpecialTag = "SYSTEM",
        },
    };

    public WindowsTools()
    {
        this.InitializeComponent();
        App.ControlPanelWindow?.TitleBarEx.SetWindowIcon("AppRT\\Exported\\imageres_114.ico");
        if (App.ControlPanelWindow is not null)
        {
            App.ControlPanelWindow.Title = "Windows Tools";
        }
        ItemsGrid.ItemsSource = items;
    }
}
