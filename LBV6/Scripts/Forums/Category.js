// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    // ReSharper disable once PossiblyUnassignedProperty
    $.getJSON("/api/categories/" + window._categoryId, function (category) {
        ko.applyBindings(new ViewModel(category), $("#page-binding-scope")[0]);
    });
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function (category) {
    var self = this;
    self.Category = new window.CategoryModel(category);

    self.GetStructureTemplateName = function () {
        return "category-structure-" + window.GetDeviceType();
    };

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function init() {
        window.StartHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();
};