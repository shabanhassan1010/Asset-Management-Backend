using Asset.Application.Common.Responses;
using Asset.Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Asset.Application.Behaviors
{
    /// <summary>
    /// Translates the response Message into the request's language (from the Accept-Language header).
    ///
    /// Why a behavior and not a localizer in every handler?
    ///  1. One place: handlers keep writing plain English ("Employee Created Successfully").
    ///  2. The cache stays language-neutral: this behavior is registered FIRST, so it is the
    ///     OUTERMOST step. CachingBehavior (inside it) stores the English response; the translation
    ///     happens after, on the way out. An Arabic request never fills the cache with Arabic
    ///     text that an English request would then receive.
    /// </summary>
    public class LocalizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        #region Fields
        private readonly IStringLocalizer<SharedResources> _localizer;
        #endregion

        #region Constructor
        public LocalizationBehavior(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
        }
        #endregion

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next();

            // The English text is the key. A text with no translation (or English requests)
            // comes back unchanged, so this is always safe.
            if (response is IHasMessage withMessage && !string.IsNullOrWhiteSpace(withMessage.Message))
                withMessage.Message = _localizer[withMessage.Message];

            return response;
        }
    }
}

#region Flow
//   Request (Accept-Language: ar)
//          ↓
//   LocalizationBehavior   ── waits for the response ──────────────┐
//          ↓                                                         │
//   LoggingBehavior → ValidationBehavior → CachingBehavior → Handler │
//                                      (stores ENGLISH)              │
//          ↓                                                         │
//   Message "Employee Created Successfully"  ───────────────────────┘
//          ↓  _localizer[...]
//   Message "تم إنشاء الموظف بنجاح"
#endregion
