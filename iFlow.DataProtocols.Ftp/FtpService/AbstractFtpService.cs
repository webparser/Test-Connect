using System;
using System.IO;

namespace iFlow.DataProviders
{
    public abstract class AbstractFtpService
    {
        /// <summary>
        /// Извлечение подстроки yyyyMMdd из имени файла.
        /// </summary>
        /// <param name="fileName">Имя файла в формате Prefix_yyyyMMdd.zip, где Prefix - либо Hourly, либо Msg (на 
        /// момент написания провайдера) - тип запрашиваемых данных.</param>
        /// <returns>Дата файла</returns>
        public static DateTime? ExtractDateFromFileName(string fileName, string prefix)
        {
            string ext = Path.GetExtension(fileName);
            if (string.Compare(ext, ".zip", true) != 0)
                return null;

            fileName = Path.GetFileNameWithoutExtension(fileName);
            if (!fileName.ToLower().StartsWith(prefix.ToLower()))
                return null;

            fileName = fileName.Substring(prefix.Length);

            DateTime result;
            if (!DateTime.TryParseExact(fileName, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out result))
                return null;

            return result;
        }

        public abstract bool Ping(string server);
        public abstract FtpFileInfo[] GetFileList(string host, string path, string login, string password, string prefix);
        public abstract bool GetFileSize(string host, string path, string login, string password, out long? size);
        public abstract byte[] GetFile(string host, string path, string login, string password);
    }
}
