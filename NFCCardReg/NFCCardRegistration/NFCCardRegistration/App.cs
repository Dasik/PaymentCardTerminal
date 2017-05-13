using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace NFCCardRegistration
{
	public class App : Application
	{
		public App ()
		{
            // The root page of your application
            //MainPage = new NavigationPage(new NFCPage());
            MainPage = new NavigationPage(new MainXAMLPage());
        }

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
