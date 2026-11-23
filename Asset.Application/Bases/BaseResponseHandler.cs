using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using System.Net;


namespace Asset.Application.Bases
{
    public class BaseResponseHandler
    {
        // These helpers no longer translate: they put the resource KEY (e.g. "GetSuccess") or the
        // caller's English message in Message, and LocalizationBehavior translates it on the way out.
        // Translating here would store Arabic or English text in the Redis cache (query handlers are
        // cached), and the next caller would get the first caller's language.
        // The localizer parameter stays so the existing handlers and tests keep compiling unchanged.
        #region Constructor
        public BaseResponseHandler(IStringLocalizer<SharedResources> localizer)
        {
        }
        #endregion

        #region Methods
        public BaseResponse<T> GetSuccess<T>(object Meta = null, string Message = null, HttpStatusCode? statuscode = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = statuscode ?? System.Net.HttpStatusCode.OK,    //= statuscode == null ?  System.Net.HttpStatusCode.OK : statuscode ,
                Succeeded = true,
                Message = SharedResourcesKeys.GetSuccess,
                Meta = Meta
            };
        }
        public BaseResponse<T> GetSuccess<T>(T entity, object Meta = null, string Message = null, HttpStatusCode? statuscode = null)
        {
            return new BaseResponse<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = SharedResourcesKeys.GetSuccess,
                Meta = Meta
            };
        }

        public BaseResponse<T> Deleted<T>(string message = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.NoContent,
                Succeeded = true,
                Message = message ?? SharedResourcesKeys.DeleteSuccess
            };
        }
        public BaseResponse<T> EditSuccess<T>(T entity, object Meta = null)
        {
            return new BaseResponse<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = SharedResourcesKeys.UpdateSuccess,
                Meta = Meta
            };
        }
        public BaseResponse<T> Success<T>(T entity, object Meta = null)
        {
            return new BaseResponse<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = SharedResourcesKeys.AddSuccess,
                Meta = Meta
            };
        }
        public BaseResponse<T> Unauthorized<T>()
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.Unauthorized,
                Succeeded = true,
                Message = SharedResourcesKeys.UnAuthorized
            };
        }
        public BaseResponse<T> BadRequest<T>(string Message = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Succeeded = false,
                Message = Message ?? SharedResourcesKeys.BadRequest
            };
        }
        public BaseResponse<T> NotFound<T>(string message = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Succeeded = false,
                Message = message ?? SharedResourcesKeys.NotFound
            };
        }
        public BaseResponse<T> Created<T>(T entity, object Meta = null)
        {
            return new BaseResponse<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.Created,
                Succeeded = true,
                Message = SharedResourcesKeys.AddSuccess,
                Meta = Meta
            };
        }

        public BaseResponse<T> UnProcessableEntity<T>(string message = null)
        {
            // Equivalent to HTTP status 422.System.Net.HttpStatusCode.UnProcessableEntity
            // indicates that the request was well-formed but was unable to be followed due
            // to semantic errors.
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.UnprocessableEntity,
                Succeeded = false,
                Message = message ?? SharedResourcesKeys.UnProcessableEntity
            };
        }
        #endregion
    }
}
