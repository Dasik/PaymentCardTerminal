using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Xamarin.Forms;

namespace NFCFormsSample
{
	public partial class LoginPagePopUp : ContentView
    {
        ObservableCollection<KeyValuePair<long,string>> _busesList;
        public ObservableCollection<KeyValuePair<long, string>> BusesList
        {
            get { return _busesList; }
            set
            {
                _busesList = value;
                OnPropertyChanged("BusesList");
            }
        }
        ObservableCollection<KeyValuePair<long, string>> _routesList;
        public ObservableCollection<KeyValuePair<long, string>> RoutesList
        {
            get { return _routesList; }
            set
            {
                _routesList = value;
                OnPropertyChanged("RoutesList");
            }
        }
        public LoginPagePopUp()
        {
            InitializeComponent();
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
                CurrentUserData.UserName = LoginEntry.Text;
                CurrentUserData.Password = PasswordEntry.Text;
                CurrentUserData.DriverId = await RestService.LoginDriver(CurrentUserData.UserName,
                                                                            CurrentUserData.Password);
                if (CurrentUserData.DriverId <= 0) //TODO: Добавить форму для пароль или логин неверен
                {
                    return;
                }
                var routes=await RestService.GetRoutesOfDriver();
                var buses = await RestService.GetBusesOfDriver();

                RoutesList = new ObservableCollection<KeyValuePair<long, string>>(routes.ToList());
                BusesList = new ObservableCollection<KeyValuePair<long, string>>(buses.ToList());
                if (RoutesList.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Внимание", "Не обнаружено маршрутов, которые зарегистрированы за вашим работадателем. Обратитесь к администратору.", "OK");
                }
                if (BusesList.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Внимание", "Не обнаружено автобусов, которые зарегистрированы за вашим работадателем. Обратитесь к администратору.", "OK");
                }
                RoutesPicker.SelectedIndex = -1;
                BusesPicker.SelectedIndex = -1;
                RoutesPicker.ItemsSource = RoutesList;
                BusesPicker.ItemsSource = BusesList;
                string routeValue;
                if (routes.TryGetValue(CurrentUserData.RouteId, out routeValue))
                    //RoutesPicker.SelectedItem = new KeyValuePair<long, string>(CurrentUserData.RouteId,routeValue);
                    RoutesPicker.SelectedIndex =
                        RoutesList.IndexOf(new KeyValuePair<long, string>(CurrentUserData.RouteId, routeValue));
                string busValue;
                if (buses.TryGetValue(CurrentUserData.BusId, out busValue))
                    //BusesPicker.SelectedItem = new KeyValuePair<long, string>(CurrentUserData.BusId, busValue);
                    BusesPicker.SelectedIndex =
                        BusesList.IndexOf(new KeyValuePair<long, string>(CurrentUserData.BusId, busValue));
                ChooseRouteLabel.IsVisible = true;
                RoutesPicker.IsVisible = true;
                ChooseBusLabel.IsVisible = true;
                BusesPicker.IsVisible = true;
                OKButton.IsVisible = true;
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

        private void LoginEntry_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            LoginEntry.Text = LoginEntry.Text.Replace(" ","");
        }

        private async void OKButton_OnClicked(object sender, EventArgs e)
        {
            OKButton.IsEnabled = false;
            try
            {
                if (RoutesPicker.SelectedIndex == -1)
                {
                    await Application.Current.MainPage.DisplayAlert("Внимание", "Не выбран маршрут", "OK");
                    return;
                }
                if (BusesPicker.SelectedIndex == -1)
                {
                    await Application.Current.MainPage.DisplayAlert("Внимание", "Не выбран автобус", "OK");
                    return;
                }
                try
                {
                    CurrentUserData.RouteId = RoutesList[RoutesPicker.SelectedIndex].Key;
                    CurrentUserData.BusId = BusesList[BusesPicker.SelectedIndex].Key;
                    CurrentUserData.SaveBusRoute();
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }

                if (!await RestService.SendCarAssign())
                {
                    return;
                }
                await Navigation.PopModalAsync(true);
            }
            finally
            {
                OKButton.IsEnabled = true;
            }
            
        }
    }
}
