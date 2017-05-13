using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Xamarin.Forms;

namespace NFCCardRegistration
{
    public partial class LoginPagePopUp : ContentView
    {
        public LoginPagePopUp()
        {
            InitializeComponent();
        }

        private void LoginEntry_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            LoginEntry.Text = LoginEntry.Text.Replace(" ", "");
        }

        private void LoginEntry_OnCompleted(object sender, EventArgs e)
        {
            PasswordEntry.Focus();
        }

        private async void AuthorizeButton_OnClicked(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(LoginEntry.Text) ||
                String.IsNullOrEmpty(PasswordEntry.Text))
            {
                await Application.Current.MainPage.DisplayAlert("Внимание", "Поля не могут быть пустыми", "OK");
                return;
            }
            try
            {
                AuthorizeButton.IsEnabled = false;
                long result;
                if ((result=await RestService.Login(LoginEntry.Text,
                                                    PasswordEntry.Text)) == 1)
                    await Navigation.PopModalAsync(true);
                else if (result==-1)
                    await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Ошибка", "Неверный логин или пароль", "OK");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            finally
            {
                AuthorizeButton.IsEnabled = true;
            }

        }
    }
}
