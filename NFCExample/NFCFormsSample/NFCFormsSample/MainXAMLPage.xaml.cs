using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Android.Media;
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
        /// Время, на которое карта заносится в локальный бан терминала в секундах
        /// </summary>
        private const int TimeOfBan = 7;
        /// <summary>
        /// Время обновления списка забаненных uid карточек в милисекундах 
        /// </summary>
        private const int BanTimerUpdate = 7 * 1000;
        /// <summary>
        /// Время сброса значения индикатора терминала в милисекундах 
        /// </summary>
        private const int ResultBoxResetTime = 13 * 1000;
        /// <summary>
        /// Время обновления списка заблокированных карт в милисекундах 
        /// </summary>
        private const int BlackListUpdateTime = 10 * 60 * 1000;
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
        public Dictionary<long, DateTime> CardsBufferBan = new Dictionary<long, DateTime>();
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
        private List<long> _blackList = new List<long>();
        /// <summary>
        /// Текущее состояние ResultBox'а
        /// </summary>
        private ResultStatesEnum _currentResultState=ResultStatesEnum.Waiting;

        public MainXAMLPage()
        {
            device = DependencyService.Get<INfcForms>();
            device.NewTag += HandleNewTag;
            InitializeComponent();
            BanTimerTask = new Timer(BanTimerUpdate);
            BanTimerTask.Elapsed += (sender, e) =>
            {
                Debug.WriteLine($" BanTimerTask.Elapsed; CardsBufferBan.Count={CardsBufferBan.Count}");
                try
                {
                    var cardsToRemove = CardsBufferBan.Where(t => _checkTimeValue(t.Value, TimeOfBan))
                                     .Select(pair => pair.Key)
                                     .ToList();
                    //var cardsToRemove = CardsBufferBan.Where(t => DateTime.Now.Second - t.Value.Second >= TimeOfBan)
                    //                                    .Select(pair => pair.Key)
                    //                                    .ToList();
                    foreach (var item in cardsToRemove)
                    {
                        CardsBufferBan.Remove(item);
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }

            };
            BanTimerTask.Start();
            _resultBoxResetTimerTask = new Timer(ResultBoxResetTime);
            _resultBoxResetTimerTask.Elapsed += (sender, e) =>
            {
                Debug.WriteLine($" _resultBoxResetTimerTask.Elapsed");
                try
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        ChangeResultBoxState(ResultStatesEnum.Waiting);
                    });
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            };
            _resultBoxResetTimerTask.Start();
            _blackListUpdateTimerTask = new Timer(BlackListUpdateTime);
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
                    Debug.WriteLine($"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
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
            try
            {
                Task.Run(async () =>
                {
                    Debug.WriteLine($"Waiting setup DriverId");
                    while (CurrentUserData.DriverId == -1)
                        await Task.Delay(1000);
                    Debug.WriteLine($"DriverId={CurrentUserData.DriverId}");
                    _blackList = await RestService.GetBlockedCardsList();
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            _blackListUpdateTimerTask.Elapsed += async (sender, e) =>
            {
                Debug.WriteLine($"_blackListUpdateTimerTask.Elapsed");
                try
                {
                    _blackList = await RestService.GetBlockedCardsList();
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            };
            _blackListUpdateTimerTask.Start();
            ChangeResultBoxState(ResultStatesEnum.Waiting);
        }

        async void HandleNewTag(object sender, NfcFormsTag tag)
        {
            if (_currentResultState==ResultStatesEnum.Working)
                return;
            Debug.WriteLine($"HandleNewTag");
            _resultBoxResetTimerTask.Stop();
            try
            {
                ChangeResultBoxState(ResultStatesEnum.Working);
                var bytesArray = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < tag.Id.Length; i++)
                    bytesArray[i] = tag.Id[i];
                long tagID = BitConverter.ToInt64(bytesArray, 0);
                Debug.WriteLine($"tagID={tagID}");
                //foreach (var item in tag.Id)
                //{
                //    tagID += item.ToString("X2") /*+ ":"*/;
                //}
                if (_blackList.Contains(tagID))
                {
                    Debug.WriteLine($"Card in blacklist");
                    ChangeResultBoxState(ResultStatesEnum.Blocked);
                    return;
                }
                if (CardsBufferBan.ContainsKey(tagID))
                {
                    if (_checkTimeValue(CardsBufferBan[tagID], TimeOfBan))
                    {
                        Debug.WriteLine($"Card in CardsBufferBan. Removing");
                        CardsBufferBan.Remove(tagID);
                    }
                    else
                    {
                        Debug.WriteLine($"Card in CardsBufferBan. Updating time");
                        CardsBufferBan[tagID] = DateTime.Now;
                        ChangeResultBoxState(ResultStatesEnum.Banned);
                        return;
                    }
                }
                string resultMessage;
                if ((resultMessage = await RestService.SendNewEvent(_currentLongitude, _currentLatitude, tagID)) != null)
                {
                    Debug.WriteLine($"Not enought money");
                    ChangeResultBoxState(ResultStatesEnum.NotEnoughMoney, resultMessage);
                    return;
                }
                Debug.WriteLine($"Success transaction");
                CardsBufferBan.Add(tagID, DateTime.Now);
                ChangeResultBoxState(ResultStatesEnum.CanGo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex);
            }
            finally
            {
                _resultBoxResetTimerTask.Start();
            }
        }

        /// <summary>
        /// Проверяет, прошло ли с item момента времени time секунд 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private bool _checkTimeValue(DateTime item, int time)
        {
            //return DateTime.Now.Minute + DateTime.Now.Hour * 60 - item.Hour * 60 - item.Minute >= time;//в минутах
            return DateTime.Now.Second + DateTime.Now.Minute * 60 - item.Second - item.Minute * 60 >= time;//в секундах
        }

        private void onLoginButton_Clicled(object sender, EventArgs e)
        {
            Debug.WriteLine($"onLoginButton_Clicled");
            try
            {
                ShowLoginPagePopUp();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
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

        private ToneGenerator toneGen1;
        /// <summary>
        /// Меняет текущий статус окна результата и при неободимости издает звуковой сигнал
        /// </summary>
        /// <param name="state"></param>
        /// <param name="message"></param>
        private void ChangeResultBoxState(ResultStatesEnum state, string message = "")
        {
            Debug.WriteLine($"ChangeResultBoxState; state={state},message={message}");
            //toneGen1.StartTone(Tone.PropNack, 500);
            //toneGen1.StartTone(Tone.PropPrompt, 500);
            try
            {
                if (toneGen1 == null)
                    toneGen1 = new ToneGenerator(Stream.Notification, 100);
                else
                {
                    toneGen1.Release();
                    toneGen1 = new ToneGenerator(Stream.Notification, 100);
                }
                _currentResultState = state;
                switch (state)
                {
                    case ResultStatesEnum.CanGo:
                        resultBox.Color = Color.Green;
                        resultLabel.Text = "Можете идти";
                        toneGen1.StartTone(Tone.PropPrompt, 500);
                        break;
                    case ResultStatesEnum.Banned:
                        resultBox.Color = Color.Yellow;
                        resultLabel.Text = "Вы уже прикладывали карту";
                        toneGen1.StartTone(Tone.PropNack, 500);
                        break;
                    case ResultStatesEnum.Blocked:
                        resultBox.Color = Color.Red;
                        resultLabel.Text = "Ваша карта была заблокирована";
                        toneGen1.StartTone(Tone.PropNack, 500);
                        break;
                    case ResultStatesEnum.NotEnoughMoney:
                        resultBox.Color = Color.Purple;
                        resultLabel.Text = "На Вашей карте недостаточно средств";
                        toneGen1.StartTone(Tone.PropNack, 500);
                        break;
                    case ResultStatesEnum.Waiting:
                        resultBox.Color = Color.Aqua;
                        resultLabel.Text = "Приложите карту к терминалу";
                        break;
                    case ResultStatesEnum.Working:
                        resultBox.Color = Color.Thistle;
                        resultLabel.Text = "Подождите. Выполняется обработка.";
                        break;

                    default:
                        resultBox.Color = Color.Aqua;
                        resultLabel.Text = "Приложите карту к терминалу";
                        toneGen1.StartTone(Tone.PropPrompt, 500);
                        break;
                }
                if (!String.IsNullOrEmpty(message))
                    resultLabel.Text = message;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

        }



        public enum ResultStatesEnum
        {
            CanGo = 0,
            Blocked,
            Banned,
            NotEnoughMoney,
            Waiting,
            Working
        }
    }
}
