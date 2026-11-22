using Microsoft.AspNetCore.Mvc;

namespace MYAcademy_IdentitiyEmailProject.ViewComponents.RegisterViewComponents
{
    public class _RegisterTermsComponentPartial:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();  
        }
    }
}
