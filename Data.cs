using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace BinanceLinux
{
    public class Data
    {

        public static ObservableCollection<T> Select_Command_Data_With_Parameters<T>(string query, MySqlParameter[] parameters, Func<MySqlDataReader, T> mapFunction)
        {

            try
            {
                using (MySqlConnection connection = new MySqlConnection(Variables.ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {

                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        using (MySqlDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            ObservableCollection<T> result = new ObservableCollection<T>();

                            while (reader.Read())
                            {
                                T item = mapFunction(reader);
                                result.Add(item);
                            }

                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        public static ObservableCollection<T> Select_Command_Data<T>(string query, Func<MySqlDataReader, T> mapFunction)
        {

            try
            {
                using (MySqlConnection connection = new MySqlConnection(Variables.ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            ObservableCollection<T> result = new ObservableCollection<T>();

                            while (reader.Read())
                            {
                                T item = mapFunction(reader);
                                result.Add(item);
                            }

                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static async Task<bool> ExecuteStoredProc(string storedProcedureNameorQuery, CommandType commandType, MySqlParameter[] parameters = null)
        {
            try
            {
                using (var connection = new MySqlConnection(Variables.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(storedProcedureNameorQuery, connection))
                    {
                        command.CommandType = commandType;
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing stored procedure or query: {ex.Message}");
                return false;

            }
        }
        public static async Task<string> ExecuteStoredProcReturnsStringAsync(string storedProcedureNameorQuery, CommandType commandType, MySqlParameter[] parameters = null)
        {
            try
            {
                using (var connection = new MySqlConnection(Variables.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(storedProcedureNameorQuery, connection))
                    {
                        command.CommandType = commandType;
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        object result = await command.ExecuteScalarAsync();
                        return result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing stored procedure or query: {ex.Message}");
                return string.Empty;

            }
        }
    }
}
