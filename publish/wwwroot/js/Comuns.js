

function ajustafontselect2() {
    $('.select2-selection__rendered').css('font-family', 'Roboto Condensed, sans-serif').css('font-size', '12px');
    $('.select2-dropdown .select2-results__option').css('font-size', '12px !important').css('font-family', 'Roboto Condensed, sans-serif');
}

function atualizarTextoModalProcessando(titulo, subtitulo) {
    document.getElementById("modalTituloProcessando").innerText = titulo;
    document.getElementById("modalTextoProcessando").innerText = subtitulo;
}

function formatarData(data) {
    const d = new Date(data);
    const ano = d.getFullYear();
    const mes = String(d.getMonth() + 1).padStart(2, '0');
    const dia = String(d.getDate()).padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
}