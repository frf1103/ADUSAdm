function formatCpfCnpj(input) {
    let value = input.value.replace(/\D/g, ''); // Remove todos os caracteres não numéricos
    input.value = formatreg(value);
}

function formatreg(value) {
    let formattedValue;

    if (value.length <= 11) { // Formatação de CPF
        formattedValue = value.replace(/(\d{3})(\d)/, '$1.$2')
            .replace(/(\d{3})(\d)/, '$1.$2')
            .replace(/(\d{3})(\d{1,2})$/, '$1-$2');
    } else { // Formatação de CNPJ
        formattedValue = value.replace(/^(\d{2})(\d)/, '$1.$2')
            .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
            .replace(/\.(\d{3})(\d)/, '.$1/$2')
            .replace(/(\d{4})(\d{1,2})$/, '$1-$2');
    }

    return formattedValue;
}