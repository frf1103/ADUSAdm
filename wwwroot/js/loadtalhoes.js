var talhoes = [];

function loadtalhoes(talhao,todos) {
    var x = $("#idfaz").val();
    $.ajax({
        type: "get",
        url: "/defareas/gettalhoes?idfazenda=" + x,
        data: {},
        dataType: 'json',

        async: false,
        contentType: "application/json; charset=utf-8",
        success: function (obj) {
            if (obj != null) {
                var data = obj;
                var selectbox = $('#idtalhao');
                selectbox.find('option').remove();
                if (todos) {
                    $('<option>').val("0").text("escolha um talhão").appendTo(selectbox);
                }
                talhoes = [];
                $.each(data, function (i, d) {
                    talhoes.push({ "id": d.id, "area": d.areaProdutiva });
                    $('<option>').val(d.id).text(d.descricao).appendTo(selectbox);
                });

                if (talhao != "0") {
                    $('#idtalhao').val(talhao);
                }
            }
        }
    });
}