using Asset.Application.Resoures;
using Microsoft.Extensions.Localization;
using System.Net;


namespace Asset.Application.Bases
{
    public class BaseResponseHandler
    {
        #region Fields
        private readonly IStringLocalizer<SharedResources> _localizer;
        #endregion

        #region Constructor
        public BaseResponseHandler(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
        }
        #endregion

        #region Methods
        public BaseResponse<T> GetSuccess<T>(object Meta = null, string Message = null, HttpStatusCode? statuscode = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = statuscode ?? System.Net.HttpStatusCode.OK,    //= statuscode == null ?  System.Net.HttpStatusCode.OK : statuscode ,
                Succeeded = true,
                Message = _localizer[SharedResourcesKeys.GetSuccess],
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
                Message = _localizer[SharedResourcesKeys.GetSuccess],
                Meta = Meta
            };
        }

        public BaseResponse<T> Deleted<T>(string message = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.NoContent,
                Succeeded = true,
                Message = message != null ? _localizer[SharedResourcesKeys.DeleteSuccess] : message
            };
        }
        public BaseResponse<T> EditSuccess<T>(T entity, object Meta = null)
        {
            return new BaseResponse<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = _localizer[SharedResourcesKeys.UpdateSuccess],
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
                Message = _localizer[SharedResourcesKeys.AddSuccess],
                Meta = Meta
            };
        }
        public BaseResponse<T> Unauthorized<T>()
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.Unauthorized,
                Succeeded = true,
                Message = _localizer[SharedResourcesKeys.UnAuthorized]
            };
        }
        public BaseResponse<T> BadRequest<T>(string Message = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Succeeded = false,
                Message = Message != null ? _localizer[SharedResourcesKeys.BadRequest] : Message
            };
        }
        public BaseResponse<T> NotFound<T>(string message = null)
        {
            return new BaseResponse<T>()
            {
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Succeeded = false,
                Message = message != null ? _localizer[SharedResourcesKeys.NotFound] : message
            };
        }
        public BaseResponse<T> Created<T>(T entity, object Meta = null)
        {
            return new BaseResponse<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.Created,
                Succeeded = true,
                Message = _localizer[SharedResourcesKeys.AddSuccess],
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
                Message = message != null ? _localizer[SharedResourcesKeys.UnProcessableEntity] : message
            };
        }
        #endregion
    }
}
