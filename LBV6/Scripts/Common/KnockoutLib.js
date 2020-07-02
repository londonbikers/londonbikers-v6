// Here's a custom Knockout binding that makes elements shown/hidden via jQuery's fadeIn()/fadeOut() methods
ko.bindingHandlers.fadeVisible = {
    init: function (element, valueAccessor) {
        // Initially set the element to be instantly visible/hidden depending on the value
        $(element).toggle(ko.utils.unwrapObservable(valueAccessor())); // Use "unwrapObservable" so we can handle values that may or may not be observable
    },
    update: function (element, valueAccessor) {
        // Whenever the value subsequently changes, slowly fade the element in or out
        if (ko.utils.unwrapObservable(valueAccessor())) {
            $(element).fadeIn(100);
        } else {
            $(element).fadeOut(100);
        }
    }
};