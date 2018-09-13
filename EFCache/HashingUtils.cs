using System.Data.HashFunction.xxHash;
using System.Text;

namespace EFCache
{
    public static class HashingUtils
    {
        private static readonly IxxHash Instance = xxHashFactory.Instance.Create();

        /// <summary>
        ///     Compute xxhash for string and return hex value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ComputeHash(string value)
        {
            return Instance.ComputeHash(Encoding.UTF8.GetBytes(value)).AsHexString();
        }
    }
}
