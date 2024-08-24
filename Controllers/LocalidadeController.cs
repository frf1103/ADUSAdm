using FarmPlannerAdm.Shared;
using FarmPlannerClient.Controller;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC")]
    public class LocalidadeController : Controller
    {
        private readonly FarmPlannerClient.Controller.SharedControllerClient _sharedAPI;
        private readonly SessionManager _sessionManager;

        public LocalidadeController(SharedControllerClient sharedAPI, SessionManager sessionManager)
        {
            _sharedAPI = sharedAPI;
            _sessionManager = sessionManager;
        }

        public async Task<JsonResult> GetCidades(int iduf)
        {
            Task<List<FarmPlannerClient.Localidade.MunicipioViewModel>> ret = _sharedAPI.ListaCidade(iduf, "");
            List<FarmPlannerClient.Localidade.MunicipioViewModel> c = await ret;

            return Json(c);
        }
    }
}