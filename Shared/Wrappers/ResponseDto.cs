using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Wrappers
{
    public class ResponseDto<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        private ResponseDto() { }

        public static ResponseDto<T> SuccessResult(T data)
        {
            return new ResponseDto<T>
            {
                Success = true,
                Data = data,
            };
        }

        public static ResponseDto<T> FailResult(string errorCode, string errorMessage)
        {
            return new ResponseDto<T>
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        public static ResponseDto<T> FailResult(string errorMessage)
        {
            return new ResponseDto<T>
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
