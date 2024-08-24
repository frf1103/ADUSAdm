using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace FarmPlannerAdm.FormatingConfiguration
{
    public class DecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult != ValueProviderResult.None && !string.IsNullOrEmpty(valueProviderResult.FirstValue))
            {
                decimal actualValue = 0;
                bool success = false;

                // Attempt to parse the input string
                success = decimal.TryParse(valueProviderResult.FirstValue, NumberStyles.Any, CultureInfo.CurrentCulture, out actualValue);

                if (!success)
                {
                    // Fallback to a different culture if parsing fails
                    success = decimal.TryParse(valueProviderResult.FirstValue, NumberStyles.Any, new CultureInfo("pt-BR"), out actualValue);
                }

                if (success)
                {
                    bindingContext.Result = ModelBindingResult.Success(actualValue);
                    return Task.CompletedTask;
                }
            }

            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid decimal value.");
            return Task.CompletedTask;
        }
    }
}