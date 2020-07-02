ko.bindingHandlers.redactor = {

    _updateFromModel: true,

    init: function (element, valueAccessor, allBindings) {

        var value = valueAccessor();

        // We only want Redactor to notify our value of changes if the value
        // is an observable (rather than a string, say).

        this._updateFromModel = true;
        if (ko.isObservable(value)) {

            var toolbarParam = allBindings ? allBindings.get("toolbar") : null;
            var maxHeightParam = allBindings ? allBindings.get("maxHeight") : null;
            var placeholderParam = allBindings ? allBindings.get("placeholder") : null;
            var isModeratorParam = window._user.IsModerator;

            if (allBindings && !IsNull(allBindings.get("updateFromModel"))) {
                this._updateFromModel = allBindings.get("updateFromModel");
            }

            // override, never show the toolbar on mobile devices, it just doesn't work well
            if (window.IsMobile()) {
                toolbarParam = false;
            }

            if (maxHeightParam) {

                if (!IsNull(toolbarParam)) {

                    if (!IsNull(placeholderParam)) {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, toolbar: false, placeholder: placeholderParam, plugins: ["source", "quote"], linkSize: 100 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, toolbar: false, placeholder: placeholderParam, plugins: ["quote"], linkSize: 100 });
                        }

                    } else {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, toolbar: false, plugins: ["source", "quote"], linkSize: 100 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, toolbar: false, plugins: ["quote"], linkSize: 100 });
                        }
                        
                    }
                    
                } else {

                    if (!IsNull(placeholderParam)) {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, placeholder: placeholderParam, plugins: ["source", "quote"], linkSize: 100 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, placeholder: placeholderParam, plugins: ["quote"], linkSize: 100 });
                        }

                    } else {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, plugins: ["source", "quote"], linkSize: 100 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, maxHeight: maxHeightParam, plugins: ["quote"], linkSize: 100 });
                        }

                    }

                }

            } else {

                if (!IsNull(toolbarParam)) {

                    if (!IsNull(placeholderParam)) {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, toolbar: false, placeholder: placeholderParam, plugins: ["source", "quote"], linkSize: 1000 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, toolbar: false, placeholder: placeholderParam, plugins: ["quote"], linkSize: 1000 });
                        }

                    } else {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, toolbar: false, plugins: ["source", "quote"], linkSize: 1000 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, toolbar: false, plugins: ["quote"], linkSize: 1000 });
                        }
                    }

                } else {

                    if (!IsNull(placeholderParam)) {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, placeholder: placeholderParam, plugins: ["source", "quote"], linkSize: 1000 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, placeholder: placeholderParam, plugins: ["quote"], linkSize: 1000 });
                        }

                    } else {

                        if (isModeratorParam) {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, plugins: ["source", "quote"], linkSize: 1000 });
                        } else {
                            $(element).redactor({ buttonsHide: ["format", "line"], callbacks: { change: value }, toolbarFixed: false, plugins: ["quote"], linkSize: 1000 });
                        }
                    }

                }

            }
        }

    },

    update: function (element, valueAccessor) {

        // New value, note that Redactor expects the argument passed to 'set'
        // to have toString method, which is why we disjoin with ''.

        var value = ko.utils.unwrapObservable(valueAccessor()) || "";

        // We only call 'set' if the content has changed, as we only need to
        // to do so then, and 'set' also resets the cursor position, which
        // we don't want happening all the time.

        // The API method has become 'code.get', and it behaves a bit differently: it
        // returns formatted HTML, i.e. with whitespace and EOLs. That means that we
        // would update the Redactor content every time the observable changed, which
        // was bad. So instead we can use this:

        if (value !== $(element).redactor("core.textarea").val() && this._updateFromModel) {
            $(element).redactor("code.set", value);
        }
    }

}