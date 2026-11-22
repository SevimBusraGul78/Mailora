using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MYAcademy_IdentitiyEmailProject.ViewComponents.MessageLayoutViewComponents
{
    public class _MessageLayoutStyleComponentPartial:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
