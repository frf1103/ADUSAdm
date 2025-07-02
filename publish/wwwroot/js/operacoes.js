function loadoperacoes(idoper, operacao, todos, tipo) {
    $.ajax({
        type: "get",
        url: "/operacao/getdata?idtipo=" + tipo,
        data: {},
        dataType: 'json',

        async: false,
        contentType: "application/json; charset=utf-8",
        success: function (obj) {
            if (obj != null) {
                var data = obj;
                var selectbox = idoper;
                selectbox.find('option').remove();
                ops = []
                if (todos) {
                    ops.push({
                        id: "0", text: "escolha uma operação", plantio: "false"
                    });
                    //$('<option>').val("0").text("escolha um produto").appendTo(selectbox);
                }

                $.each(data, function (i, d) {
                    ops.push({
                        id: d.id, text: d.descricao, plantio: d.plantio.toString()
                    });

                    //$('<option>').val(d.id).text(d.descricao).appendTo(selectbox);
                });
                selectbox.select2({
                    data: ops
                });
                if (operacao != "0") {
                    selectbox.val(operacao);
                }
            }
        }
    });
}