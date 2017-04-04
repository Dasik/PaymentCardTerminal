using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using Poz1.NFCForms.Abstract;
using Poz1.NFCForms.Droid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Util;
using System.Threading.Tasks;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.Locations;
using Android.Widget;

namespace NFCFormsSample.Droid
{
    [Activity(Label = "NFCFormsSample", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity,
                                GoogleApiClient.IConnectionCallbacks,
                                GoogleApiClient.IOnConnectionFailedListener
    {
        public static MainActivity Instance;
        public NfcAdapter NFCdevice;
        public NfcForms x;

        protected GoogleApiClient mGoogleApiClient;
        public Location mLastLocation;

        public delegate void onUpdateLocationDelegate(Location location);
        public event onUpdateLocationDelegate onUpdateLocationEvent;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            BuildGoogleApiClient();


            NfcManager NfcManager = (NfcManager)Android.App.Application.Context.GetSystemService(Context.NfcService);
            NFCdevice = NfcManager.DefaultAdapter;

            Xamarin.Forms.DependencyService.Register<INfcForms, NfcForms>();
            x = Xamarin.Forms.DependencyService.Get<INfcForms>() as NfcForms;
            Instance = this;
            LoadApplication(new NFCFormsSample.App());
        }

        protected void BuildGoogleApiClient()
        {
            mGoogleApiClient = new GoogleApiClient.Builder(this)
                .AddConnectionCallbacks(this)
                .AddOnConnectionFailedListener(this)
                .AddApi(LocationServices.API)
                .Build();
        }

        protected override void OnStart()
        {
            base.OnStart();
            mGoogleApiClient.Connect();
        }

        protected override void OnStop()
        {
            base.OnStop();
            CloseGoogleApiClient();
        }

        public void CloseGoogleApiClient()
        {
            if (mGoogleApiClient.IsConnected)
            {
                mGoogleApiClient.Disconnect();
            }
        }

        public void OnConnected(Bundle connectionHint)
        {
            mLastLocation = LocationServices.FusedLocationApi.GetLastLocation(mGoogleApiClient);
            onUpdateLocationEvent?.Invoke(mLastLocation);
        }

        public void OnConnectionSuspended(int cause)
        {
            Log.Info("TAG", "Connection suspended");
            mGoogleApiClient.Connect();
        }

        public void OnConnectionFailed(Android.Gms.Common.ConnectionResult result)
        {
            Log.Info("TAG", "Connection failed: ConnectionResult.getErrorCode() = " + result.ErrorCode);
        }


        protected override void OnResume()
        {
            base.OnResume();
            if (NFCdevice != null)
            {
                var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);
                NFCdevice.EnableForegroundDispatch
                (
                    this,
                    PendingIntent.GetActivity(this, 0, intent, 0),
                    //new[] { new IntentFilter(NfcAdapter.ActionTechDiscovered) },
                    //new String[][] {new string[] {
                    //        NFCTechs.Ndef,
                    //    },
                    //    new string[] {
                    //        NFCTechs.MifareClassic,
                    //    },
                    //}
                    null,
                    null
                );
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            NFCdevice.DisableForegroundDispatch(this);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            x.OnNewIntent(this, intent);
        }
    }
}

