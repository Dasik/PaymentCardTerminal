using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Java.Math;
using RestSharp;
using RestSharp.Authenticators;

namespace NFCCardRegistration
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

            if (!File.Exists(filename))
            {
                Debug.WriteLine("URLSettings File Not Exists. Creating new");
                File.WriteAllText(filename, "http://192.168.0.90:8080");
            }
            var URLSettings = File.ReadAllText(filename);
            _client = new RestClient(URLSettings);
        }
        /// <summary>
        /// Выполняет попытку залогинить водителя.
        /// </summary>
        /// <param name="username">логин пользователя</param>
        /// <param name="password"> пароль</param>
        /// <returns>Возвращает 1 в случае успеха, -1 в случае провала и 0 если произошла непредвиденная ошибка</returns>
        public static async Task<long> Login(string username, string password)
        {
            tokenID = await _getCSRFToken();
            _client.Authenticator = new HttpBasicAuthenticator(username, password);
            var request = new RestRequest("/API/Login", Method.GET);
            if (tokenID != null)
                request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            var response = await _client.ExecuteTaskAsync<CsrfToken<long>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return -1;
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("LoginError",
                        response.StatusCode.ToString(), "OK");
                return 0;
            }
            UpdateCsrfToken(response.Data);
            if (response.Data.RequestBody == -1)
                return 1;
            return -1;
        }

        /// <summary>
        /// Передает серверу комманду заегестрировать новую карту
        /// </summary>
        /// <param name="TagID">ид карты</param>
        /// <param name="balance">изначальный баланс карты</param>
        /// <param name="cardType">тип карты</param>
        /// <returns>true в случае успеха, иначе false</returns>
        public static async Task<bool> SendNewEvent(long cardKey, String balance, String cardType = "limited")
        {
            var request = new RestRequest("/API/admin/card", Method.POST);
            request.RequestFormat = DataFormat.Json;
            if (tokenID != null)
                request.AddParameter(tokenID.x_CSRF_PARAM, tokenID.x_CSRF_TOKEN, ParameterType.QueryString);
            request.AddJsonBody(new
            {
                CardKey = cardKey,
                Balance = balance,
                CardType = cardType,
            });
            var response = await _client.ExecuteTaskAsync<CsrfToken<string>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("SendNewEvent",
                        response.StatusCode.ToString(), "OK");
                return false;
            }
            UpdateCsrfToken(response.Data);
            return true;
        }

        private static async Task<CsrfToken<Object>> _getCSRFToken()
        {
            var request = new RestRequest("API/Login/csrf-token", Method.GET);
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


        private class CsrfToken<T>
        {
            public string x_CSRF_HEADER { get; set; }
            public string x_CSRF_PARAM { get; set; }
            public string x_CSRF_TOKEN { get; set; }
            public T RequestBody { get; set; }
        }
    }
}
