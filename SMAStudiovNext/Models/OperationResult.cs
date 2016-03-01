using SMAStudiovNext.Services;
using System.Net;

namespace SMAStudiovNext.Models
{
    public class OperationResult
    {
        public OperationStatus Status { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public string RequestUrl { get; set; }
    }
}
