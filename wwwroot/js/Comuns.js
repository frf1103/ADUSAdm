

$.validator.methods.number = function (value, element) {
    return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:,\d{3})+)?(?:\,\d+)?$/.test(value);
}

function ajustafontselect2() { 
    $('.select2-selection__rendered').css('font-family', 'Roboto Condensed, sans-serif').css('font-size', '12px');
    $('.select2-dropdown .select2-results__option').css('font-size', '12px !important').css('font-family', 'Roboto Condensed, sans-serif');
}
