using Microsoft.UI.Xaml.Controls;
using Rebound.Modding.Instructions;
using Rebound.ViewModels;

namespace Rebound.Views;

public partial class Rebound11Page : Page
{
    public ReboundViewModel ReboundViewModel { get; set; } = new();

    public Rebound11Page()
    {
        InitializeComponent();
    }
}