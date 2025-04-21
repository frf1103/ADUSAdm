using System.Reflection;

namespace ADUSAdm.Shared
{
    public static class ViewModelMapper
    {
        public static TTarget Map<TSource, TTarget>(TSource source)
            where TTarget : new()
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            TTarget target = new TTarget();
            PropertyInfo[] sourceProperties = typeof(TSource).GetProperties();
            PropertyInfo[] targetProperties = typeof(TTarget).GetProperties();

            foreach (var sourceProperty in sourceProperties)
            {
                var targetProperty = Array.Find(targetProperties, p => p.Name == sourceProperty.Name && p.PropertyType == sourceProperty.PropertyType);
                if (targetProperty != null && targetProperty.CanWrite)
                {
                    targetProperty.SetValue(target, sourceProperty.GetValue(source));
                }
            }

            return target;
        }
    }
}