// shared view model used just to wire up the hub

// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel(), $("#page-binding-scope")[0]);
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function () {

    function init() {
        window.StartHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();
};