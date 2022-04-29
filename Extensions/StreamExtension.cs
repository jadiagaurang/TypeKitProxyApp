using System.IO;

namespace TypeKitProxyApp {
    public static class StreamExtension {
        /// <summary>
        /// Convert Stream to Byte Array
        /// </summary>
        /// <param name="input">Stream</param>
        /// <returns>Byte Array</returns>
        public static byte[] ToByteArray(this Stream input) {
            input.Position = 0; //Move Stream at the begining position
            byte[] buffer = new byte[16 * 1024];

            using (MemoryStream ms = new MemoryStream()) {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0) {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}