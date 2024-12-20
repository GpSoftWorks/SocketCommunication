
using SocketCommunication.ViewModels;

namespace SocketCommunication.Views;

public partial class HomePageView : ContentPage
{
	public HomePageView(HomePageViewModel _viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel;
	}
}