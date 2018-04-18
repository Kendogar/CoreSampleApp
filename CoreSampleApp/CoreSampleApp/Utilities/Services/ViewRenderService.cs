using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace CoreSampleApp.Utilities.Services
{
    /*
     * Short Description:
     * Allows to render a specific view from (almost) everywhere with the view name and the view model given as parameters to RenderToStringAsync();
     * 
     * Usage:
     * 
     * 1. Add the service to the application scope in Startup.cs adding the following to the ConfigureServices() method
     * 
     * public void ConfigureServices(IServiceCollection services)
     *  {
     *      [...]
     *
     *      services.AddScoped<IViewRenderService, ViewRenderService>();
     *  }
     * 
     * 2. Inject the service where you need it
     * 
     * public class SomeApiController : Controller
     * {
     *  private readonly IViewRenderService _viewRenderService;
     *
     *  public SomeApiController(IViewRenderService viewRenderService)
     *  {
     *      _viewRenderService = viewRenderService;
     *  }
     * }
     * 
     * 3. Use the viewRenderService's methods in any method
     * 
     *  [HttpGet]
     *  public async Task<IActionResult> SomeAction()
     *  {
     *      [...]
     *
     *      var viewModel = SomeViewModel.ToViewModel(someData);
     *
     *      var result = await _viewRenderService.RenderToStringAsync("Path/To/Razor/View/_MyPartialName", viewModel);
     *
     *      return Content(result);
     *  }
     * 
     */

    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model);
    }

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public ViewRenderService(IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderToStringAsync(string viewName, object model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = _razorViewEngine.FindView(actionContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }
    }
}
