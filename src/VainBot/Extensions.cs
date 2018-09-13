using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using VainBot.Classes;

namespace VainBot
{
    public static class Extensions
    {
        /// <summary>
        /// Gets the value of KeyValue for the specified key.
        /// </summary>
        /// <param name="dbSetKv">KeyValue DbSet</param>
        /// <param name="key">Key for which to get the value</param>
        /// <returns>Value for the specified key</returns>
        public static async Task<string> GetValueAsync(this DbSet<KeyValue> dbSetKv, string key)
        {
            return (await dbSetKv.FindAsync(key)).Value;
        }

        // https://stackoverflow.com/a/20175
        public static string ToOrdinal(this long num)
        {
            if (num <= 0)
                return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
            }
        }

        public static string ToOrdinal(this int num)
        {
            return ToOrdinal((long)num);
        }
    }
}
