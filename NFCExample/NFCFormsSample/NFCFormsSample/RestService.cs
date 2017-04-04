using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace NFCFormsSample
{
    public static class RestService
    {
        private static Random rand=new Random(); //TODO: remove this when server is complete
        private static readonly RestClient _client = new RestClient("http://exampleDomainOfServer.com");
        private static string tokenID;
        /// <summary>
        /// Выполняет попытку залогинить водителя.
        /// </summary>
        /// <param name="username">логин пользователя</param>
        /// <param name="passwordHash"> MD5 хэш сумма пароля</param>
        /// <returns>Возвращает id пользователя в случае успешного логина, иначе -1</returns>
        public static async Task<int> LoginUser(string username, string passwordHash)
        {
            var request = new RestRequest("carAssignment/", Method.POST);
            request.AddParameter("username", username);
            request.AddParameter("passwordHash", passwordHash);
            var response = await _client.ExecuteTaskAsync<LoginUserResult>(request);
            //TODO: remove this when server is complete
            response.ErrorMessage = rand.Next(0, 2)==1 ? "" : "Login or password is incorrect";
            response.Content = "{id: 42,token=dsjksdkfdjdfnjkdfnj}";
            if (!String.IsNullOrEmpty(response.ErrorMessage))
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("LoginError", response.ErrorMessage, "OK");
                return -1;
            }
            tokenID = response.Data.token;
            return response.Data.id;
        }

        private class LoginUserResult
        {
            public int id { get; set; }
            public string token { get; set; }
        }

        /// <summary>
        /// Передает серверу значения связи водитель-маршрут-автобус
        /// </summary>
        /// <returns>true в случае успешной передачи, иначе false</returns>
        public static async Task<bool> SendCarAssign()
        {
            var request = new RestRequest("carAssignment/", Method.POST);
            request.AddParameter("BusId", CurrentUserData.BusId);
            request.AddParameter("DriverId", CurrentUserData.DriverId);
            request.AddParameter("RouteId", CurrentUserData.RouteId);
            request.AddParameter("tokenId", tokenID);

            var response = await _client.ExecuteTaskAsync(request);
            //TODO: remove this when server is complete
            response.ErrorMessage = rand.Next(0, 2) == 1 ? "" : "Unknown error";
            if (!String.IsNullOrEmpty(response.ErrorMessage))
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.ErrorMessage, "OK");
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
        public static async Task<string> SendNewEvent(double longitude,double latitude,string TagID)
        {
            var request = new RestRequest("newEvent/", Method.POST);
            request.AddParameter("BusId", CurrentUserData.BusId);
            request.AddParameter("longitude", longitude);
            request.AddParameter("latitude", latitude);
            request.AddParameter("TagID", TagID);
            request.AddParameter("tokenId", tokenID);

            var response = await _client.ExecuteTaskAsync(request);
            //TODO: remove this when server is complete
            response.ErrorMessage ="Not enough money";
            if (!String.IsNullOrEmpty(response.ErrorMessage))
            {
                //await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.ErrorMessage, "OK");
                return response.ErrorMessage;
            }
            return null;
        }

        /// <summary>
        /// Получает с сервера список заблокированных карт
        /// </summary>
        public static async Task<List<string>> GetBlockedCardsList()
        {
            var request = new RestRequest("getBlockedCards/", Method.GET);

            var response = await _client.ExecuteTaskAsync<List<string>>(request);
            //TODO: remove this when server is complete
            response.ErrorMessage = rand.Next(0, 2) == 1 ? "" : "Unknown error";
            if (!String.IsNullOrEmpty(response.ErrorMessage))
            {
                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Error", response.ErrorMessage, "OK");
                return null;
            }
            return response.Data;
        }

    }
}
