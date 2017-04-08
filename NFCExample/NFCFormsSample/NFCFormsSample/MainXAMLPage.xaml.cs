using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using NFCFormsSample.Droid;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Poz1.NFCForms.Abstract;
using Xamarin.Forms;

namespace NFCFormsSample
{
    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainXAMLPage : ContentPage
    {
        /// <summary>
        /// Время, на которое карта заносится в локальный бан терминала в минутах
        /// </summary>
        private const int TimeOfBan = 10;//in minutes
        /// <summary>
        /// Время обновления списка забаненных uid карточек в милисекундах 
        /// </summary>
        private const int BanTimerUpdate = 30 * 1000;
        /// <summary>
        /// Время сброса значения индикатора терминала в милисекундах 
        /// </summary>
        private const int ResultBoxResetTime = 5 * 1000;
        /// <summary>
        /// Время обновления списка заблокированных карт в милисекундах 
        /// </summary>
        private const int BlackListUpdateTime = 10*60 * 1000;
        /// <summary>
        /// Таймер для обновления списка забаненных карточек
        /// </summary>
        private Timer BanTimerTask;

        private readonly INfcForms device;
/// <summary>
/// Широта
/// </summary>
        private double _currentLatitude = -1;
        /// <summary>
        /// Долгота
        /// </summary>
        private double _currentLongitude = -1;
        private IGeolocator _locator;
        /// <summary>
        /// Список карт, занесенных в локальный бан
        /// </summary>
        public Dictionary<string, DateTime> CardsBufferBan = new Dictionary<string, DateTime>();
        /// <summary>
        /// Таймер сброса значения индикатора терминала
        /// </summary>
        private readonly Timer _resultBoxResetTimerTask;
        /// <summary>
        /// Таймер обновления списка заблокированных карт
        /// </summary>
        private readonly Timer _blackListUpdateTimerTask;
        /// <summary>
        /// Список пожизненно забаненных карт. Загружается с сервера
        /// </summary>
        private List<string> _blackList=new List<string>();

        public MainXAMLPage()
        {
            device = DependencyService.Get<INfcForms>();
            device.NewTag += HandleNewTag;
            InitializeComponent();
            BanTimerTask = new Timer(BanTimerUpdate);
            BanTimerTask.Elapsed += (sender, e) =>
            {
                var cardsToRemove = CardsBufferBan.Where(t =>_checkTimeValue(t.Value,TimeOfBan))
                                                    .Select(pair => pair.Key)
                                                    .ToList();
                //var cardsToRemove = CardsBufferBan.Where(t => DateTime.Now.Second - t.Value.Second >= TimeOfBan)
                //                                    .Select(pair => pair.Key)
                //                                    .ToList();
                foreach (var item in cardsToRemove)
                {
                    CardsBufferBan.Remove(item);
                }
            };
            BanTimerTask.Start();
            _resultBoxResetTimerTask = new Timer(ResultBoxResetTime);
            _resultBoxResetTimerTask.Elapsed += (sender, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ChangeResultBoxState(ResultStatesEnum.Waiting);
                });
            };
            _resultBoxResetTimerTask.Start();
            _blackListUpdateTimerTask =new Timer(BlackListUpdateTime);
            _blackListUpdateTimerTask.Elapsed += (sender, e) =>
            {
                _blackList = RestService.GetBlockedCardsList().Result;
            };
            _blackListUpdateTimerTask.Start();
            #region LocationSetting
            try
            {
                //RestService.LoginDriver("Nahuy", "Eto");
                throw new NotImplementedException();
                _locator = CrossGeolocator.Current;
                _locator.AllowsBackgroundUpdates = true;
                _locator.DesiredAccuracy = 50;
                Debug.WriteLineIf(!_locator.IsGeolocationAvailable, "!locator.IsGeolocationAvailable");
                Debug.WriteLineIf(!_locator.IsListening, "!locator.IsListening");
                var position = _locator.GetPositionAsync(10000);
                _currentLatitude = position.Result.Latitude;
                _currentLongitude = position.Result.Longitude;
                _locator.PositionChanged += (sender, e) =>
                {
                    _currentLatitude = e.Position.Latitude;
                    _currentLongitude = e.Position.Longitude;
                };
                _locator.PositionError += (o, args) =>
                {
                    Debug.WriteLine($"Error: {args.Error.ToString()}");
                };
                MainActivity.Instance.CloseGoogleApiClient();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                MainActivity.Instance.onUpdateLocationEvent += (location) =>
                {
                    //PaymentTimeLabel.Text = "Trying update";
                    if (location == null)
                    {
                        _currentLatitude = -1;
                        _currentLongitude = -1;
                    }
                    else
                    {
                        _currentLatitude = location.Latitude;
                        _currentLongitude = location.Longitude;
                    }
                };
            }
#endregion

            ShowLoginPagePopUp();
            ChangeResultBoxState(ResultStatesEnum.Waiting);
        }

        async void HandleNewTag(object sender, NfcFormsTag tag)
        {
            _resultBoxResetTimerTask.Stop();
            try
            {
                string tagID = "";
                foreach (var item in tag.Id)
                {
                    tagID += item.ToString("X2") /*+ ":"*/;
                }
                
                if (_blackList.Contains(tagID))
                {
                    ChangeResultBoxState(ResultStatesEnum.Blocked);
                    return;
                }
                if (CardsBufferBan.ContainsKey(tagID))
                {
                    ChangeResultBoxState(ResultStatesEnum.Banned);
                    if (_checkTimeValue(CardsBufferBan[tagID],TimeOfBan))
                        CardsBufferBan.Remove(tagID);
                    else
                    {
                        CardsBufferBan[tagID] = DateTime.Now;
                        return;
                    }
                }
                if (await RestService.SendNewEvent(_currentLongitude,_currentLatitude, tagID) != null)
                {
                    ChangeResultBoxState(ResultStatesEnum.NotEnoughMoney);
                    return;
                }
                CardsBufferBan.Add(tagID, DateTime.Now);
                ChangeResultBoxState(ResultStatesEnum.CanGo);
                //string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                //string filename = Path.Combine(path, "querysLog.txt");
                //using (var streamWriter = new StreamWriter(filename, true))
                //{
                //    streamWriter.WriteLine($"insert into events(payment_time,bus_id,coordinates,card_id) " +
                //                           $"values (sysdate,{CurrentUserData.BusId},'{_currentLongitude} x {_currentLatitude}','{tagID}');\n");
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex);
            }

            _resultBoxResetTimerTask.Start();
        }

        /// <summary>
        /// Проверяет, прошло ли с item момента времени time минут 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private bool _checkTimeValue(DateTime item, int time)
        {
            return DateTime.Now.Minute + DateTime.Now.Hour * 60 - item.Hour * 60 - item.Minute >= time;
        }

        private void onLoginButton_Clicled(object sender, EventArgs e)
        {
            ShowLoginPagePopUp();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="message"></param>
        private void ChangeResultBoxState(ResultStatesEnum state, string message = "")
        {
            switch (state)
            {
                case ResultStatesEnum.CanGo:
                    resultBox.Color = Color.Green;
                    resultLabel.Text = "Можете идти";
                    break;
                case ResultStatesEnum.Banned:
                    resultBox.Color = Color.Yellow;
                    resultLabel.Text = "Вы уже прикладывали карту";
                    break;
                case ResultStatesEnum.Blocked:
                    resultBox.Color = Color.Red;
                    resultLabel.Text = "Ваша карта была заблокирована";
                    break;
                case ResultStatesEnum.NotEnoughMoney:
                    resultBox.Color = Color.Purple;
                    resultLabel.Text = "На Вашей карте недостаточно средств";
                    break;
                case ResultStatesEnum.Waiting:
                    resultBox.Color = Color.Aqua;
                    resultLabel.Text = "Приложите карту к терминалу";
                    break;
                default:
                    resultBox.Color = Color.Aqua;
                    resultLabel.Text = "Приложите карту к терминалу";
                    break;
            }
            if (!String.IsNullOrEmpty(message))
                resultLabel.Text = message;
        }



        public enum ResultStatesEnum
        {
            CanGo = 0,
            Blocked,
            Banned,
            NotEnoughMoney,
            Waiting,
        }
    }
}
