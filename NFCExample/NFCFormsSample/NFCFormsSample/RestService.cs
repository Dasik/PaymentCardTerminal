using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;

namespace NFCFormsSample
{
    public static class RestService
    {
        private static readonly RestClient _client = new RestClient("http://192.168.0.90:8080");
        private static CsrfToken<Object> tokenID = new CsrfToken<Object>() { x_CSRF_TOKEN = "A", x_CSRF_HEADER = "B", x_CSRF_PARAM = "C" };

        static RestService()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string filename = Path.Combine(path, "URLSettings.dat");
            //using (var streamWriter = new StreamWriter(filename, true))
            //{
            //    streamWriter.WriteLine($"insert into events(payment_time,bus_id,coordinates,card_id) " +
            //                           $"values (sysdate,{CurrentUserData.BusId},'{_currentLongitude} x {_currentLatitude}','{tagID}');\n");
            //}

            //File.WriteAllText(filename, SimpleJson.SerializeObject(new BusRouteIdsClassForDeserealize() { BusID = 5, RouteId = 5 }));

            if (!File.Exists(filename))
            {
                Debug.WriteLine("URLSettings File Not Exists");
                return;
            }
            var URLSettings = File.ReadAllText(filename);
            _client = new RestClient(URLSettings);
        }
        /// <summary>
        /// Выполняет попытку залогинить водителя.
        /// </summary>
        /// <param name="username">логин пользователя</param>
        /// <param name="password"> пароль</param>
        /// <returns>Возвращает id пользователя в случае успешного логина, иначе -1</returns>
        public static async Task<long> LoginDriver(string username, string password)
        {
            tokenID = await _getCSRFToken();
            _client.Authenticator = new HttpBasicAuthenticator(username, password);
            var request = new RestRequest("/API/driverLogin", Method.GET);
            //request.AddHeader("Accept", "application/json");
            //request.RequestFormat = DataFormat.Json;
            if (tokenID != null)
                request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            //request.AddJsonBody(new
            //{
            //    username = username,
            //    passHash = password
            ////});
            var response = await _client.ExecuteTaskAsync<CsrfToken<long>>(request);

            //var response = await _client.ExecuteTaskAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("LoginError", response.StatusCode.ToString(), "OK");
                return -1;
            }
            UpdateCsrfToken(response.Data);
            return response.Data.RequestBody;
            //return Convert.ToInt64(response.Content);
        }

        /// <summary>
        /// Передает серверу значения связи водитель-маршрут-автобус
        /// </summary>
        /// <returns>true в случае успешной передачи, иначе false</returns>
        public static async Task<bool> SendCarAssign()
        {
            var request = new RestRequest("API/carAssignment", Method.POST);
            request.RequestFormat = DataFormat.Json; 
            if (tokenID != null)
                request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            request.AddJsonBody(new
            {
                BusId = CurrentUserData.BusId,
                DriverId = CurrentUserData.DriverId,
                RouteId = CurrentUserData.RouteId
            });

            var response = await _client.ExecuteTaskAsync<CsrfToken<object>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "OK");
                return false;
            }
            UpdateCsrfToken(response.Data);
            return true;
        }

        /// <summary>
        /// Передает серверу о сведение о новой поездке
        /// </summary>
        /// <param name="coordinates">Координаты терминала</param>
        /// <param name="TagID">ид карты</param>
        /// <returns>null в случае успеха, иначе сообщение об ошибке</returns>
        public static async Task<string> SendNewEvent(double longitude, double latitude, long TagID)
        {
            var request = new RestRequest("API/newEvent/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            if (tokenID != null)
                request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            request.AddJsonBody(new
            {
                BusId = CurrentUserData.BusId,
                Longitude = longitude,
                Latitude = latitude,
                TagID = TagID
            });
            var response = await _client.ExecuteTaskAsync<CsrfToken<string>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                string responceCodeDescription;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        responceCodeDescription = "Карта не зарегистрирована в системе";
                        UpdateCsrfToken(response.Data);
                        break;
                    case HttpStatusCode.PaymentRequired:
                        UpdateCsrfToken(response.Data);
                        responceCodeDescription = "Недостаточно средств";
                        break;
                    default:
                        responceCodeDescription = "Возникла ошибка. Повторите ошибку позднее.";
                        break;
                }
                //await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.ErrorMessage, "OK");
                return responceCodeDescription;
            }
            UpdateCsrfToken(response.Data);
            return null;
        }

        /// <summary>
        /// Получает с сервера список заблокированных карт
        /// </summary>
        public static async Task<List<long>> GetBlockedCardsList()
        {
            var request = new RestRequest("API/getBlockedCards/", Method.GET);

            var response = await _client.ExecuteTaskAsync<CsrfToken<List<long>>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "OK");
                return new List<long>(); 
            }
            if (response.Data.RequestBody == null)
                response.Data.RequestBody = new List<long>();
            UpdateCsrfToken(response.Data);
            return response.Data.RequestBody;
        }

        private static async Task<CsrfToken<Object>> _getCSRFToken()
        {
            var request = new RestRequest("API/driverLogin/csrf-token", Method.GET);
            var response = await _client.ExecuteGetTaskAsync<CsrfToken<Object>>(request);
            //TODO: remove this when server is complete
            //response.ErrorMessage = rand.Next(0, 2)==1 ? "" : "Login or password is incorrect";
            //response.Content = "{id: 42,token=dsjksdkfdjdfnjkdfnj}";
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            CookieContainer _cookieJar = new CookieContainer();
            var sessionCookie = response.Cookies.SingleOrDefault(x => x.Name == "JSESSIONID");
            if (sessionCookie != null)
            {
                _cookieJar.Add(new Cookie(sessionCookie.Name, sessionCookie.Value, sessionCookie.Path, sessionCookie.Domain));
            }
            _client.CookieContainer = _cookieJar;
            return response.Data;
        }

        private static void UpdateCsrfToken<T>(CsrfToken<T> token)
        {
            tokenID.x_CSRF_TOKEN = token.x_CSRF_TOKEN;
            tokenID.x_CSRF_HEADER = token.x_CSRF_HEADER;
            tokenID.x_CSRF_PARAM = token.x_CSRF_PARAM;
        }
        //static RestService()
        //{
        //}

        //public static async Task<int> LoginUser(string username, string passwordHash)
        //{

        //}



        ///// <summary>
        ///// Передает серверу значения связи водитель-маршрут-автобус
        ///// </summary>
        ///// <returns>true в случае успешной передачи, иначе false</returns>
        //public static async Task<bool> SendCarAssign()
        //{
        //    return false;
        //}

        ///// <summary>
        ///// Передает серверу о сведение о новой поездке
        ///// </summary>
        ///// <param name="coordinates">Координаты терминала</param>
        ///// <param name="TagID">ид карты</param>
        ///// <returns>null в случае успеха, иначе сообщение об ошибке</returns>
        //public static async Task<string> SendNewEvent(double longitude, double latitude, string TagID)
        //{
        //    return "FUCK";
        //}

        ///// <summary>
        ///// Получает с сервера список заблокированных карт
        ///// </summary>
        //public static async Task<List<string>> GetBlockedCardsList()
        //{
        //    return new List<string>() { "Fuck" };
        //}

        //private static async Task<string> _getCSRFToken()
        //{
        //    Uri address = new Uri(baseUrl + "/API/driverLogin/csrf-token");
        //    HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
        //    request.Method = "GET";
        //    request.Timeout = 10000;
        //    //string requestBody="huy";

        //    //byte[] byteData = UTF8Encoding.UTF8.GetBytes(requestBody);
        //    //request.ContentLength = byteData.Length;
        //    //using (Stream requestStream = request.GetRequestStream())
        //    //{
        //    //requestStream.Write(byteData, 0, byteData.Length);
        //    //}
        //    using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
        //    {
        //        if (response.StatusCode == HttpStatusCode.OK)
        //        {

        //            Console.WriteLine("Fuckyeah");
        //            using (var reader = new StreamReader(response.GetResponseStream()))
        //            {
        //                return reader.ReadToEnd();
        //            }
        //        }
        //    }
        //    return "";
        //}

        private class CsrfToken<T>
        {
            public string x_CSRF_HEADER { get; set; }
            public string x_CSRF_PARAM { get; set; }
            public string x_CSRF_TOKEN { get; set; }
            public T RequestBody { get; set; }
        }
    }
}
