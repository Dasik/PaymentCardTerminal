using System;
using Xamarin.Forms;

namespace NFCFormsSample
{
	public partial class LoginPagePopUp : ContentView
    {
        public LoginPagePopUp()
        {
            InitializeComponent();
        }

        private void LoginEntry_OnCompleted(object sender, EventArgs e)
        {
            PasswordEntry.Focus();
        }

        private async void Button_OnClicked(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(LoginEntry.Text) ||
                String.IsNullOrEmpty(PasswordEntry.Text))
            {
                await Application.Current.MainPage.DisplayAlert("Внимание", "Поля не могут быть пустыми", "OK");
                return;
            }
            CurrentUserData.UserName = LoginEntry.Text;
            CurrentUserData.PasswordHash = PasswordEntry.Text;
            CurrentUserData.DriverId = await RestService.LoginUser(CurrentUserData.UserName, CurrentUserData.PasswordHash);
            if (CurrentUserData.DriverId == -1)
                return;
            if (!await RestService.SendCarAssign())
                return;
            await Navigation.PopModalAsync(true);
        }

        private void LoginEntry_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            LoginEntry.Text = LoginEntry.Text.Replace(" ","");
        }
    }
}
