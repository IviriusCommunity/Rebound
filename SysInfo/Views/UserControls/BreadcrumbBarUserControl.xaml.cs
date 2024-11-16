using System.Collections.ObjectModel;

namespace Rebound.SysInfo.Views;
public sealed partial class BreadcrumbBarUserControl : UserControl
{
    public ObservableCollection<string> BreadcrumbBarCollection;
    public List<string> Items
    {
        get => (List<string>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register("Items", typeof(List<string>), typeof(BreadcrumbBarUserControl), new PropertyMetadata(null));

    public string SingleItem
    {
        get => (string)GetValue(SingleItemProperty);
        set => SetValue(SingleItemProperty, value);
    }

    public static readonly DependencyProperty SingleItemProperty =
        DependencyProperty.Register("SingleItem", typeof(string), typeof(BreadcrumbBarUserControl), new PropertyMetadata(default(string)));

    public BreadcrumbBarUserControl()
    {
        this.InitializeComponent();
        BreadcrumbBarCollection = new ObservableCollection<string>();
        Loaded += BreadcrumbBarUserControl_Loaded;
    }

    private void BreadcrumbBarUserControl_Loaded(object sender, RoutedEventArgs e)
    {
        BreadcrumbBarCollection.Add("Settings");
        if (Items != null)
        {
            foreach (var item in Items)
            {
                BreadcrumbBarCollection.Add(item);
            }
        }
        else
        {
            BreadcrumbBarCollection.Add(SingleItem);
        }
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        var numItemsToGoBack = BreadcrumbBarCollection.Count - args.Index - 1;
        for (var i = 0; i < numItemsToGoBack; i++)
        {
            _ = App.Current.JsonNavigationViewService.GoBack();
        }
    }
}

