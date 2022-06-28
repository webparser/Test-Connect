using System;

namespace iFlow.DataProviders
{
    /// <summary>
    /// Информация о файле (только необходимые нам данные), получаемая с ftp-сервера в результате считывания списка 
    /// файлов командой LIST.
    /// </summary>
    public class FtpFileInfo
    {
        /// <summary>
        /// Инициализация экземпляра класса FtpFileInfo.
        /// </summary>
        /// <param name="name">Имя файла, закодиованное в формате Prefix_yyyyMMdd.zip</param>
        /// <param name="date">Дата файла(извлекается из имени).</param>
        /// <param name="size">Размер файла. Может быть неизвестен в момент создания экземпляра.</param>
        public FtpFileInfo(string name, DateTime date, long? size)
            : base()
        {
            Name = name;
            Date = date;
            Size = size != -1 ? size : null;
        }

        /// <summary>
        /// Имя файла в формате Prefix_yyyyMMdd.zip, где Prefix - либо Hourly, либо Msg (на момент написания провайдера) - 
        /// тип запрашиваемых данных.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Дата файла. Данная дата извлекается из имени файла (напр. Hourly_yyyyMMdd.zip).
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Размер файла. ftp-сервер может передавать размеры файлов вместе со списком, а может не поддерживать данную
        /// функциюю. Если сервер не передает длины файлов вместе со списком, их нужно получать другими методами.
        /// </summary>
        public long? Size { get; set; }
    }

}