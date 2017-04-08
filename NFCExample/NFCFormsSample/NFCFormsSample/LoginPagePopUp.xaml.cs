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

        private async void OKButton_OnClicked(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(LoginEntry.Text) ||
                String.IsNullOrEmpty(PasswordEntry.Text))
            {
                await Application.Current.MainPage.DisplayAlert("Внимание", "Поля не могут быть пустыми", "OK");
                return;
            }
            OKButton.IsEnabled = false;
            CurrentUserData.UserName = LoginEntry.Text;
            CurrentUserData.PasswordHash = PasswordEntry.Text;
            CurrentUserData.DriverId = await RestService.LoginDriver(CurrentUserData.UserName, CurrentUserData.PasswordHash);
            if (CurrentUserData.DriverId == -1)//TODO: Добавить форму для пароль или логин неверен
            {
                OKButton.IsEnabled = true;
                return;
            }
            if (!await RestService.SendCarAssign())
            {
                OKButton.IsEnabled = true;
                return;
            }
            OKButton.IsEnabled = true;
            await Navigation.PopModalAsync(true);
        }

        private void LoginEntry_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            LoginEntry.Text = LoginEntry.Text.Replace(" ","");
        }
    }
}
