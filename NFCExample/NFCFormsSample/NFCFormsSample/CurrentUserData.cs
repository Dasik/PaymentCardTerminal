using System;
using System.IO;
using System.Text;
using RestSharp;
using Xamarin.Forms;

namespace NFCFormsSample
{
    public static class CurrentUserData
    {
        /// <summary>
        /// логин пользователя
        /// </summary>
        public static string UserName { get; set; }
        /// <summary>
        /// Хеш пароля пользователя. Следует присваивать сам пароль, а не его хеш, т.к. свойство само расчитает его хеш. 
        /// </summary>
        public static string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = CreateSHA1(value); }
        }
        private static string _passwordHash = "";


        /// <summary>
        /// Возвращает id водителя, если он объявлен, иначе -1
        /// </summary>
        public static int DriverId { get; set; }

        /// <summary>
        /// Возвращает id маршрута, если он объявлен, иначе -1
        /// </summary>
        public static int BusId
        {
            //Application.Current.Properties ["id"] = someClass.ID;
            get
            {
                if (Application.Current.Properties.ContainsKey("BusId"))
                {
                    return (int) Application.Current.Properties["BusId"];
                }
                    return -1;
            }
        }

        public static int RouteId
        {
            get {
                if (Application.Current.Properties.ContainsKey("RouteId"))
                {
                    return (int)Application.Current.Properties["RouteId"];
                }
                    return -1;
            }
        }

        static CurrentUserData()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string filename = Path.Combine(path, "SystemSettings.dat");
            //using (var streamWriter = new StreamWriter(filename, true))
            //{
            //    streamWriter.WriteLine($"insert into events(payment_time,bus_id,coordinates,card_id) " +
            //                           $"values (sysdate,{CurrentUserData.BusId},'{_currentLongitude} x {_currentLatitude}','{tagID}');\n");
            //}

            //File.WriteAllText(filename, SimpleJson.SerializeObject(new BusRouteIdsClassForDeserealize() { BusID = 5, RouteId = 5 }));

            if (!File.Exists(filename))
            {
                ShowError("Ошибка. Возможно файл настроек программы поврежден. Обратитесь к админимтратору");
                return;
            }
            var jsonFile= File.ReadAllText(filename);
            try
            {
                var result = RestSharp.SimpleJson.DeserializeObject<BusRouteIdsClassForDeserealize>(jsonFile);
                Application.Current.Properties["BusId"] = result.BusID;
                Application.Current.Properties["RouteId"] = result.RouteId;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        public static async void ShowError(string message)
        {
            await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Program error", message, "OK");
            Application.Current.Properties["BusId"] = 1;
            Application.Current.Properties["RouteId"] = 1;
        }

        public static string CreateSHA1(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        private class BusRouteIdsClassForDeserealize
        {
            public int BusID { get; set; }
            public int RouteId { get; set; }
        }
    }

}
