namespace MassTransit.Initializers.PropertyProviders
{
    using System.Threading.Tasks;
    using Util;


    public class FromNullablePropertyProvider<TInput, TProperty> :
        IPropertyProvider<TInput, TProperty>
        where TInput : class
        where TProperty : struct
    {
        readonly IPropertyProvider<TInput, TProperty?> _provider;

        public FromNullablePropertyProvider(IPropertyProvider<TInput, TProperty?> provider)
        {
            _provider = provider;
        }

        public Task<TProperty> GetProperty<T>(InitializeContext<T, TInput> context)
            where T : class
        {
            if (context.HasInput)
            {
                var propertyTask = _provider.GetProperty(context);
                if (propertyTask.IsCompleted)
                    return Task.FromResult(propertyTask.Result ?? default);

                async Task<TProperty> GetPropertyAsync()
                {
                    return await propertyTask.ConfigureAwait(false) ?? default;
                }

                return GetPropertyAsync();
            }

            return TaskUtil.Default<TProperty>();
        }
    }
}
