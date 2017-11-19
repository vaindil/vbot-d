using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using VainBotDiscord.Classes;

namespace VainBotDiscord
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
    }
}
