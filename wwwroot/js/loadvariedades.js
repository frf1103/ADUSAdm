function loadvariedades(variedade,todos) {
    if ($("#idsafra").val() != "0") {
        $.ajax({
            type: "get",
            url: "/anoagricola/getvariedadesbysafra?idsafra=" + $("#idsafra").val(),
            data: {},
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            success: function (obj) {
                if (obj != null) {
                    var data = obj;
                    var selectbox = $('#idvariedade');
                    selectbox.find('option').remove();
                    if (todos) {
                        $('<option>').val("0").text("escolha uma variedade").appendTo(selectbox);
                    }
                    $.each(data, function (i, d) {
                        $('<option>').val(d.id).text(d.descricao).appendTo(selectbox);
                    });

                    if (variedade != "0") {
                        $('#idvariedade').val(variedade);
                    }
                }
            }
        });
    }
}