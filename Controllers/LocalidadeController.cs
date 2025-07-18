﻿using ADUSAdm.Shared;
using ADUSClient.Controller;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super,User,Admin")]
    public class LocalidadeController : Controller
    {
        private readonly SharedControllerClient _sharedAPI;
        private readonly SessionManager _sessionManager;

        public LocalidadeController(SharedControllerClient sharedAPI, SessionManager sessionManager)
        {
            _sharedAPI = sharedAPI;
            _sessionManager = sessionManager;
        }

        public async Task<JsonResult> GetCidades(int iduf, string? nomecidade = "")
        {
            Task<List<ADUSClient.Localidade.MunicipioViewModel>> ret = _sharedAPI.ListaCidade(iduf, nomecidade);
            List<ADUSClient.Localidade.MunicipioViewModel> c = await ret;

            return Json(c);
        }
        [AllowAnonymous]
        public async Task<JsonResult> GetCidadeByIBGE(string ibge)
        {
            Task<ADUSClient.Localidade.MunicipioViewModel> ret = _sharedAPI.ListaCidadeIBGE(ibge);
            ADUSClient.Localidade.MunicipioViewModel c = await ret;

            return Json(c);
        }
    }
}