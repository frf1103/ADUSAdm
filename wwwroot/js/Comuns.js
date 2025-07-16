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

function enviarWhatsapp(numero, mensagem) {
    const texto = encodeURIComponent(mensagem);
    const url = `https://wa.me/55${numero}?text=${texto}`;
    window.open(url, '_blank');
}

function enviarconviteEmail(email, idconvite, url) {
    fetch('/Convite/MailConvite', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ email: email, idconvite: idconvite, url: url })
    })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                alert("E-mail enviado com sucesso!");
            } else {
                alert("Falha ao enviar o e-mail: " + result.message);
            }
        })
        .catch(error => {
            alert("Erro ao enviar o e-mail.");
            console.error(error);
        });
}