using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Security.Cryptography;

namespace BinanceLinux
{

    public class Coin
    {
        public int PriceActionID { get; set; }
        public string CoinName { get; set; } = string.Empty;
        public string PrimaryCoin { get; set; } = string.Empty;
        public string SecondaryCoin { get; set; } = string.Empty;
        public float LastSoldPrice { get; set; }
        public float LastBoughtPrice { get; set; }
        public float CurrentPrice { get; set; }

        public bool LastProcessIsSell { get; set; } = false;

        public DateTime? ProcessTime { get; set; }


        public static ObservableCollection<Coin> GetCurrentPrice(string primaryCoin, string secondaryCoin)
        {
            try
            {
                if (string.IsNullOrEmpty(primaryCoin) || string.IsNullOrEmpty(secondaryCoin))
                    return null;


                Variables.Query = "Select * from priceactions where PrimaryCoin=@primaryCoin and secondaryCoin = @secondaryCoin order by ProcessTime desc LIMIT 1";
                MySqlParameter[] parameters = new MySqlParameter[2];
                parameters[0] = new MySqlParameter("@primaryCoin", MySqlDbType.VarChar, 5);
                parameters[1] = new MySqlParameter("@secondaryCoin", MySqlDbType.VarChar, 5);
                parameters[0].Value = primaryCoin;
                parameters[1].Value = secondaryCoin;

                ObservableCollection<Coin> result = Data.Select_Command_Data_With_Parameters(Variables.Query, parameters, reader =>
                {
                    Coin model = new();
                    model.PriceActionID = reader["id"] is DBNull ? 0 : Convert.ToInt32(reader["id"]);
                    model.CoinName = reader["CoinName"] is DBNull ? "" : reader["CoinName"].ToString();
                    model.PrimaryCoin = reader["PrimaryCoin"] is DBNull ? "" : reader["PrimaryCoin"].ToString();
                    model.SecondaryCoin = reader["SecondaryCoin"] is DBNull ? "" : reader["SecondaryCoin"].ToString();
                    model.LastSoldPrice = reader["LastSoldPrice"] is DBNull ? 0 : Convert.ToSingle(reader["LastSoldPrice"]);
                    model.LastBoughtPrice = reader["LastBoughtPrice"] is DBNull ? 0 : Convert.ToSingle(reader["LastBoughtPrice"]);
                    model.CurrentPrice = reader["CurrentPrice"] is DBNull ? 0 : Convert.ToSingle(reader["CurrentPrice"]);
                    model.LastProcessIsSell = reader["LastProcessIsSell"] is DBNull ? false : Convert.ToBoolean(reader["LastProcessIsSell"]);
                    model.ProcessTime = reader["ProcessTime"] is DBNull ? DateTime.Now : Convert.ToDateTime(reader["ProcessTime"]);
                    return model;
                });
                return result;

            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> InsertLastProcess(Coin lastProcess)
        {
            try
            {
                if (lastProcess == null) return false;

                MySqlParameter[] parameters = new MySqlParameter[8];

                parameters[0] = new MySqlParameter("@CoinName", MySqlDbType.VarChar) { Value = lastProcess.CoinName };
                parameters[1] = new MySqlParameter("@PrimaryCoin", MySqlDbType.VarChar) { Value = lastProcess.PrimaryCoin };
                parameters[2] = new MySqlParameter("@SecondaryCoin", MySqlDbType.VarChar) { Value = lastProcess.SecondaryCoin };
                parameters[3] = new MySqlParameter("@LastSoldPrice", MySqlDbType.Float) { Value = lastProcess.LastSoldPrice };
                parameters[4] = new MySqlParameter("@LastBoughtPrice", MySqlDbType.Float) { Value = lastProcess.LastBoughtPrice };
                parameters[5] = new MySqlParameter("@CurrentPrice", MySqlDbType.Float) { Value = lastProcess.CurrentPrice };
                parameters[6] = new MySqlParameter("@LastProcessIsSell", MySqlDbType.Bit) { Value = lastProcess.LastProcessIsSell };
                parameters[7] = new MySqlParameter("@ProcessTime", MySqlDbType.DateTime) { Value = lastProcess.ProcessTime };

                Variables.Result = await Data.ExecuteStoredProc("bpInsertLastProcess", CommandType.StoredProcedure, parameters);
                return Variables.Result;
            }
            catch
            {

                return false;
            }
        }

        public async static Task<string> getApi()
        {
            try
            {
                Variables.Query = "GetApi";
                Variables.ResultString = await Data.ExecuteStoredProcReturnsStringAsync(Variables.Query, CommandType.StoredProcedure, null);
                string api = await Decrypt(Variables.ResultString);
                return api;
            }
            catch
            {

                return string.Empty;
            }
        }
        public async static Task<string> getApis()
        {
            try
            {
                Variables.Query = "GetApis";
                Variables.ResultString = await Data.ExecuteStoredProcReturnsStringAsync(Variables.Query, CommandType.StoredProcedure, null);
                string apis = await Decrypt(Variables.ResultString);
                return apis;
            }
            catch
            {
                return string.Empty;
            }
        }
        private async static Task<string> getKey()
        {
            try
            {
                Variables.Query = "GetKey";
                Variables.ResultString = await Data.ExecuteStoredProcReturnsStringAsync(Variables.Query, CommandType.StoredProcedure, null);
                return Variables.ResultString;
            }
            catch
            {

                return string.Empty;
            }
        }
        public async static Task<string> Decrypt(string encryptedText)
        {
            string[] parts = encryptedText.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid encrypted text format.");

            byte[] combinedBytes = Convert.FromBase64String(parts[0]);
            byte[] keyBytes = Convert.FromBase64String(parts[1]);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyBytes;

                // Extract IV from combinedBytes
                byte[] ivBytes = new byte[aesAlg.BlockSize / 8];
                byte[] encryptedBytes = new byte[combinedBytes.Length - ivBytes.Length];
                Array.Copy(combinedBytes, 0, ivBytes, 0, ivBytes.Length);
                Array.Copy(combinedBytes, ivBytes.Length, encryptedBytes, 0, encryptedBytes.Length);

                aesAlg.IV = ivBytes;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

    }
}
