using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Monq.Core.HttpClientExtensions.Extensions
{
    /// <summary>
    /// Хелпер для создания оберток над объектами в виде http-ответа.
    /// </summary>
    public static class RestHttpResponseMessageWrapper
    {
        /// <summary>
        /// Вернуть пустой экземпляр <typeparamref name="TResult"/>, который обернут в http-ответ с кодом 200.
        /// </summary>
        /// <typeparam name="TResult">Тип объекта, пустой экземпляр которого будет создан.</typeparam>
        public static RestHttpResponseMessage<TResult> Empty<TResult>()
        {
            var message = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            var resultMessage = new RestHttpResponseMessage<TResult>(message);

            var resultType = typeof(TResult);

            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var genericTypeArguments = resultType.GenericTypeArguments;

                var listType = typeof(List<>);
                var constructedListType = listType.MakeGenericType(genericTypeArguments);

                var instance = Activator.CreateInstance(constructedListType);
                resultMessage.ResultObject = (TResult)instance;
            }
            else
                resultMessage.ResultObject = default;

            return resultMessage;
        }
    }
}
