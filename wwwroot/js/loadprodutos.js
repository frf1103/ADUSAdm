function loadprodutos(idproduto, produto, todos, idprincipio, idgrupo, idfab, filtro, tipo) {
    $.ajax({
        type: "get",
        url: "/produto/getdata?idprincipio=" + idprincipio + "&idgrupo=" + idgrupo + "&idfab=" + idfab + "&filtro=" + filtro + '&tipo=' + tipo,
        data: {},
        dataType: 'json',

        async: false,
        contentType: "application/json; charset=utf-8",
        success: function (obj) {
            if (obj != null) {
                var data = obj;
                var selectbox = idproduto;
                selectbox.find('option').remove();
                prods = []
                if (todos) {
                    prods.push({
                        id: "0", text: "escolha um produto",und:""
                    });
                    $('<option>').val("0").text("escolha um produto").appendTo(selectbox);
                }

                $.each(data, function (i, d) {
                    prods.push({
                        id: d.id, text: d.descricao, und: d.descunidade
                    });

                    //$('<option>').val(d.id).text(d.descricao).appendTo(selectbox);
                });
                selectbox.select2({
                    data: prods
                });
                if (produto != "0") {
                    selectbox.val(produto);
                }
            }
        }
    });
}