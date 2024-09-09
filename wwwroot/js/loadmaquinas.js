function loadmaquinas(idmaquina, maquina, todos, idmodelo, filtro) {
    $.ajax({
        type: "get",
        url: "/maquina/getdata?idmodelo=" + idmodelo + "&filtro=" + filtro,
        data: {},
        dataType: 'json',

        async: false,
        contentType: "application/json; charset=utf-8",
        success: function (obj) {
            if (obj != null) {
                var data = obj;
                var selectbox = idmaquina;
                selectbox.find('option').remove();
                if (todos) {
                    $('<option>').val("0").text("escolha uma máquina").appendTo(selectbox);
                }
                talhoes = [];
                $.each(data, function (i, d) {
                    $('<option>').val(d.id).text(d.descricao).appendTo(selectbox);
                });

                if (produto != "0") {
                    selectbox.val(maquina);
                }
            }
        }
    });
}