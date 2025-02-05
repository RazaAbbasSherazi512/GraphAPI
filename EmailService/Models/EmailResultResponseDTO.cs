using System;
using System.Collections.Generic;
using System.Text;

namespace EmailService.Models
{
    public class EmailResultResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string Token { get; set; }
        public bool RefreshToken { get; set; }
        public StatusCode StatusCode { get; set; }
    }
    public enum StatusCode
    {
        Succeeded,
        Failed,
        InvalidClientOrTenantId,
        DataNotFound
    }
}
