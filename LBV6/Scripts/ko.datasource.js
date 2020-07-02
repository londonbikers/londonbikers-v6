/*! https://github.com/CraigCav/ko.datasource */
// Nov 2014: modified by Jay Van Der Zant (jay@tetron.eu) to support a starting page, onPageChange custom event and general js best-practices
// Mar 2016: modified by Jay Van Der Zant (jay@tetron.eu) to include a .replace function.
(function (ko) {

    function datasource(source, target) {

        var resolvedTarget = target || ko.observable(),
            paused = true,
            trigger = ko.observable(false),
            loading = ko.observable(false),
            result = ko.computed({
                read: function () {
                    if (paused) {
                        paused = false;
                        trigger(true);
                    }
                    return resolvedTarget();
                },
                write: function (newValue) {
                    resolvedTarget(newValue);
                    loading(false);
                },
                deferEvaluation: true
            });

        ko.computed(function () {
            if (!trigger()) return;
            loading(true);
            source.call(result);
        });

        result.refresh = function () {
            trigger(trigger() + 1);
        };

        result.replace = function (oldItem, newItem) {
            resolvedTarget.replace(oldItem, newItem);
        };

        result.sort = function (sortCallback) {

            Log("sort()");

            resolvedTarget.sort(function(a, b) {
                return sortCallback(a, b);
            });

        }

        result.loading = loading;
        return result;
    }

    function datasourcePager(startPage, limit, onPageChange) {

        var self = this;
        self.onPageChange = function () { onPageChange(); }
        self.page = ko.observable(startPage);
        self.totalCount = ko.observable(0);
        self.limit = ko.observable(limit);

        self.totalPages = ko.computed(function () {
            var count = Math.ceil(ko.utils.unwrapObservable(self.totalCount) / ko.utils.unwrapObservable(self.limit));
            return count == 0 ? 1 : count;
        }, self);

        self.pages = ko.computed(function () {
            var a = [];
            var count = self.totalPages();
            for (var p = 0; p < count; p++) {
                a.push(p + 1);
            }
            return a;
        }, self);

        self.next = function () {
            var currentPage = self.page();
            self.page(currentPage === self.totalPages() ? self.totalPages() : currentPage + 1);
            self.onPageChange();
        }.bind(self);

        self.previous = function () {
            var currentPage = self.page();
            self.page(currentPage === 1 ? 1 : currentPage - 1);
            self.onPageChange();
        }.bind(self);

        self.specific = function (page) {
            self.page(page);
            self.onPageChange();
        }.bind(self);

        self.first = function () {
            self.page(1);
            self.onPageChange();
        }.bind(self);

        self.last = function () {
            self.page(self.totalPages());
            self.onPageChange();
        }.bind(self);

        self.isFirstPage = ko.computed(function () {
            return parseInt(self.page()) === 1;
        }, self);

        self.isLastPage = ko.computed(function () {
            return parseInt(self.page()) === parseInt(self.totalPages());
        }, self);
    }

    ko.extenders.datasource = function (target, source) {
        var result = datasource(source, target);
        result.options = target.options || {};
        return result;
    };

    ko.extenders.pager = function (target, options) {
        var pager = new datasourcePager(options.startPage, options.limit || 10, options.onPageChange || {});
        target.options = target.options || {};
        target.options.pager = target.pager = pager;
        return target;
    };

})(ko);