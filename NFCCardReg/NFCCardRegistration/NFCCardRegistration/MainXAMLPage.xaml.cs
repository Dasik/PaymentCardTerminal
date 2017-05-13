using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Android.Media;
using Java.Math;
using Java.Text;
using NFCCardRegistration.Droid;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Poz1.NFCForms.Abstract;
using Xamarin.Forms;

namespace NFCCardRegistration
{
    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainXAMLPage : ContentPage
    {
        private readonly INfcForms device;
        private long CurrentKey = -1;

        public MainXAMLPage()
        {
            device = DependencyService.Get<INfcForms>();
            device.NewTag += HandleNewTag;
            InitializeComponent();

            ShowLoginPagePopUp();
        }

        async void HandleNewTag(object sender, NfcFormsTag tag)
        {
            Debug.WriteLine($"HandleNewTag");
            try
            {
                var bytesArray = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < tag.Id.Length; i++)
                    bytesArray[i] = tag.Id[i];
                long tagID = BitConverter.ToInt64(bytesArray, 0);
                Debug.WriteLine($"tagID={tagID}");
                KeyLabel.Text = tagID.ToString();
                CurrentKey = tagID;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex);
            }
        }

        private async void ShowLoginPagePopUp()
        {
            ContentPage contentPage = new ContentPage
            {
                Padding = new Thickness(10, 50, 10, 50),
                Content = new LoginPagePopUp()
            };
            await Navigation.PushModalAsync(contentPage, true);
        }

        private async void SendButton_OnClicked(object sender, EventArgs e)
        {
            try
            {
                SendButton.IsEnabled = false;
                if (CurrentKey == -1)
                    await DisplayAlert("Ошибка", "Необходимо считать карту", "OK");
                await RestService.SendNewEvent(CurrentKey, BalanceEntry.Text);
            }
            catch (Exception exception)
            {
                await DisplayAlert("Ошибка", exception.Message, "OK");
                Debug.WriteLine(exception);
            }
            finally
            {
                SendButton.IsEnabled = true;
            }
        }

        private void BalanceEntry_OnCompleted(object sender, EventArgs e)
        {
            if (BalanceEntry.Text == "")
                BalanceEntry.Text = "0";
            else
                SendButton_OnClicked(sender, e);
        }
    }
}
