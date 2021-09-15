using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Monq.Core.HttpClientExtensions.Extensions
{
    /// <summary>
    /// Helper for creating wrappers over objects in the form of an http response.
    /// </summary>
    public static class RestHttpResponseMessageWrapper
    {
        /// <summary>
        /// Return an empty instance of <typeparamref name = "TResult" />, which is wrapped in an http response with code 200.
        /// </summary>
        /// <typeparam name="TResult">The type of object to be instantiated empty.</typeparam>
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
                resultMessage.ResultObject = (TResult)instance!;
            }
            else
                resultMessage.ResultObject = default;

            return resultMessage;
        }
    }
}
