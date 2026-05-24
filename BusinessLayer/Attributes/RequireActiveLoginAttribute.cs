using BusinessLayer.Filters;
using Microsoft.AspNetCore.Mvc;

namespace BusinessLayer.Attributes
{
    public class RequireActiveLoginAttribute : TypeFilterAttribute
    {
        public RequireActiveLoginAttribute() : base(typeof(RequireActiveLoginFilter))
        {
        }
    }
}