namespace FarmPlannerAdm.ViewModel.Cultura
{
    public class CulturaViewModel
    {
        public int id { get; set; }
        public string descricao { get; set; }
        public string unidadeProdutiva { get; set; }
        public string nomeProduto { get; set; }
        public int diasEstimadosEmergencia { get; set; }
        public string? codigoExterno { get; set; }

        //public List<ListVariedadeViewModel>? listVariedade { get; set; }
    }
}