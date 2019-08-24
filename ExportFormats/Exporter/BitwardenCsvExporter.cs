using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Bitwarden_ExportBackup.Model;
using Bitwarden_ExportBackup.Extensions;
using Bitwarden_ExportBackup.Model.Exceptions;

namespace Bitwarden_ExportBackup.ExportFormats.Exporter
{
    public class BitwardenCsvExporter : IExporter
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
                case BitwardenObject.ItemType.Identity:
                    return GetIdentityCsv(items);
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
            builder.AppendLine("folder,favorite,type,name,notes,fields,login_uri,login_username,login_password,login_totp");
            foreach (ExportItem item in items)
            {
                builder.AppendLine(CellBuilder.BuildCells(
                    item.Folder?.Name,
                    item.Object.IsFavorite ? 1 : 0,
                    item.Object.Type.ToString(),
                    item.Object.Name,
                    item.Object.Notes,
                    item.Object.Fields?.Select(x => $"{x.Name}: {x.Value}").Join("; "),
                    item.Object.Login?.Uris?.Select(x => x.Uri).Join(";"),
                    item.Object.Login?.Username,
                    item.Object.Login?.Password,
                    item.Object.Login?.Totp).Join(","));
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
            builder.AppendLine("cardholderName,brand,number,expMonth,expYear,code");
            foreach (ExportItem item in items)
            {
                builder.AppendLine(CellBuilder.BuildCells(
                    item.Object.Card.CardholderName,
                    item.Object.Card.Brand,
                    item.Object.Card.Number,
                    item.Object.Card.ExpiryMonth,
                    item.Object.Card.ExpiryYear,
                    item.Object.Card.Code).Join(","));
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
        /// Returns a file containing the exported identity information
        /// Encoding is iso-8859-1
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private byte[] GetIdentityCsv(List<ExportItem> items)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("title,firstName,middleName,lastName,address1,address2,address3,city,state,postalCode,country,company,email,phone,ssn,username,passportNumber,licenseNumber");
            foreach (ExportItem item in items)
            {
                builder.AppendLine(CellBuilder.BuildCells(
                    item.Object.Identity.Title,
                    item.Object.Identity.FirstName,
                    item.Object.Identity.MiddleName,
                    item.Object.Identity.LastName,
                    item.Object.Identity.Adress1,
                    item.Object.Identity.Adress2,
                    item.Object.Identity.Adress3,
                    item.Object.Identity.City,
                    item.Object.Identity.State,
                    item.Object.Identity.PostalCode,
                    item.Object.Identity.Country,
                    item.Object.Identity.Company,
                    item.Object.Identity.EMail,
                    item.Object.Identity.Phone,
                    item.Object.Identity.SSN,
                    item.Object.Identity.Username,
                    item.Object.Identity.PassportNumber,
                    item.Object.Identity.LicenseNumber).Join(","));
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
                BitwardenObject.ItemType.Identity,
                BitwardenObject.ItemType.Login,
                BitwardenObject.ItemType.SecureNote
            };
        }
    }
}
