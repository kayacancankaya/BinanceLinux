using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceLinux
{
    public class Variables
    {
        public static string Query { get; set; } = string.Empty;

        public static string ConnectionString { get; private set; } = "Server=127.0.0.1;Port=3306;Database=binance;Uid=root;Pwd=My,3654778;";

        public static bool Result { get; set; } = false;
        public static string ResultString { get; set; } = string.Empty;
    }
}
