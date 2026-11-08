using System.Windows;
using System.Windows.Controls;
namespace DiskShift;
public sealed class UpdateWindow : Window
{
    public UpdateWindow(Window owner, UpdateResult result)
    {
        Owner = owner; Title = "Updates · DYE.DiskShift"; Width = 580; Height = 340; MinWidth = 460; MinHeight = 300; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (System.Windows.Media.Brush)FindResource("Panel"); FontFamily = new("Segoe UI");
        var body = new StackPanel { Margin = new Thickness(24) };
        body.Children.Add(new TextBlock { Text = result.Url == null ? "Updateprüfung" : "Neue Version verfügbar", FontSize = 23, Margin = new Thickness(0,0,0,16) });
        body.Children.Add(new TextBlock { Text = result.Message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,18) });
        body.Children.Add(new TextBlock { Text = "Installiert: " + Updates.Current + " · Kein automatischer Download oder Austausch.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,18) });
        var actions = new WrapPanel(); var open = new Button { Content = "Releases öffnen" };
        open.Click += (_,_) => { try { Updates.Open(result.Url ?? Updates.Repository + "/releases"); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } }; actions.Children.Add(open);
        var close = new Button { Content = "Schließen", IsCancel = true, Margin = new Thickness(8,0,0,0) }; close.Click += (_,_) => Close(); actions.Children.Add(close); body.Children.Add(actions); Content = body;
    }
}
