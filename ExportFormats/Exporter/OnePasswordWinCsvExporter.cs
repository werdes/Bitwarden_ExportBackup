using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bitwarden_ExportBackup.Extensions;
using Bitwarden_ExportBackup.Model;
using Bitwarden_ExportBackup.Model.Exceptions;

namespace Bitwarden_ExportBackup.ExportFormats.Exporter
{
    public class OnePasswordWinCsvExporter : IExporter
    {
        /// <summary>
        /// Returns the content of the file that is to be exported
        /// </summary>
        /// <param name="items"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public byte[] GetFileContent(List<ExportItem> items, BitwardenObject.ItemType itemType)
        {
            switch (itemType)
            {
                case BitwardenObject.ItemType.Login:
                    return GetLoginCsv(items);
                case BitwardenObject.ItemType.SecureNote:
                    return GetSecureNoteCsv(items);
                case BitwardenObject.ItemType.Card:
                    return GetCardCsv(items);
                default:
                    //Should not happen, but nevertheless
                    throw new UnsupportedItemTypeException(this, itemType);
            }
        }

        /// <summary>
        /// Returns a file containing login information
        /// Encoding is iso-8859-1
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private byte[] GetLoginCsv(List<ExportItem> items)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("title,website,username,password,notes");
            foreach (ExportItem item in items)
            {
                List<string> lstCells = new List<string>()
                {
                    item.Object.Name,
                    item.Object.Login?.Uris?.Select(x => x.Uri).Join(";"),
                    item.Object.Login?.Username,
                    item.Object.Login?.Password,
                    item.Object.Notes
                };

                lstCells.AddRangeIfNotNull(item.Object.Fields?.Select(x => $"{x.Name}:{x.Value}"));

                builder.AppendLine(CellBuilder.BuildCells(lstCells.ToArray()).Join(","));
            }

            return Encoding.GetEncoding("iso-8859-1").GetBytes(builder.ToString());
        }

        /// <summary>
        /// Returns a file that contains credit card information 
        /// Encoding is iso-8859-1
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private byte[] GetCardCsv(List<ExportItem> items)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("title,card number,expiry date (MM/YYYY),cardholder name,PIN,bank name,CVV,notes");
            foreach (ExportItem item in items)
            {
                builder.AppendLine(CellBuilder.BuildCells(
                    item.Object.Card.Brand,
                    item.Object.Card.Number,
                    new DateTime(int.Parse(item.Object.Card.ExpiryYear), int.Parse(item.Object.Card.ExpiryMonth), 1).ToString("MM/yyyy"),
                    item.Object.Card.CardholderName,
                    item.Object.Card.Code,
                    string.Empty,
                    string.Empty,
                    item.Object.Notes).Join(","));
            }

            return Encoding.GetEncoding("iso-8859-1").GetBytes(builder.ToString());
        }

        /// <summary>
        /// Returns a file containing the exported secure notes
        /// Encoding is iso-8859-1
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private byte[] GetSecureNoteCsv(List<ExportItem> items)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("title,note");
            foreach (ExportItem item in items)
            {
                builder.AppendLine(CellBuilder.BuildCells(
                    item.Object.Name,
                    item.Object.Notes).Join(","));
            }

            return Encoding.GetEncoding("iso-8859-1").GetBytes(builder.ToString());
        }

        /// <summary>
        /// Returns the file extension of the output file
        /// </summary>
        /// <returns></returns>
        public string GetFileExtension()
        {
            return "csv";
        }

        /// <summary>
        /// Returns the implemented item types that are supported by the target format
        /// </summary>
        /// <returns></returns>
        public BitwardenObject.ItemType[] GetSupportedItemTypes()
        {
            return new BitwardenObject.ItemType[]
            {
                BitwardenObject.ItemType.Card,
                BitwardenObject.ItemType.Login,
                BitwardenObject.ItemType.SecureNote
            };
        }
    }
}
