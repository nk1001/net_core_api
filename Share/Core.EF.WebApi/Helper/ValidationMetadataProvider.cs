using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Core.EF.WebApi.Helper
{
    public class ValidationMetadataProvider :
        IValidationMetadataProvider
    {
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            var attr = context.Key.ContainerType?.GetCustomAttribute(typeof(RequiredAttribute));
            if (attr == null)
            {
                var attrValid = context.ValidationMetadata.ValidatorMetadata.FirstOrDefault(t =>
                    t.GetType() == typeof(RequiredAttribute));
                if (attrValid != null)
                {
                    context.ValidationMetadata.ValidatorMetadata.Remove(attrValid);
                }
            }

        }
    }
}
