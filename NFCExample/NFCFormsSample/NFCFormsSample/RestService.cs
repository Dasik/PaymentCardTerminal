using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace NFCFormsSample
{
    public static class RestService
    {
        private const string baseUrl = "http://192.168.0.90:8080";
        private static Random rand = new Random(); //TODO: remove this when server is complete
        private static readonly RestClient _client = new RestClient("http://192.168.0.90:8080");
        private static CSRFToken tokenID;
        /// <summary>
        /// Выполняет попытку залогинить водителя.
        /// </summary>
        /// <param name="username">логин пользователя</param>
        /// <param name="passwordHash"> sha1 хэш сумма пароля</param>
        /// <returns>Возвращает id пользователя в случае успешного логина, иначе -1</returns>
        public static async Task<int> LoginDriver(string username, string passwordHash)
        {
            tokenID = await _getCSRFToken();
            var request = new RestRequest("/API/driverLogin");
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            request.AddJsonBody(new
            {
                username = username,
                passHash = passwordHash
            });
            var response = await _client.ExecutePostTaskAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("LoginError", response.StatusCode.ToString(), "OK");
                return -1;
            }
            return Convert.ToInt32(response.Content);
        }

        /// <summary>
        /// Передает серверу значения связи водитель-маршрут-автобус
        /// </summary>
        /// <returns>true в случае успешной передачи, иначе false</returns>
        public static async Task<bool> SendCarAssign()
        {
            var request = new RestRequest("API/carAssignment/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            request.AddJsonBody(new
            {
                BusId = CurrentUserData.BusId,
                DriverId = CurrentUserData.DriverId,
                RouteId = CurrentUserData.RouteId
            });

            var response = await _client.ExecuteTaskAsync(request);
            //TODO: remove this when server is complete
            //response.ErrorMessage = rand.Next(0, 2) == 1 ? "" : "Unknown error";
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "OK");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Передает серверу о сведение о новой поездке
        /// </summary>
        /// <param name="coordinates">Координаты терминала</param>
        /// <param name="TagID">ид карты</param>
        /// <returns>null в случае успеха, иначе сообщение об ошибке</returns>
        public static async Task<string> SendNewEvent(double longitude, double latitude, string TagID)
        {
            var request = new RestRequest("API/newEvent/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            request.AddJsonBody(new
            {
                BusId = CurrentUserData.BusId,
                Longitude = longitude,
                Latitude = latitude,
                TagID = TagID
            });
            var response = await _client.ExecuteTaskAsync(request);
            //TODO: remove this when server is complete
            //response.ErrorMessage ="Not enough money";
            if (response.StatusCode != HttpStatusCode.OK)
            {
                //await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.ErrorMessage, "OK");
                return response.ErrorMessage;
            }
            return null;
        }

        /// <summary>
        /// Получает с сервера список заблокированных карт
        /// </summary>
        public static async Task<List<string>> GetBlockedCardsList()//TODO:Сделать на сервере
        {
            var request = new RestRequest("API/getBlockedCards/", Method.GET);

            var response = await _client.ExecuteTaskAsync<List<string>>(request);
            //TODO: remove this when server is complete
            //response.ErrorMessage = rand.Next(0, 2) == 1 ? "" : "Unknown error";
            response.Data=new List<string>();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "OK");
                return null;
            }
            return response.Data;
        }

        private static async Task<CSRFToken> _getCSRFToken()
        {
            var request = new RestRequest("API/driverLogin/csrf-token", Method.GET);
            var response = await _client.ExecuteGetTaskAsync<CSRFToken>(request);
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

        private class CSRFToken
        {
            public string x_CSRF_HEADER { get; set; }
            public string x_CSRF_PARAM { get; set; }
            public string x_CSRF_TOKEN { get; set; }
        }
    }
}
