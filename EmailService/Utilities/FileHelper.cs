using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EmailService.Utilities
{
    internal sealed class FileHelper
    {
        internal static string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            string mimeType;

            switch (extension)
            {
                case ".pdf":
                    mimeType = "application/pdf";
                    break;
                case ".txt":
                    mimeType = "text/plain";
                    break;
                case ".doc":
                    mimeType = "application/msword";
                    break;
                case ".docx":
                    mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                case ".jpg":
                    mimeType = "image/jpeg";
                    break;
                case ".png":
                    mimeType = "image/png";
                    break;
                default:
                    mimeType = "application/octet-stream"; 
                    break;
            }

            return mimeType;
        }

    }
}
